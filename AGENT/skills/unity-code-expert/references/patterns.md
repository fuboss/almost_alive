# Паттерны и архитектура

## Cache-Friendly System Pattern

Основной паттерн проекта — один модуль/система управляет множеством связанных сущностей.

### Структура системы

```csharp
public class EntityModule : ITickable {
  [Inject] private IEnumerable<Entity> _entities;
  
  // Индексы для быстрого доступа
  private Dictionary<int, Entity> _byId;
  private Dictionary<EntityType, List<Entity>> _byType;
  
  public void Initialize() {
    _byId = _entities.ToDictionary(e => e.id);
    _byType = _entities.GroupBy(e => e.type)
                       .ToDictionary(g => g.Key, g => g.ToList());
  }
  
  public Entity GetById(int id) => _byId.TryGetValue(id, out var e) ? e : null;
  
  public void Tick() {
    foreach (var entity in _entities) {
      if (entity.isActive) {
        entity.OnUpdate();
      }
    }
  }
}
```

### Принципы

1. **Централизованное управление** — модуль знает обо всех своих сущностях
2. **Индексация** — быстрый lookup по ID, типу, состоянию
3. **Batch операции** — обновление в едином цикле
4. **Минимум GetComponent** — кешировать ссылки при инициализации

## VContainer DI Pattern

### Регистрация

```csharp
public class GameLifetimeScope : LifetimeScope {
  protected override void Configure(IContainerBuilder builder) {
    // Modules
    builder.Register<UiModule>(Lifetime.Singleton).AsImplementedInterfaces();
    builder.Register<SimulationLoop>(Lifetime.Singleton);
    
    // MonoBehaviours from scene
    builder.RegisterComponentInHierarchy<GOAPAgent>();
    
    // Collections
    builder.Register<IEnumerable<UILayer>>(c => 
      FindObjectsOfType<UILayer>(), Lifetime.Singleton);
  }
}
```

### Injection

```csharp
public class MySystem {
  [Inject] private readonly SimulationLoop _simLoop;
  [Inject] private readonly IEnumerable<Entity> _entities;
  
  // Constructor injection для обязательных зависимостей
  public MySystem(RequiredService required) { }
}
```

## Simulation Pattern

### ISimulatable

```csharp
public interface ISimulatable {
  int tickPriority { get; }
  void Tick();           // Каждый кадр (анимация, визуал)
  void SimTick(float simDeltaTime);  // Симуляция (логика)
}
```

### Использование

```csharp
public class GOAPAgent : ISimulatable {
  public int tickPriority => 0;
  
  public void Tick() {
    UpdateAnimation();  // Визуальное обновление
  }
  
  public void SimTick(float simDeltaTime) {
    _agentBody.TickStats(simDeltaTime);  // Логика
    _agentBrain.Tick(simDeltaTime);
  }
}
```

## UI Layer Pattern

```csharp
public abstract class UILayer : MonoBehaviour {
  public bool isVisible { get; private set; }
  
  public virtual void Show() {
    isVisible = true;
    gameObject.SetActive(true);
  }
  
  public virtual void Hide() {
    isVisible = false;
    gameObject.SetActive(false);
  }
  
  public virtual void OnUpdate() { }
}
```

### UiModule управление

```csharp
public class UiModule : ITickable {
  public void SetLayers(params UILayer[] targetLayers) {
    foreach (var layer in _uiLayers) {
      var shouldBeVisible = targetLayers.Contains(layer);
      if (layer.isVisible != shouldBeVisible) {
        if (shouldBeVisible) layer.Show();
        else layer.Hide();
      }
    }
  }
}
```

## Component Caching Pattern

```csharp
public class MyComponent : MonoBehaviour {
  // Кешированные ссылки
  public NavMeshAgent navMeshAgent { get; private set; }
  
  private void Awake() {
    RefreshLinks();
  }
  
  private void OnValidate() {
    RefreshLinks();  // Для Editor
  }
  
  private void RefreshLinks() {
    if (navMeshAgent == null) 
      navMeshAgent = GetComponent<NavMeshAgent>();
  }
}
```

## Event Subscription Pattern

```csharp
private void OnEnable() {
  _simLoop?.Register(this);
  if (_simTime != null) 
    _simTime.OnSpeedChanged += OnSimSpeedChanged;
}

private void OnDisable() {
  _simLoop?.Unregister(this);
  if (_simTime != null) 
    _simTime.OnSpeedChanged -= OnSimSpeedChanged;
}
```

## Interface Segregation

```csharp
// Разные интерфейсы для разных аспектов агента
public interface IGoapAgentCore {
  IAgentBrain agentBrain { get; }
  void StopAndCleanPath();
}

public interface ITransientTargetAgent {
  ActorDescription transientTarget { get; set; }
  int transientTargetId { get; }
}

public interface IInventoryAgent {
  ActorInventory inventory { get; }
}

// Класс реализует нужные интерфейсы
public class GOAPAgent : IGoapAgent, ISimulatable { }
```
