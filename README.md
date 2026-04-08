# Dungeon of Shadows

Action Roguelike with procedural dungeons, real-time combat, magic system, and permadeath.
Built with C# 12, Raylib-cs, and NativeAOT.

<!-- ![Gameplay](docs/screenshots/gameplay.png) -->

## Features

- **Procedural Dungeons** — BSP-generated floors with 8-14 rooms, L-shaped corridors, fog of war, decorations, and floor scaling every 5 levels
- **Real-time Combat** — Melee attacks, dash with i-frames, 3 enemy types with A* pathfinding
- **Magic System** — 6 spells (Magic Bolt, Fireball, Frost Nova, Chain Lightning, Shadow Step, Heal), mana, cooldowns, status effects (Burn, Slow)
- **Loot & Equipment** — Swords, armor, rings, amulets, potions, spell scrolls. Rarity tiers, inventory, quick slots
- **Pixel Art** — 16x16 sprites, animated characters, Y-sorted depth rendering, fog of war

## Tech Stack

| | |
|---|---|
| Language | C# 12 / .NET 8 |
| Graphics | [Raylib-cs](https://github.com/ChristopherRae/Raylib-cs) 6.1.1 |
| DI | Microsoft.Extensions.DependencyInjection |
| Compilation | NativeAOT (single binary, no runtime) |
| Architecture | Custom ECS (Entity Component System) |
| Data | JSON configs with AOT-compatible source generators |

## Build & Run

**Requirements:** .NET 8 SDK

```bash
# Development
dotnet run --project DungeonOfShadows.csproj

# Release (NativeAOT)
dotnet publish -c Release -r win-x64 -p:PublishAot=true
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
dotnet publish -c Release -r osx-arm64 -p:PublishAot=true
```

## Controls

| Key | Action |
|-----|--------|
| WASD | Move |
| LMB | Melee attack |
| RMB | Cast spell |
| Z / X / Scroll | Switch spell slot |
| Shift | Dash |
| E / Space | Descend stairs |
| 1-4 | Use quick slot item |
| Tab | Inventory |
| F3 | Debug overlay |

## Architecture

Lightweight ECS with DI-based system lifecycle:

```
Start() → Tick(dt) → Dispose()
   ↑          ↑          ↑
 once    every frame   on exit
```

Systems are independent — they communicate through shared data (`World` components, `GameContext` flags, event queues). Registration order in `ServiceRegistration.cs` defines tick order.

```
src/
├── Core/           — Game loop, config, DI, asset loading
├── ECS/
│   ├── Core/       — World, ComponentStore, lifecycle interfaces
│   ├── Player/     — Input handling
│   ├── Physics/    — Movement, collisions
│   ├── Combat/     — Melee, dash, AI, health, damage
│   ├── Magic/      — Spells, projectiles, status effects, mana
│   ├── Items/      — Inventory, equipment, loot, chests
│   ├── Exploration/ — Dungeon generation, FOV, floor transitions
│   └── Rendering/  — Sprites, tiles, animations, HUD, UI
├── UI/             — Layout, draw, theme, input routing
└── Dungeon/        — Tilemap, BSP, room placement, corridors
```

## Roadmap

| Phase | Status |
|-------|--------|
| Core (window, movement, camera) | Done |
| Dungeon Generation (BSP, FOV, minimap) | Done |
| Combat (melee, AI, dash) | Done |
| Items (loot, inventory, equipment) | Done |
| Magic (spells, projectiles, effects) | Done |
| Progression (XP, leveling, perks) | Planned |
| Bosses (unique encounters, arenas) | Planned |
| Polish (audio, balance, particles) | Planned |

## CI/CD

- **PR Review** — Automated code review on pull requests via GitHub Models API
- **Dev Build** — NativeAOT publish + version tag on PR merge to `develop`

## License

All rights reserved.
