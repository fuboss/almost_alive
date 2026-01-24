# Universal Animator System

## Обзор архитектуры

Система генерации и управления AnimatorController для агентов.

## Структура файлов

```
Editor/AnimatorGenerator/
├── AnimatorGeneratorConfig.cs       # ScriptableObject с настройками
├── AnimatorGeneratorService.cs      # Главный сервис генерации
├── AnimationClipProvider.cs         # Загрузка и кэширование клипов
├── BlendTreeFactory.cs              # Фабрика BlendTree
├── TransitionFactory.cs             # Фабрика переходов
├── ClipValidator.cs                 # Валидация наличия клипов
├── ILayerBuilder.cs                 # Интерфейс + контекст
├── UniversalAnimatorGeneratorWindow.cs  # UI (Odin Window)
└── Builders/
    ├── BaseLayerBuilder.cs          # Locomotion, Jump, Dodge, Death
    ├── CombatLayerBuilder.cs        # Attack, Block, Aim
    ├── UpperBodyLayerBuilder.cs     # Work, Interact, Social
    └── AdditiveLayerBuilder.cs      # Hit reactions

Animation/
├── UniversalAnimationController.cs  # Runtime контроллер
├── IAnimationController.cs          # Интерфейсы
└── AnimationActions.cs              # Action паттерн
```

## Слои аниматора

| Layer | Index | Weight | Mask | Назначение |
|-------|-------|--------|------|------------|
| Base | 0 | 1.0 | - | Locomotion, Jump, Crouch, Death |
| Combat | 1 | 1.0 | - | Attack, Block, Aim, Shoot |
| UpperBody | 2 | 1.0 | UpperBodyMask | Work, Interact, Throw, Wave |
| Additive | 3 | 0.5 | - | Hit reactions |

## Параметры

### Float
- `Speed` — скорость для locomotion blend (0-1)
- `Direction` — направление strafe (-1 to 1)
- `Vertical` — forward/backward (-1 to 1)
- `AimAngle` — угол прицеливания
- `ToolType` — тип инструмента для Work BlendTree (0-6)

### Int
- `WeaponType` — тип оружия (0-7)
- `AttackIndex` — индекс combo (0-3)
- `SitType` — тип сидения (0-2)
- `DeathType` — вариант смерти

### Bool
- `IsGrounded`, `IsCrouching`, `IsSneaking`
- `IsAiming`, `IsBlocking`, `IsWorking`
- `IsSitting`, `IsLying`, `IsDead`, `InCombat`

### Triggers
- `Attack`, `Dodge`, `Jump`, `Hit`, `Die`
- `Interact`, `UseItem`, `Throw`, `Spawn`
- `Cheer`, `Wave`, `Reload`, `StartWork`

## ToolType значения

| Value | Enum | Animation |
|-------|------|-----------|
| 0 | None | Idle_A |
| 1 | Axe | Chopping |
| 2 | Pickaxe | Pickaxing |
| 3 | Shovel | Digging |
| 4 | Hammer | Hammering |
| 5 | Saw | Sawing |
| 6 | FishingRod | Fishing_Idle |

## Использование в стратегиях

```csharp
// В OnStart()
_animations?.CutTree();  // или StartWork(ToolAnimationType.Axe)

// В OnStop()
_animations?.StopWork();
```

## Известные проблемы / TODO

- [ ] **Debug:** Проверить что UpperBody layer корректно проигрывает Work анимации
- [x] **Preview Mode:** Добавить режим превью для тестирования анимаций
- [ ] **Avatar Mask:** Убедиться что UpperBodyMask.mask применён к слою

## Preview Controller

Компонент `AnimationPreviewController` для тестирования анимаций в сцене.

### Использование

1. Добавь компонент на GameObject с `UniversalAnimationController`
2. Или на любой объект и назначь ссылку на контроллер
3. Используй Inspector для переключения состояний

### Сценарии (Presets)

| Сценарий | Описание |
|----------|----------|
| Idle | Стоит на месте |
| Walk | Медленная ходьба (speed=0.3) |
| Run | Бег (speed=1.0) |
| WorkAxe | Рубка топором |
| WorkPickaxe | Добыча киркой |
| WorkAndWalk | Рубка + ходьба (тест UpperBody mask) |
| Combat | Боевой режим |
| CombatCombo | Боевой режим + атака |

### Кнопки

**Work:** Start Work, Stop Work
**Combat:** Enter/Exit Combat, Attack, Block, Aim
**Triggers:** Jump, Dodge, Hit, Die, Interact, Use Item, Throw, Wave
**State:** Crouch, Sit Chair, Sit Floor, Stand Up, Resurrect, Reset All

### Debug Info

В Inspector отображается:
- `CurrentState` — текущее состояние Base Layer
- `UpperBodyState` — текущее состояние UpperBody Layer
- `IsWorking` — флаг работы
- `CurrentToolType` — текущий тип инструмента

## Debug логи

В `UniversalAnimationController` добавлены логи:
- `[Animation] SetToolType: {type} ({value})`
- `[Animation] SetWorking: {bool}`
- `[Animation] StartWork: {tool}`
- `[Animation] StopWork`

## Команды

- **Генерация:** `Tools > Animation > Generate Universal Animator`
- **Mask:** Кнопка "Generate Avatar Mask" в окне генератора

## История изменений

### 2026-01-24
- Рефакторинг генератора по SOLID
- Добавлены ILayerBuilder для расширяемости
- ToolType изменён с Int на Float (требование BlendTree)
- Исправлен ClearController (не удаляет StateMachine)
- Добавлены debug логи для Work анимаций

