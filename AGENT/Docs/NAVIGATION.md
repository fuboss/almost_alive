# Navigation

## PathCostEvaluator

NavMesh-aware distance for "nearest" queries.
```csharp
PathCostEvaluator.GetPathCost(agent, target) → float
PathCostEvaluator.IsReachable(agent, target) → bool
```

**MemorySearcher** has `useNavMeshDistance` flag.

---

## AgentStuckDetector

Detects agent not moving despite having path (2s threshold, 0.5m min movement).

Raises `OnStuck` → triggers replan.

---

## Interruption System

**InterruptionManager** - periodic check of sources

**PlanStack** - save/restore interrupted plans (max depth 3)

**Built-in sources:**
- CriticalStatInterruption (stat below threshold)
- ValuableItemInterruption (valuable in vision/memory)

Flow: Check sources → Push plan → ForceGoal → On complete: Pop → Validate → Resume/Replan

---

## Notes

**Slope sliding fix:** `Rigidbody.isKinematic = true` on agent prefabs.
