# Tree Fall System

> Система физического падения деревьев с импакт-уроном и эффектами

## Overview

Когда дерево срублено, оно не уничтожается мгновенно, а становится физическим объектом, эффектно падает, наносит урон объектам при столкновении, и превращается в бревно для дальнейшей обработки.

---

## Architecture

### Компоненты системы

```
TreeModule (orchestrator)
├── TreeFallConfigSO (настройки)
├── ITreeFallDirectionStrategy (стратегия направления)
├── TreeColliderCache (кеш MeshCollider)
├── FallingTreeBehaviour (MonoBehaviour на падающем дереве)
└── EffectsModule (партиклы, эффекты)

IImpactReceiver (интерфейс для получателей урона)
├── GOAPAgent
├── AnimalAgent  
├── Module (building)
└── Structure
```

### Flow

```
ChopDownTree()
  → TreeModule.StartTreeFall(tree, chopper)
    → Disable NavMeshObstacle (если есть)
    → Get/Create MeshCollider (из кеша)
    → Add Rigidbody (mass из TreeTag)
    → Add FallingTreeBehaviour
    → Calculate fall direction (через ITreeFallDirectionStrategy)
    → Apply initial torque/impulse
    
FallingTreeBehaviour.Update()
  → Track velocity, angle
  → OnCollisionEnter → Check impact receivers → Deal damage
  → Detect "settled" state (velocity < threshold, angle > threshold)
    → OnSettled:
      1. EffectsModule.SpawnLeafBurst(crownPosition)
      2. Replace mesh with procedural log
      3. Spawn LogActor (new actor with LogTag)
      4. Destroy original tree actor
```

---

## New Files

### 1. TreeFallConfigSO.cs
**Path:** `Assets/Content/Scripts/Game/Trees/TreeFallConfigSO.cs`

```csharp
[CreateAssetMenu(fileName = "TreeFallConfig", menuName = "Genes/Config/Tree Fall")]
public class TreeFallConfigSO : ScriptableObject {
  [Header("Physics")]
  public float defaultMass = 50f;
  public float initialTorqueMultiplier = 10f;
  public float angularDrag = 0.5f;
  public float drag = 0.1f;
  
  [Header("Impact Detection")]
  public float settledVelocityThreshold = 0.3f;    // м/с
  public float settledAngularThreshold = 0.2f;     // рад/с  
  public float settledAngleFromVertical = 45f;     // градусы (дерево "лежит")
  public float settledCheckDelay = 0.5f;           // секунды после первого импакта
  
  [Header("Damage")]
  public float impactDamageMultiplier = 1f;        // damage = velocity * mass * multiplier
  public float minVelocityForDamage = 2f;          // м/с
  public LayerMask impactLayers;
  
  [Header("Log Generation")]
  public float logDiameterRatio = 0.3f;            // относительно ширины дерева
  public int logSides = 6;                          // гексагональное сечение (не катится)
  public string logActorKey = "log_0";
  
  [Header("Effects")]
  public GameObject leafBurstPrefab;
  public float leafBurstDuration = 2f;
}
```

### 2. ITreeFallDirectionStrategy.cs
**Path:** `Assets/Content/Scripts/Game/Trees/Strategies/ITreeFallDirectionStrategy.cs`

```csharp
public interface ITreeFallDirectionStrategy {
  Vector3 GetFallDirection(Transform tree, Vector3 chopperPosition);
}

// Implementations:
// - RandomFallStrategy (случайное направление)
// - AwayFromChopperStrategy (от рубщика)
// - TowardsBuildingsStrategy (к зданиям колонистов с настраиваемой вероятностью)
// - CompositeFallStrategy (комбинация с весами)
```

### 3. FallingTreeBehaviour.cs  
**Path:** `Assets/Content/Scripts/Game/Trees/FallingTreeBehaviour.cs`

MonoBehaviour, управляющий физикой падения:
- Отслеживает состояние (falling, settling, settled)
- Обрабатывает OnCollisionEnter для урона
- Детектит "settled" состояние
- Вызывает callback при завершении падения

### 4. TreeColliderCache.cs
**Path:** `Assets/Content/Scripts/Game/Trees/TreeColliderCache.cs`

```csharp
public static class TreeColliderCache {
  private static Dictionary<string, Mesh> _colliderMeshes = new();
  
  public static Mesh GetOrCreateColliderMesh(string actorKey, MeshFilter sourceMesh);
  public static void Clear();
}
```

### 5. ProceduralLogGenerator.cs
**Path:** `Assets/Content/Scripts/Game/Trees/ProceduralLogGenerator.cs`

Генератор процедурного меша бревна:
- Гексагональное сечение (6 сторон - не катится)
- Размер по bounds оригинального дерева
- Возвращает Mesh + настраивает MeshCollider

### 6. IImpactReceiver.cs
**Path:** `Assets/Content/Scripts/Game/Interaction/IImpactReceiver.cs`

```csharp
public interface IImpactReceiver {
  void ReceiveImpact(float damage, Vector3 impactPoint, Vector3 impactDirection);
  bool canReceiveImpact { get; }
}
```

### 7. LogTag.cs
**Path:** `Assets/Content/Scripts/Descriptors/Tags/LogTag.cs`

```csharp
public class LogTag : TagDefinition {
  public string plankActorID = "plank_0";
  public int plankYield = 2;
  public float workRequired = 8f;
  public override string Tag => AI.Tag.LOG;
}
```

---

## EffectsModule Architecture

### Предметная область

EffectsModule управляет жизненным циклом визуальных эффектов:
- Спавн эффектов (партиклы, VFX)
- Пулинг для переиспользования
- Автоматическая очистка по времени/условию
- Привязка к объектам (follow target)

### API Design

```csharp
public class EffectsModule : IInitializable, IDisposable {
  // Основные методы
  UniTask<EffectHandle> SpawnEffect(EffectRequest request);
  void StopEffect(EffectHandle handle);
  void StopAllEffects();
  
  // Быстрые хелперы
  UniTask<EffectHandle> SpawnAt(GameObject prefab, Vector3 position, float duration);
  UniTask<EffectHandle> SpawnAttached(GameObject prefab, Transform parent, float duration);
  
  // Стратегии завершения
  // - ByDuration (через N секунд)
  // - ByParticleComplete (когда партиклы закончатся)
  // - Manual (ручное управление)
  // - ByCondition (Func<bool> predicate)
}

public struct EffectRequest {
  public GameObject prefab;
  public Vector3 position;
  public Quaternion rotation;
  public Transform parent;           // optional, для attached эффектов
  public float duration;             // 0 = manual control
  public IEffectLifetimeStrategy lifetimeStrategy;
  public Action<EffectHandle> onComplete;
}

public struct EffectHandle {
  public int id;
  public GameObject instance;
  public bool isValid { get; }
}
```

### Стратегии (IEffectLifetimeStrategy)

```csharp
public interface IEffectLifetimeStrategy {
  bool ShouldComplete(EffectHandle handle, float elapsed);
}

// Implementations:
// - DurationLifetime(float seconds)
// - ParticleCompleteLifetime()
// - ConditionLifetime(Func<bool>)
// - CompositeLifetime(params IEffectLifetimeStrategy[])
```

### Файлы EffectsModule

| File | Purpose |
|------|---------|
| `EffectsModule.cs` | Основной модуль |
| `EffectRequest.cs` | Структура запроса |
| `EffectHandle.cs` | Handle для управления эффектом |
| `IEffectLifetimeStrategy.cs` | Интерфейс стратегии |
| `EffectLifetimeStrategies.cs` | Реализации стратегий |
| `EffectPool.cs` | Пулинг эффектов |

---

## Modified Files

### TreeModule.cs
- Добавить `[Inject] TreeFallConfigSO`
- Добавить `[Inject] EffectsModule`
- Добавить `[Inject] ITreeFallDirectionStrategy`
- Новый метод `StartTreeFall()` вместо немедленного destroy
- Callback `OnTreeSettled()` для финализации

### TreeTag.cs
- Добавить `float mass = 50f`
- Добавить `Transform crownTransform` (опционально, для позиции партиклов)

### GameScope.cs
- Регистрация `TreeFallConfigSO`
- Регистрация `EffectsModule`
- Регистрация `ITreeFallDirectionStrategy` (default implementation)

### GOAPAgent.cs / AnimalAgent.cs
- Реализация `IImpactReceiver`

### Tag.cs
- Добавить константу `LOG = "log"`

---

## Config Example

```yaml
TreeFallConfigSO:
  # Physics
  defaultMass: 50
  initialTorqueMultiplier: 15
  angularDrag: 0.5
  drag: 0.1
  
  # Settled Detection  
  settledVelocityThreshold: 0.3
  settledAngularThreshold: 0.2
  settledAngleFromVertical: 60
  settledCheckDelay: 0.5
  
  # Damage
  impactDamageMultiplier: 0.5
  minVelocityForDamage: 3
  
  # Log
  logDiameterRatio: 0.25
  logSides: 6
  logActorKey: "log_0"
```

---

## Implementation Order

1. **Phase 1: Core Infrastructure** ✅
   - [x] IImpactReceiver.cs
   - [x] TreeFallConfigSO.cs
   - [x] Tag.cs (add LOG constant)
   - [x] LogTag.cs
   - [x] StatType.HEALTH

2. **Phase 2: Effects System** ✅
   - [x] IEffectLifetimeStrategy.cs
   - [x] EffectLifetimeStrategies.cs
   - [x] EffectRequest.cs, EffectHandle.cs
   - [x] EffectPool.cs
   - [x] EffectsModule.cs
   - [x] LeafBurstFactory.cs

3. **Phase 3: Tree Fall Mechanics** ✅
   - [x] ITreeFallDirectionStrategy.cs
   - [x] Fall direction strategies (Random, AwayFromChopper, TowardsBuildings, Default, Composite)
   - [x] TreeColliderCache.cs
   - [x] ProceduralLogGenerator.cs
   - [x] FallingTreeBehaviour.cs

4. **Phase 4: Integration** ✅
   - [x] TreeTag.cs modifications (mass, crownTransform)
   - [x] TreeModule.cs (StartTreeFall, OnTreeSettled)
   - [x] GameScope.cs registrations
   - [x] GOAPAgent implements IImpactReceiver
   - [x] AgentBody.TakeDamage()

5. **Phase 5: Debug & Polish** ✅
   - [x] ChopTreeAction.cs (debug action)
   - [x] Leaf burst particle (via LeafBurstFactory)
   - [ ] Testing & calibration

---

## Setup Instructions

### 1. TreeFallConfigSO asset ✅ CREATED
- Asset created at: `Assets/Content/Settings/TreeFallConfig.asset`
- **TODO:** Assign to GameScope.treeFallConfig in Inspector

### 2. Create Log actor prefab
You can use existing log prefab from ThirdParty:
- `Assets/ThrirdParty/RPGPP_LT/Prefabs/Props/Wood/rpgpp_lt_log_wood_01.prefab`

Or create new:
1. Create empty GameObject "log_0"
2. Add components:
   - ActorDescription (set actorKey = "log_0")
   - ActorId
   - LogTag
   - MeshFilter + MeshRenderer (mesh will be replaced with procedural)
3. Create prefab in `Assets/Content/Prefabs/Resources/`
4. Register in Addressables with key `log_0`

### 3. Leaf Burst Particles (Optional)
- If `TreeFallConfigSO.leafBurstPrefab` is null → uses procedural `LeafBurstFactory`
- For custom particles: create ParticleSystem prefab and assign to config

### 4. Testing
- Open Debug Panel (default: F1 or configured key)
- Select a tree in scene
- Click "Chop Tree (Fall)" action
- Tree should fall, spawn leaf burst on impact, convert to log

---

## Files Created

| File | Path |
|------|------|
| IImpactReceiver | `Scripts/Game/Interaction/IImpactReceiver.cs` |
| TreeFallConfigSO | `Scripts/Game/Trees/TreeFallConfigSO.cs` |
| FallingTreeBehaviour | `Scripts/Game/Trees/FallingTreeBehaviour.cs` |
| TreeColliderCache | `Scripts/Game/Trees/TreeColliderCache.cs` |
| ProceduralLogGenerator | `Scripts/Game/Trees/ProceduralLogGenerator.cs` |
| ITreeFallDirectionStrategy | `Scripts/Game/Trees/Strategies/ITreeFallDirectionStrategy.cs` |
| RandomFallStrategy | `Scripts/Game/Trees/Strategies/RandomFallStrategy.cs` |
| AwayFromChopperStrategy | `Scripts/Game/Trees/Strategies/AwayFromChopperStrategy.cs` |
| TowardsBuildingsStrategy | `Scripts/Game/Trees/Strategies/TowardsBuildingsStrategy.cs` |
| CompositeFallStrategy | `Scripts/Game/Trees/Strategies/CompositeFallStrategy.cs` |
| DefaultFallStrategy | `Scripts/Game/Trees/Strategies/DefaultFallStrategy.cs` |
| LogTag | `Scripts/Descriptors/Tags/LogTag.cs` |
| EffectsModule | `Scripts/Game/Effects/EffectsModule.cs` |
| EffectRequest | `Scripts/Game/Effects/EffectRequest.cs` |
| EffectHandle | `Scripts/Game/Effects/EffectHandle.cs` |
| EffectPool | `Scripts/Game/Effects/EffectPool.cs` |
| IEffectLifetimeStrategy | `Scripts/Game/Effects/IEffectLifetimeStrategy.cs` |
| EffectLifetimeStrategies | `Scripts/Game/Effects/EffectLifetimeStrategies.cs` |
| LeafBurstFactory | `Scripts/Game/Effects/LeafBurstFactory.cs` |
| ChopTreeAction | `Scripts/DebugPanel/Actions/ChopTreeAction.cs` |

---

## Notes

- MeshCollider кешируется по `actorKey` для производительности
- Процедурное бревно имеет гексагональное сечение чтобы не катиться
- EffectsModule использует UniTask для async операций
- Стратегия направления падения настраивается через DI
