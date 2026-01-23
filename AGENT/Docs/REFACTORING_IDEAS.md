# Refactoring Ideas

Заметки о потенциальных улучшениях кодовой базы.

---

## High Priority

### Memory System - Unified Nearest Query
**Проблема:** Разные места используют разные способы поиска "nearest":
- `AgentMemory.GetNearest()` — Euclidean
- `MemorySearcher.GetNearest()` — теперь NavMesh-aware
- `PathCostEvaluator.GetNearestReachable()` — прямой вызов

**Решение:** Унифицировать через AgentMemory с опциональным NavMesh:
```csharp
// AgentMemory.cs
public MemorySnapshot GetNearest(
  Vector3 position, 
  string[] tags,
  NavMeshAgent navAgent = null,  // если передан - использует NavMesh
  Func<MemorySnapshot, bool> predicate = null
);
```

### Strategy Base Class Patterns
**Проблема:** Много стратегий имеют одинаковый boilerplate:
- SimTimer initialization/disposal
- Animation controller access
- Target validation

**Решение:** Расширить `AgentStrategy` базовыми хелперами:
```csharp
public abstract class AgentStrategy {
  protected SimTimer CreateTimer(float duration, Action onComplete);
  protected void SafePlayAnimation(Action<AnimationController> action);
  protected bool ValidateTarget(out ActorDescription target);
}
```

---

## Medium Priority

### Sensor System Consolidation
**Проблема:** VisionSensor и InteractionSensor имеют похожую структуру (OnActorEntered/Exited).

**Решение:** Общий базовый класс `ActorSensor<T>`.

### Belief Caching
**Проблема:** Beliefs пересчитываются каждый раз при проверке.

**Решение:** Добавить dirty flag + cache для тяжёлых beliefs.

---

## Low Priority

### Registry<T> Query Optimizations
**Проблема:** `Registry<T>.GetAll().Where(...)` создаёт аллокации.

**Решение:** Добавить `Query(Func<T, bool>)` с pooled list.

### WorldGrid Cell Size Configuration
**Проблема:** Hardcoded cell size = 1m.

**Решение:** Сделать конфигурируемым через SO.

---

## Done ✓

- [x] ClearPlan() extraction in AgentBrain (was duplicated in 3 places)
- [x] PathCostEvaluator centralized NavMesh queries
