# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## About

A clicker-style game about redirecting a river carrying gold toward your civilization. Built with Godot (managed by mise — currently 4.5.2-stable).

The player shapes terrain on a grid map to guide a river from the top-left toward the bottom-right. Panning river banks earns gold; gold buys shovels; shovels reshape terrain; river flow unlocks new map regions.

Read TODO.md for what remains in the build.

## Running the Game

```bash
mise run run     # launch the game window
mise run check   # headless boot verification (exit 0 = clean)
mise run editor  # open Godot editor
```

Always run `mise run check` after any change before calling a session done.

## Testing

```bash
mise run test         # C# xUnit unit tests (pure logic, no Godot runtime)
mise run itest        # in-engine integration tests (boots Godot headless, exit 0 = pass)
mise run itest-regen  # regenerate fixture snapshots from their full (player-action) setups
```

- **Unit tests** (`tests/unit/`) are the xUnit project — pure C# logic (Economy math, FlowSteadyState, DAG). Excluded from the game build; they cannot touch the autoload-driven game.
- **Integration tests** (`tests/integration/`) are compiled INTO the game so they run inside Godot. They boot the real game via the `--run-tests` cmdline flag, drive it through `PlayerActions` (which emits the same Grid/HUD signals a player triggers), and assert on real `GameState`. Add a test by implementing `IIntegrationTest` and registering it in `IntegrationTestRunner.Tests`.
- **State recreation** has two linked forms. A *fixture* (`IFixture`) reaches a state using only player actions — the authoritative "full setup". A snapshot serialized from that run is the fast "quick setup". `mise run itest-regen` regenerates snapshots; regenerate after any change to the save format or to behaviour the fixtures depend on. `GameState.Save/Load` (+ `ToSnapshot/ApplySnapshot`) are the serialization primitives and the future save-slot foundation.
- `GameRunner.TestMode` suspends the real-time tick so tests step deterministically via `Tick(delta)` / `StepPropagation()`. `GameRunner.StartNewGame()` resets to a fresh game (also the future "New Game" button).

## Project Structure

```
scenes/
  main/        # Root scene (entry point)
  overworld/   # Grid map view (14x14 isometric)
  ui/          # HUD (gold counter, tab panels: Equip/Shop/Map)
scripts/
  autoloads/   # Global singletons: GameState, GameRunner, SceneManager
  game/        # Game systems: Economy, FlowPropagation, FlowSteadyState,
               #   RegionSystem, WaterPropagation, TileEditor, QuestSystem
  overworld/   # Grid input (Grid.cs), isometric math (IsoMath.cs), tile rendering (TileRenderer.cs)
  ui/          # HUD (HUD.cs) — all UI built in code, no layout .tscn
  main/        # Entry point (Main.cs) + Godot shim (main.gd)
  utils/       # DAG, DAGNode, DAGEdge, RiverDAG, RiverNode, RiverEdge, TileCell
tests/
  unit/        # xUnit project (pure logic) — excluded from the game build
  integration/ # In-engine harness (runner, PlayerActions, fixtures, tests) — compiled into the game
resources/
  tiles/       # Tile config definitions (.tres)
assets/
  shaders/     # tile_water, tile_channel, tile_bank, tile_soil (.gdshader)
  sprites/
  audio/
  fonts/
```

## Architecture Rules

- **System isolation**: Each system (grid, HUD, river sim) lives in its own scene subtree. Systems never reach into each other directly.
- **Communication**: Systems talk only through signals or the `GameState` autoload. Never use `get_tree().get_node("/root/OtherScene")`.
- **Data in GameState**: All mutable game data (gold, tile grid, shovels, unlocked tiles) lives in `GameState`, never in scene scripts.
- **Autoloads**: `GameState`, `SceneManager`, and `GameRunner` are global singletons (all three registered in project.godot).
- **Scene transitions**: Always use `SceneManager.change_scene(path)`, never `get_tree().change_scene_to_file()` directly.

## C# Conventions

The game logic is ~90% C# (Godot Mono). GDScript is only used for `main.gd` (a Godot compatibility shim).

- Static typing on all fields and method signatures: `int _gold = 0;`
- `[Export]` attribute for anything configurable per-instance in the editor
- Signals via `[Signal]` attribute + `EmitSignal(SignalName.Foo)` — not GDScript `signal` keyword
- Node references via `[Export]` wired in editor, or resolved once in `_Ready()`
- Signal-first design: child nodes emit signals upward; parent nodes call methods downward
- No `_Process()` polling for state changes that should use signals
- Game logic goes in `scripts/game/` classes, not in scene scripts
- Scene scripts (Grid.cs, HUD.cs, Main.cs) are thin: wire signals, call game classes, update visuals

## Tile System

- Tile types defined as enum in `GameState`: `SOIL`, `BANK`, `RIVER`, `CHANNEL`, `STONE`
  - `SOIL`: bare diggable earth
  - `BANK`: water-adjacent sediment; accumulates gold over time
  - `RIVER`: active connected water; drives gold accumulation rate
  - `CHANNEL`: transitional water (filling or draining); not yet connected to source
  - `STONE`: impassable; used for map edges and obstacles
- Grid data = 2D `TileCell[,]` stored in `GameState.Tiles` (C# array, not GDScript Array)
- `TileCell` holds: `Type` (TileType enum), gold amount (float), flow rate (float)
- Grid scene builds one Polygon2D node per tile with a custom shader material
- When `GameState` emits `tile_changed(col, row)`, only that tile node updates
- Water propagation uses a 3-pass BFS: connectivity check → drain disconnected tiles → fill adjacent channels

## DAG / Flow System

The water flow system uses a directed acyclic graph (DAG) to propagate flow values through connected river tiles. This is the most complex part of the codebase — treat `FlowPropagation.Propagate()` as a black box unless you specifically need to change flow behaviour.

- `scripts/utils/DAG.cs` — generic typed DAG with BFS traversal, node/edge CRUD
- `scripts/utils/RiverDAG.cs` / `RiverNode.cs` / `RiverEdge.cs` — DAG specialised for river tiles (node = tile position, edge = flow rate)
- `scripts/game/FlowPropagation.cs` — builds RiverDAG from tile grid; `Propagate(tiles, river)` updates flow rates; `Rebuild()` rebuilds topology from scratch; `ConnectedTiles()` returns BFS-ordered tile list
- `scripts/game/FlowSteadyState.cs` — computes the steady-state flow distribution used by `Propagate` to constrain new edges

`GameRunner` calls `FlowPropagation.Propagate()` each tick. After any tile mutation, call `FlowPropagation.Rebuild()` to update connectivity, then let the tick loop propagate flow.

**Do not read FlowPropagation.cs or FlowSteadyState.cs unless the task explicitly requires changing flow behaviour. Treat them as black boxes.**

## Economy and Quests

- `scripts/game/Economy.cs` — accumulates gold per tick on BANK tiles proportional to adjacent river flow; reads from `GameState.Tiles`, writes via `GameState.AddGold()`
- `scripts/game/QuestSystem.cs` — stub; listens to `GameState` signals for milestone events (gold thresholds, shovel purchases, tile mutations)

## Region System

- Map is divided into 14×14 regions; player starts in Region 0
- When river reaches the right edge (col 13), the next region unlocks
- Regions are independent tile grids stored in `_region_data`; switching swaps the active grid
- New regions receive water entry at col 0 matching the exit rows of the previous region
- Gold accumulates in all regions simultaneously; only active region is visible
- HUD Map tab shows a minimap of unlocked regions as clickable diamonds

## Iterative Development Rules

- **Every session ends with a runnable game.** Do not leave the project in a broken state.
- **One system per session.** Verify each phase with the user before building the next.
- **Verify after each change** with `mise run check` before declaring done.
- **Stop and wait** for explicit user sign-off before moving to the next phase.

## Build Order (Phases)

1. ~~**Bootstrap**~~ ✓ — autoloads, main scene, colored grid boots correctly
2. ~~**River shape**~~ ✓ — data-driven map with river/bank/soil/channel/stone tiles
3. ~~**Click to pan**~~ ✓ — clicking bank earns gold, HUD counter
4. ~~**Shovel**~~ ✓ — buy and use to reshape terrain
5. ~~**River flow**~~ ✓ — 3-pass BFS water propagation with fill/drain timing
6. ~~**Region unlocking**~~ ✓ — river exits east edge → next region unlocks, minimap in HUD
7. **Visual polish** — water animation, sound, UI styling, more region content
8. **Progression** — multiple resource types, equipment, crops, win condition
