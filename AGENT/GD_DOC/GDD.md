# Almost Alive — Game Design Document

> *A 3D colony simulation where you witness almost-living beings survive, struggle, and tell their stories.*

## Vision Statement

Create a deep, autonomous colony simulation where colonists feel genuinely alive. Players experience emergent narratives driven by believable AI behaviors and a dynamic storyteller system. The goal is emotional engagement through observation, not constant optimization.

## Core Pillars

1. **Autonomy** — Colonists make believable, observable decisions based on needs, personality, and relationships
2. **Emergent Stories** — Gameplay generates emotional narratives organically
3. **Observable Life** — Watching the colony is inherently enjoyable
4. **Storyteller-Driven** — Dynamic events create pacing and challenge

## Technical Foundation

| Aspect | Choice |
|--------|--------|
| Engine | Unity 6.3 LTS |
| Render Pipeline | URP |
| Terrain | Procedural (hills, valleys) |
| Spatial Grid | World divided into grid cells on uneven terrain |
| Navigation | NavMesh (grid available for spatial queries) |
| Camera | Top-down primary + cinematic observation modes |

## Inspirations

- **RimWorld** — Core colony sim mechanics, storyteller system, emergent drama
- [Add more as project develops]

## Design Principles

- **Pareto (80/20)**: Prioritize high-impact, low-complexity features
- **Depth over Breadth**: Few systems that interact richly > many shallow systems
- **Show, Don't Tell**: Convey information through agent behavior and world state

---

## GDD Index

| Document | Description | Status |
|----------|-------------|--------|
| [BUILDING.md](BUILDING.md) | Smart Blueprints building system | 🟡 Design phase |
| [BUILDING_DEVPLAN.md](BUILDING_DEVPLAN.md) | Building implementation plan | 🟡 Phase 1 |
| [CORE_LOOP.md](CORE_LOOP.md) | Core gameplay loop | 🔴 Not started |
| [COLONISTS.md](COLONISTS.md) | Colonist systems, needs, AI | 🔴 Not started |
| [STORYTELLER.md](STORYTELLER.md) | Event system, difficulty | 🔴 Not started |
| [UI.md](UI.md) | Interface design | 🔴 Not started |
| [IDEAS_GD.md](IDEAS_GD.md) | GD's idea proposals | 🟢 Active |
| [IDEAS_NIKITA.md](IDEAS_NIKITA.md) | Nikita's ideas | 🟢 Active |

---

*Last updated: [auto-update on edit]*
