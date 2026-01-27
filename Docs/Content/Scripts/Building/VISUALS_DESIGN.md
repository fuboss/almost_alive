# Structure Visuals Management System — Design Document v2

Last-Updated: 2026-01-27

## Architecture Overview

**Centralized Module Approach:**
- `StructureVisualsModule` — singleton service, обрабатывает все структуры из ActorRegistry
- `StructureDecoration` — component на декорациях, содержит visibility rules
- **Strategy Pattern** для construction progression и animations
- **Event-driven** updates (module built, work progress changed)

...existing content...
