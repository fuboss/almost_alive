### DebugPanel (NEW)
```
Assets/Content/Scripts/DebugPanel/
├── Actions/           — SpawnActorAction, SpawnStructureAction, DestroyActorAction, etc.
├── DebugModule.cs     — Main controller, action registry, input handling
├── DebugPanelUI.cs    — UI building, category dropdowns
├── DebugActionRegistry.cs — Action storage by category
├── DebugEnums.cs      — DebugState, DebugCategory, DebugActionType
└── IDebugAction.cs    — Action interface
```

# Project Index

> Последнее обновление: 2025-01-23

## Быстрые ссылки

| Категория | Путь |
|-----------|------|
| Scripts | `Assets/Content/Scripts/` |
| Prefabs | `Assets/Content/Prefabs/` |
| Configs (SO) | `Assets/Content/Configs/` |
| UI Assets | `Assets/Content/UI/` |
| Scenes | `Assets/Content/Scenes/` |
| Editor Tools | `Assets/Content/Scripts/Editor/` |
| Technical Docs | `AGENT/Docs/` |
| Game Design Docs | `AGENT/GD_DOC/` |
| AI Notes | `AGENT/AI_NOTES.md` |

## Documentation

### Technical Docs (`AGENT/Docs/`)

| File | Topic |
|------|-------|
| ARCHITECTURE.md | Core systems, patterns |
| GOAP.md | Beliefs, actions, planning |
| NAVIGATION.md | NavMesh, stuck detection |
| ANIMALS.md | Animal agents, herding |
| INTERFACE_DECOMPOSITION.md | Agent interfaces |
| WORLD_GENERATION.md | Biomes, terrain, scatters |
| INVENTORY_CRAFT.md | Items, storage, recipes |
| CAMP.md | Camp system (legacy) |
| REFACTORING_IDEAS.md | Pending refactoring |
| UNITY_CONVENTIONS.md | Unity folder restrictions, gotchas |

### Game Design Docs (`AGENT/GD_DOC/`)

| File | Topic | Status |
|------|-------|--------|
| GDD.md | Project overview | 🟢 Active |
| BUILDING.md | Smart Blueprints system | 🟡 Design |
| BUILDING_DEVPLAN.md | Building dev plan | 🟡 Phase 1 |
| COLONISTS.md | Colonist systems | 🔴 Not started |
| STORYTELLER.md | Event system | 🔴 Not started |
| IDEAS_GD.md | GD proposals | 🟢 Active |
| IDEAS_NIKITA.md | Your ideas | 🟢 Active |

## Структура Scripts

### AI
```
Assets/Content/Scripts/AI/
├── GOAP/               — GOAP система (агенты, действия, цели, планировщик)
│   ├── Beliefs/Structure/  — Structure beliefs (NEW)
│   └── Strategies/Structure/ — Structure strategies (NEW)
├── Animals/            — AI животных
├── Camp/               — Логика лагеря (DEPRECATED)
├── Craft/              — Система крафта (AI часть)
├── Navigation/         — Навигация
└── Utility/            — AI утилиты
```

### Building (NEW)
```
Assets/Content/Scripts/Building/
├── Data/               — StructureDefinitionSO, ModuleDefinitionSO, ConstructionData, enums
├── Editor/            — Custom Editors
├── EditorUtilities/   — StructureFoundationBuilder
├── Runtime/           — Structure, UnfinishedStructure, Slot, Module, WallSegment, EntryPoint
├── Services/          — StructuresModule, PlacementService, ConstructionService
└── BuildingConstants.cs
```

### Core
```
Assets/Content/Scripts/Core/
├── Simulation/         — SimulationLoop, SimulationTimeController
├── Environment/        — Окружение
└── (root)              — IPrefabFactory, StaticReset система
```

### Game
```
Assets/Content/Scripts/Game/
├── Camera/             — Камера
├── Craft/              — Система крафта (игровая часть)
├── Interaction/        — Взаимодействия
├── Storage/            — Хранилища
├── Trees/              — Деревья
├── Work/               — Система работ
└── AgentContainerModule.cs — Контейнер агентов
```

### UI
```
Assets/Content/Scripts/Ui/
└── UiModule.cs         — Управление UI слоями
```

### Utility
```
Assets/Content/Scripts/Utility/
└── (утилиты общего назначения)
```

### Other
```
Assets/Content/Scripts/
├── Animation/          — Анимационные скрипты
├── DebugPanel/         — Дебаг панель
├── Descriptors/        — Дескрипторы
├── Docs/               — Документация в коде
├── Editor/             — Editor скрипты и окна
├── World/              — Мир
└── GameScope.cs        — VContainer главный scope
```

## Ключевые классы

| Класс | Путь | Описание |
|-------|------|----------|
| GOAPAgent | `Scripts/AI/GOAP/Agent/GOAPAgent.cs` | Главный компонент AI агента |
| AgentBrain | `Scripts/AI/GOAP/Agent/AgentBrain.cs` | Мозг агента (планирование) |
| AgentBody | `Scripts/AI/GOAP/Agent/AgentBody.cs` | Тело агента (визуал, статы) |
| UiModule | `Scripts/Ui/UiModule.cs` | Управление UI слоями |
| GameScope | `Scripts/GameScope.cs` | VContainer DI scope |

## Configs (ScriptableObjects)

| Тип | Путь | Описание |
|-----|------|----------|
| AgentStatSetSO | `Configs/` | Наборы статов агентов |
| (TODO) | — | Добавлять по мере обнаружения |

## Editor Tools

| Окно | Путь | Описание |
|------|------|----------|
| (TODO) | `Scripts/Editor/` | Добавлять по мере обнаружения |

---

*Обновлять при изменении структуры проекта. См. `AGENT/skills/unity-code-expert/references/project-index-guide.md`*
