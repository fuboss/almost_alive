# Правила ведения PROJECT_INDEX.md

Файл `AGENT/PROJECT_INDEX.md` — индекс файловой структуры проекта для быстрой навигации.

## Цель

- Минимизировать время поиска файлов
- Понимать структуру без сканирования всего проекта
- Знать где что лежит с первого взгляда

## Расположение

```
/Users/nikita/work/projects/genes/AGENT/PROJECT_INDEX.md
```

## Структура файла

```markdown
# Project Index

> Последнее обновление: [дата]

## Быстрые ссылки

| Категория | Путь |
|-----------|------|
| Scripts | `Assets/Content/Scripts/` |
| Prefabs | `Assets/Content/Prefabs/` |
| ScriptableObjects | `Assets/Content/Data/` |
| Editor Tools | `Assets/Content/Scripts/Editor/` |

## Структура Scripts

### AI
```
Assets/Content/Scripts/AI/
├── GOAP/
│   ├── Agent/          — Компоненты агента (GOAPAgent, AgentBrain, AgentBody)
│   ├── Actions/        — Действия агента
│   ├── Goals/          — Цели агента
│   └── Planner/        — Планировщик
├── Camp/               — Логика лагеря
└── Craft/              — Система крафта
```

### Core
```
Assets/Content/Scripts/Core/
├── Simulation/         — SimulationLoop, SimulationTimeController
├── DI/                 — VContainer scopes
└── Utils/              — Утилиты
```

### Game
```
Assets/Content/Scripts/Game/
├── Work/               — Система работ
├── Inventory/          — Инвентарь
└── Actors/             — ActorDescription, ActorId
```

### UI
```
Assets/Content/Scripts/Ui/
├── UiModule.cs         — Управление слоями
├── Layers/             — UILayer наследники
└── Components/         — UI компоненты
```

## Ключевые классы

| Класс | Путь | Описание |
|-------|------|----------|
| GOAPAgent | `Scripts/AI/GOAP/Agent/GOAPAgent.cs` | Главный компонент AI агента |
| UiModule | `Scripts/Ui/UiModule.cs` | Управление UI слоями |
| SimulationLoop | `Scripts/Core/Simulation/SimulationLoop.cs` | Игровой цикл симуляции |

## ScriptableObjects

| Тип | Путь | Описание |
|-----|------|----------|
| AgentStatSetSO | `Data/Agents/` | Наборы статов агентов |
| RecipeSO | `Data/Recipes/` | Рецепты крафта |

## Editor Tools

| Окно | Путь | Меню |
|------|------|------|
| RecipeEditor | `Scripts/Editor/RecipeEditor.cs` | Tools/Recipe Editor |
```

## Правила обновления

### Когда обновлять

1. **Создан новый модуль/папка** — добавить в структуру
2. **Создан ключевой класс** — добавить в "Ключевые классы"
3. **Создан новый тип SO** — добавить в "ScriptableObjects"
4. **Создан Editor Tool** — добавить в "Editor Tools"
5. **Удалена/перемещена папка** — обновить структуру

### Что НЕ индексировать

- Отдельные файлы (кроме ключевых)
- Автогенерируемые файлы
- Временные файлы
- Файлы плагинов/сторонних библиотек

### Формат даты

```
> Последнее обновление: 2024-01-15
```

## Поиск по индексу

Если нужно найти что-то:

1. Открыть PROJECT_INDEX.md
2. Найти категорию (AI, Core, UI, etc.)
3. Найти подкатегорию или ключевой класс
4. Перейти по указанному пути

Если не нашёл — добавить после обнаружения!
