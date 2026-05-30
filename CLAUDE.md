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
mise run shots   # render gameplay screenshots to screenshots/<name>.png
```

Always run `mise run check` after any change before calling a session done.

## Seeing the game (visual verification without watching the window)

`mise run shots` boots the game in a brief real window, drives it to scripted states via `PlayerActions`, and saves `screenshots/<scenario>.png` — which you can then read directly to inspect the UI/layout. Headless mode (`check`/`itest`) has no renderer, so screenshots need this windowed path. Scenarios live in `tests/integration/ScreenshotBootstrap.cs` (`Setup` switch): `title`, `overworld`, `unlocked` (region-1 unlocked → east arrow), `village` (region 1 → dialog + west arrow), `hover` (cursor over the east arrow). Add a `case` to capture any new state, then re-run and read the PNG. Driven by the `--shot <scenario>` cmdline flag (wired in `main.gd` + `TitleScreen`).

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
  title/       # Opening menu (main_scene) — New Game / Continue, 3 save slots
  main/        # Game scene — instantiates Grid + HUD (entry after title)
  overworld/   # Grid map view (14x14 isometric)
  ui/          # HUD (gold/clay/brick counters, tabs: Equip/Shop/Map/Quests)
scripts/
  autoloads/   # Global singletons: GameState, GameRunner, SceneManager
  game/        # Game systems: Economy, FlowPropagation, FlowSteadyState, RegionSystem,
               #   WaterPropagation, TileEditor, QuestSystem, GateSystem, VillageSystem, SaveSystem
  overworld/   # Grid input + map-change arrows (Grid.cs), iso math (IsoMath.cs), tile rendering (TileRenderer.cs)
  ui/          # HUD (HUD.cs) + TitleScreen.cs — all UI built in code, no layout .tscn
  main/        # Godot shim main.gd (the real entry script; Main.cs is an unused dead twin)
  utils/       # DAG, DAGNode, DAGEdge, RiverDAG, RiverNode, RiverEdge, TileCell
tests/
  unit/        # xUnit project (pure logic) — excluded from the game build
  integration/ # In-engine harness (runner, PlayerActions, fixtures, tests, ScreenshotBootstrap) — compiled into the game
resources/
  tiles/       # Tile config definitions (.tres)
assets/
  shaders/     # tile_water, tile_channel, tile_bank, tile_soil (.gdshader)
  sprites/
  audio/
  fonts/
```

**Entry flow:** `project.godot` `main_scene` is `scenes/title/title.tscn`. The title (TitleScreen.cs) handles New Game / Continue across 3 save slots (`SaveSystem`, files at `user://saves/slot_{0..2}.json`), then `SceneManager.ChangeScene` (deferred) swaps to `main.tscn`, whose `main.gd` builds Grid + HUD and calls `GameRunner.ConnectViewSignals` (deferred) to wire input. `--run-tests`/`--shot`/`--screenshot` are forwarded by the title to the game scene. Autoloads (`GameState`/`GameRunner`/`SceneManager`) persist across the swap and expose static `Instance`.

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

- `GameState.TileType` enum: `Soil, Bank, River, Channel, Stone, RiverSource, Village, Gate, GoldSource, ClaySource, Brick`
  - `Soil`: bare diggable earth · `Bank`: water-adjacent sediment that accrues gold/clay · `River`/`RiverSource`: connected water · `Channel`: transitional water · `Stone`: impassable
  - `Village`/`Gate`: region-1 village + flow-gated barrier · `GoldSource`/`ClaySource`: highlands resource sources · `Brick`: laid beside a river so it doesn't lose flow (not Bank/Soil → no loss)
- `GameState.ActiveTool` enum: `Pan, Shovel, Brick` (Grid emits Dig/Pan/Brick requests based on the active tool). Resources in `GameState`: `Gold, Clay, Shovels, Bricks, HasFurnace`.
- Tile-visual mappings live in `TileRenderer.ApplyParams`/`ShaderFor` and `IsoMath.WallColor`/`PreviewColor` — add a case in each for a new TileType.
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

## Economy, Quests, Village, Saves

- `scripts/game/Economy.cs` — per-tick gold/clay on Bank tiles. **Source-gating rule:** a material is pannable in the lowlands only when a river tile sits next to *that material's source* (`Economy.SourceFed(GoldSource/ClaySource)`, tile-adjacency). The highlands map ships with a river beside the gold source (so gold works from the start); routing a river beside the clay source turns clay on; routing it away from a source turns that material off. Also `BuyShovel`/`BuyFurnace`/`MakeBrick` (clay→brick). Do NOT reintroduce a "clay suppresses gold" swap — they are independent.
- `scripts/game/QuestSystem.cs` — a guided 7-step main quest line in `QuestSystem.Defs` (single source of truth; hint strings interpolate live constants like `ShovelCost`/`VillageFlowThreshold` so they always match the checks). `GameState.QuestsComplete` is `bool[7]`. Steps: pan gold → buy shovel → carve channel (region unlock) → find next map (village discovered) → feed clay (river beside clay source) → fire a brick → supply ≥100 flow. `CurrentObjective()` drives the HUD objective banner; the Quests tab shows ✓.
- `scripts/game/VillageSystem.cs` — sets persisted `GameState.VillageDiscovered` + emits `VillageFound` the first time the player enters region 1; HUD shows the elder dialogue and reveals the Highlands toggle + furnace shop item.
- `scripts/game/GateSystem.cs` — opens the region-1 east gate when village flow ≥ `VillageFlowThreshold` (village at `GameState.VillageRow/Col`).
- `scripts/game/SaveSystem.cs` — 3 slots over `GameState.Save/Load` (`ToSnapshot`/`ApplySnapshot`); `BaseDir` is settable so tests use a temp dir.

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
7. ~~**Save slots + title screen**~~ ✓ — opening menu, 3 slots, manual Save button
8. ~~**Guided main quest**~~ ✓ — 7-step quest line + on-screen objective banner
9. ~~**Highlands / brick pipeline**~~ ✓ — village discovery + dialogue; clay routing; furnace → brick → line the river to stop flow loss; map-change arrows
10. **Now / next** — read `TODO.md`. Remaining: more region/village content, additional resources & structures (pumps, plants, auto-collection, stronger shovels), unique per-village art, visual polish (water animation, sound, dedicated brick shader), win condition. Integration coverage is 15 tests (`mise run itest`).
