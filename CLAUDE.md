# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## About

A clicker-style game about redirecting a river carrying gold toward your civilization. Built with Godot (managed by mise — currently 4.5.2-stable).

The player shapes terrain on a grid map to guide a river from the top-left toward the bottom-right. Panning river banks earns gold; gold buys shovels; shovels reshape terrain; river flow unlocks new map regions.

## Running the Game

```bash
mise run run     # launch the game window
mise run check   # headless boot verification (exit 0 = clean)
mise run editor  # open Godot editor
```

Always run `mise run check` after any change before calling a session done.

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

<!-- rtk-instructions v2 -->
# RTK (Rust Token Killer) - Token-Optimized Commands

## Golden Rule

**Always prefix commands with `rtk`**. If RTK has a dedicated filter, it uses it. If not, it passes through unchanged. This means RTK is always safe to use.

**Important**: Even in command chains with `&&`, use `rtk`:
```bash
# ❌ Wrong
git add . && git commit -m "msg" && git push

# ✅ Correct
rtk git add . && rtk git commit -m "msg" && rtk git push
```

## RTK Commands by Workflow

### Build & Compile (80-90% savings)
```bash
rtk cargo build         # Cargo build output
rtk cargo check         # Cargo check output
rtk cargo clippy        # Clippy warnings grouped by file (80%)
rtk tsc                 # TypeScript errors grouped by file/code (83%)
rtk lint                # ESLint/Biome violations grouped (84%)
rtk prettier --check    # Files needing format only (70%)
rtk next build          # Next.js build with route metrics (87%)
```

### Test (90-99% savings)
```bash
rtk cargo test          # Cargo test failures only (90%)
rtk vitest run          # Vitest failures only (99.5%)
rtk playwright test     # Playwright failures only (94%)
rtk test <cmd>          # Generic test wrapper - failures only
```

### Git (59-80% savings)
```bash
rtk git status          # Compact status
rtk git log             # Compact log (works with all git flags)
rtk git diff            # Compact diff (80%)
rtk git show            # Compact show (80%)
rtk git add             # Ultra-compact confirmations (59%)
rtk git commit          # Ultra-compact confirmations (59%)
rtk git push            # Ultra-compact confirmations
rtk git pull            # Ultra-compact confirmations
rtk git branch          # Compact branch list
rtk git fetch           # Compact fetch
rtk git stash           # Compact stash
rtk git worktree        # Compact worktree
```

Note: Git passthrough works for ALL subcommands, even those not explicitly listed.

### GitHub (26-87% savings)
```bash
rtk gh pr view <num>    # Compact PR view (87%)
rtk gh pr checks        # Compact PR checks (79%)
rtk gh run list         # Compact workflow runs (82%)
rtk gh issue list       # Compact issue list (80%)
rtk gh api              # Compact API responses (26%)
```

### JavaScript/TypeScript Tooling (70-90% savings)
```bash
rtk pnpm list           # Compact dependency tree (70%)
rtk pnpm outdated       # Compact outdated packages (80%)
rtk pnpm install        # Compact install output (90%)
rtk npm run <script>    # Compact npm script output
rtk npx <cmd>           # Compact npx command output
rtk prisma              # Prisma without ASCII art (88%)
```

### Files & Search (60-75% savings)
```bash
rtk ls <path>           # Tree format, compact (65%)
rtk read <file>         # Code reading with filtering (60%)
rtk grep <pattern>      # Search grouped by file (75%)
rtk find <pattern>      # Find grouped by directory (70%)
```

### Analysis & Debug (70-90% savings)
```bash
rtk err <cmd>           # Filter errors only from any command
rtk log <file>          # Deduplicated logs with counts
rtk json <file>         # JSON structure without values
rtk deps                # Dependency overview
rtk env                 # Environment variables compact
rtk summary <cmd>       # Smart summary of command output
rtk diff                # Ultra-compact diffs
```

### Infrastructure (85% savings)
```bash
rtk docker ps           # Compact container list
rtk docker images       # Compact image list
rtk docker logs <c>     # Deduplicated logs
rtk kubectl get         # Compact resource list
rtk kubectl logs        # Deduplicated pod logs
```

### Network (65-70% savings)
```bash
rtk curl <url>          # Compact HTTP responses (70%)
rtk wget <url>          # Compact download output (65%)
```

### Meta Commands
```bash
rtk gain                # View token savings statistics
rtk gain --history      # View command history with savings
rtk discover            # Analyze Claude Code sessions for missed RTK usage
rtk proxy <cmd>         # Run command without filtering (for debugging)
rtk init                # Add RTK instructions to CLAUDE.md
rtk init --global       # Add RTK to ~/.claude/CLAUDE.md
```

## Token Savings Overview

| Category | Commands | Typical Savings |
|----------|----------|-----------------|
| Tests | vitest, playwright, cargo test | 90-99% |
| Build | next, tsc, lint, prettier | 70-87% |
| Git | status, log, diff, add, commit | 59-80% |
| GitHub | gh pr, gh run, gh issue | 26-87% |
| Package Managers | pnpm, npm, npx | 70-90% |
| Files | ls, read, grep, find | 60-75% |
| Infrastructure | docker, kubectl | 85% |
| Network | curl, wget | 65-70% |

Overall average: **60-90% token reduction** on common development operations.
<!-- /rtk-instructions -->