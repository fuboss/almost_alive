# GOAP System

## Structure

```
Resources/GOAP/
├── Common/     # idle, wander, hauling
├── Hunger/     # eat system
├── Fatigue/    # rest system
├── Toilet/     # bladder
└── UtilityEvaluators/
```

---

## Core

| Component | Description |
|-----------|-------------|
| GoapFeatureSO | Goals, beliefs, actions bundle |
| GoapFeatureBankModule | Loads from Resources/GOAP |
| AgentBrain / AgentBrainBase | Planning, memory, sensors |

---

## Beliefs

Base: `AgentBelief.GetCondition(IGoapAgentCore)` → `Func<bool>`

Wrappers: BeliefSO, CompositeBeliefSO (generates per tag)

Naming: `Remembers_Nearby/{TAG}`, `Inventory_Has/{TAG}`, `Transient_Is/{TAG}`

---

## Actions

`AgentAction` - runtime (preconditions, effects, strategy)  
`ActionDataSO` - serialized template  
`AgentStrategy` - OnStart/OnUpdate/OnStop/OnComplete

---

## Planning: Backward Chaining

1. Start from goal's desiredEffects
2. Find action providing effect
3. Action's preconditions → new requirements
4. Repeat

**Critical pattern:**
```
MoveTo_X:  pre=[Remembers_Nearby/X]  eff=[Transient_Is/X]
Interact_X: pre=[Transient_Is/X]     eff=[Result]
```
Without Transient_Is precondition → planner won't chain MoveTo!

---

## Memory

**AgentMemory** - spatial (octree), tag index, target index

**MemorySearcher** - queries with NavMesh distance support

**persistentMemory** - key-value for camp, ownership

Fast methods (no alloc): `HasWithAllTags`, `CountWithAllTags`, `TryGetByTarget`

---

## Utility

`IUtilityEvaluator` → float priority  
`CompositeUtilitySO<T>` → generates per tag/stat

All use `IGoapAgentCore` with interface checks for specific features.
