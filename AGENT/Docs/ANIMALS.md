# Animals System

## Architecture

```
IGoapAgentCore (base)
└── IGoapAnimalAgent = core + IHerdMember

AgentBrainBase (abstract)
├── AgentBrain (human) - interruptions, plan stack
└── AnimalBrain (animal) - simple
```

---

## Components

**AnimalsModule** - central herd manager
```
CreateHerd(pos) → HerdAnchor
SpawnHerd(prefab, pos, count)
GetHerdMembers(anchor)
AddToHerd / RemoveFromHerd
```

**HerdAnchor** - drifting migration point (0.2 u/s, ~45s direction change)

**HerdingBehavior** - flocking steering
| Force | Weight |
|-------|--------|
| Cohesion | 1.0 |
| Separation | 1.5 |
| Alignment | 0.5 |
| Anchor Pull | 0.8 |

```
GetModifiedDestination(original) → Vector3
GetSteeringOffset() → Vector3
```

**AnimalAgent** = AnimalBrain + AgentBody + HerdingBehavior + NavMeshAgent

---

## Deer Design

Stats: HUNGER (-0.05/s), FATIGUE (-0.02/s)

Beliefs:
- Animal_IsHungry (HUNGER < 30)
- Animal_IsTired (FATIGUE < 20)
- Animal_IsRested (FATIGUE > 70)

Goals:
- Deer_Graze (default)
- Deer_Rest (when tired)

---

## TODO: Phase 4

- [ ] GrazeStrategy - wander + HerdingBehavior + periodic eat
- [ ] SleepOnGroundStrategy - idle, restore fatigue
- [ ] Beliefs, goals, actions
- [ ] deer_stats.asset, Animal_FeatureSet.asset
- [ ] Deer.prefab, test
