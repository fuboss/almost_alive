# Чеклист оптимизации производительности

## Update / Tick

### Критично

- [ ] **Нет GetComponent в Update** — кешировать в Awake/Start
- [ ] **Нет Find* в Update** — использовать индексы или DI
- [ ] **Нет LINQ в горячих путях** — foreach вместо .Where().Select()
- [ ] **Нет new в Update** — переиспользовать объекты, использовать pools

### Проверить

- [ ] **Early return** — выходить рано если нечего делать
- [ ] **Условная логика** — пропускать неактивные/невидимые объекты
- [ ] **Частота обновления** — не всё нужно каждый кадр

```csharp
// Плохо
void Update() {
  var targets = FindObjectsOfType<Target>()
    .Where(t => t.isActive)
    .OrderBy(t => Distance(t))
    .ToList();
}

// Хорошо
void Update() {
  if (!_needsUpdate) return;
  
  foreach (var target in _cachedTargets) {
    if (!target.isActive) continue;
    ProcessTarget(target);
  }
}
```

## Память и GC

### Критично

- [ ] **Нет boxing** — избегать object параметры с value types
- [ ] **String interpolation осторожно** — не в Update, кешировать если нужно
- [ ] **Нет лямбд в горячих путях** — создают аллокации

### Проверить

- [ ] **List capacity** — задавать начальный размер если известен
- [ ] **StringBuilder** — для множественной конкатенации
- [ ] **Pooling** — для часто создаваемых объектов

```csharp
// Плохо
void Update() {
  Debug.Log($"Position: {transform.position}");  // Аллокация каждый кадр
  _events.Add(() => DoSomething());  // Аллокация лямбды
}

// Хорошо
void Update() {
  if (_debugEnabled) {
    _debugBuilder.Clear();
    _debugBuilder.Append("Position: ");
    _debugBuilder.Append(transform.position);
    Debug.Log(_debugBuilder);
  }
}
```

## Unity-специфичное

### Transform

- [ ] **Кешировать transform** — `_transform = transform` в Awake
- [ ] **Batch изменения** — SetPositionAndRotation вместо отдельных
- [ ] **localPosition vs position** — local быстрее если родитель не двигается

### Physics

- [ ] **NonAlloc методы** — RaycastNonAlloc, OverlapSphereNonAlloc
- [ ] **Layer masks** — ограничивать слои для проверок
- [ ] **Отключать коллайдеры** — когда не нужны

```csharp
// Плохо
var hits = Physics.RaycastAll(origin, direction);

// Хорошо
private RaycastHit[] _hitBuffer = new RaycastHit[10];

void CheckHits() {
  int count = Physics.RaycastNonAlloc(origin, direction, _hitBuffer, maxDist, _layerMask);
  for (int i = 0; i < count; i++) {
    ProcessHit(_hitBuffer[i]);
  }
}
```

### NavMesh

- [ ] **Не вызывать SetDestination каждый кадр** — только при изменении цели
- [ ] **remainingDistance проверять осторожно** — может быть дорогим
- [ ] **pathPending** — проверять перед новым путём

## Коллекции

### Выбор структуры

| Операция | List | Dictionary | HashSet |
|----------|------|------------|---------|
| Добавление | O(1)* | O(1) | O(1) |
| Поиск по индексу | O(1) | - | - |
| Поиск по ключу | O(n) | O(1) | O(1) |
| Contains | O(n) | O(1) | O(1) |
| Удаление | O(n) | O(1) | O(1) |

### Рекомендации

- [ ] **Dictionary для lookup по ID** — вместо List.Find
- [ ] **HashSet для Contains** — вместо List.Contains
- [ ] **Indices** — поддерживать множественные индексы если нужны разные lookups

```csharp
// Плохо
public Entity GetById(int id) {
  return _entities.FirstOrDefault(e => e.id == id);  // O(n)
}

// Хорошо
private Dictionary<int, Entity> _byId;

public Entity GetById(int id) {
  return _byId.TryGetValue(id, out var e) ? e : null;  // O(1)
}
```

## Profiler маркеры

Для сложных систем добавлять маркеры:

```csharp
using Unity.Profiling;

static readonly ProfilerMarker s_UpdateMarker = new("MySystem.Update");

void Update() {
  using (s_UpdateMarker.Auto()) {
    // код
  }
}
```
