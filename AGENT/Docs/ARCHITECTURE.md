# Architecture

## Code Style
- Odin: sparingly
- Comments: English
- Strategies: `[Serializable]` for SO editing

---

## Static Reset

Prevents state leaking (domain reload OFF).
```csharp
static MyStaticClass() {
  StaticResetRegistry.RegisterReset(Clear);
}
```

---

## Tag System

**Tag Wizard:** `GOAP/Tag Wizard` - add/delete tags

**TagDefinition as Config:**
```csharp
public class FoodTag : TagDefinition {
  public override string Tag => AI.Tag.FOOD;
  public float nutrition = 50f;
}
// actor.GetDefinition<FoodTag>()?.nutrition
```

---

## World Grid

Spatial index (2D cells).
```csharp
WorldGrid.GetActorsInRadius(coord, 5);
WorldGrid.GetNearestInRadius(coord, 10, Tag.TREE);
```

---

## SimTimer

Simulation-aware timer for strategies.
```csharp
_timer = new SimTimer(duration);
_timer.OnTimerComplete += () => complete = true;
_timer.Start();
// OnUpdate: _timer.Tick(deltaTime);
```

---

## Environment

`WorldEnvironment.instance.dayCycle` - time, phases (Night/Dawn/Day/Dusk)

---

## ActorCreationModule

Grounded spawn via Addressables.
```csharp
actorCreation.TrySpawnActor("wood_log", position, out var actor);
```
