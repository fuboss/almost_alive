# Universal Animator System

Last-Updated: 2026-01-27

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
    ├── UpperBodyLayerBuilder.cs     # Work, Interact, Throw, Wave
    └── AdditiveLayerBuilder.cs      # Hit reactions

Animation/
├── UniversalAnimationController.cs  # Runtime контроллер
├── IAnimationController.cs          # Интерфейсы
└── AnimationActions.cs              # Action паттерн
```

...existing content...
