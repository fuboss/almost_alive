# Code Style Guide

Конвенции кода на основе проекта genes.

## Naming Conventions

### Поля класса

```csharp
// Private fields — underscore prefix
[SerializeField] private AgentBrain _agentBrain;
[Inject] private SimulationLoop _simLoop;
private float _baseNavSpeed;

// Public properties — camelCase
public AgentBrain brain => _agentBrain;
public NavMeshAgent navMeshAgent { get; private set; }
```

### Методы

```csharp
// Public — PascalCase
public void StopAndCleanPath() { }
public void AddExperience(int amount) { }

// Private — PascalCase
private void UpdateAnimation() { }
private void RefreshLinks() { }

// Event handlers — On prefix
private void OnSimSpeedChanged(SimSpeed speed) { }
private void OnEnable() { }
```

### Параметры и локальные переменные

```csharp
// camelCase
public void AddExperience(int amount) {
  var nameOf = _transientTarget?.name ?? "NULL";
  var speedNorm = navMeshAgent.velocity.magnitude / maxSpeed;
}
```

## Структура класса

Порядок членов:

1. `[Inject]` поля
2. `[SerializeField]` поля  
3. Private поля
4. Properties (public, затем private)
5. Unity lifecycle (Awake, OnEnable, OnDisable, etc.)
6. Public методы
7. Private методы

## Форматирование

### Braces

```csharp
// Методы — на новой строке НЕ ставим для коротких
public void Tick() {
  UpdateAnimation();
}

// Условия — однострочные без braces если просто
if (animController == null) return;

// Многострочные — braces обязательны
if (!shouldBeVisible) {
  layer.Hide();
}
else {
  layer.Show();
}
```

### Properties

```csharp
// Однострочные expression-bodied
public AgentBrain brain => _agentBrain;
public int tickPriority => 0;

// С логикой — полный синтаксис
public ActorDescription transientTarget {
  get => _transientTarget;
  set {
    if (_transientTarget == value) return;
    _transientTarget = value;
    Debug.Log($"Agent new target {value?.name ?? "NULL"}");
  }
}
```

### Null checks

```csharp
// Prefer null-conditional
var animController = _agentBody?.animationController;
if (animController != null && animController.animator != null) { }

// Early return pattern
if (animController == null) return;
```

## Attributes

### Odin Inspector

```csharp
[FoldoutGroup("Progression")] 
[SerializeField] private AgentExperience _experience = new();

[ShowInInspector, ReadOnly] 
private ActorDescription _transientTarget;
```

### Unity + VContainer

```csharp
[RequireComponent(typeof(NavMeshAgent))]
public class GOAPAgent : SerializedMonoBehaviour, IGoapAgent, ISimulatable {
  [Inject] private SimulationLoop _simLoop;
  [SerializeField] private AgentBrain _agentBrain;
}
```

## Interfaces

```csharp
// Explicit interface implementation когда нужно разное поведение
public AgentBrain brain => _agentBrain;
IAgentBrain IGoapAgentCore.agentBrain => _agentBrain;
```

## LINQ и коллекции

```csharp
// Предпочитать foreach для производительности
foreach (var layer in _uiLayers) {
  if (layer is T tLayer) {
    return tLayer;
  }
}

// LINQ только когда не критично к производительности
var shouldBeVisible = targetLayers.Contains(layer);
```

## Инициализация

```csharp
// Авто-инициализация в поле
[SerializeField] private AgentExperience _experience = new();

// RefreshLinks паттерн для компонентов
private void RefreshLinks() {
  if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
  if (_agentBrain == null) _agentBrain = GetComponentInChildren<AgentBrain>();
}

private void OnValidate() {
  RefreshLinks();
}
```
