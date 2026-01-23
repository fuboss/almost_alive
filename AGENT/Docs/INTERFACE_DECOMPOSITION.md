# Agent Interfaces

## Hierarchy

```
IGoapAgentCore (base)
├── transform, gameObject
├── navMeshAgent, position, isMoving
├── agentBrain (IAgentBrain)
├── body, defaultStatSet
└── StopAndCleanPath()

ITransientTargetAgent { transientTarget }
IInventoryAgent { inventory }
IWorkAgent { experience, recipes, recipeModule }
ICampAgent { camp, campData }
IHerdMember { herdId, herdingBehavior }
```

## Composites

```
IGoapAgent = Core + Transient + Inventory + Work + Camp  (human)
IGoapAnimalAgent = Core + Herd  (animal)
```

## Brains

```
IAgentBrain
├── memory, beliefs, actions, goalTemplates
├── currentGoal, actionPlan
├── visionSensor, interactSensor

AgentBrainBase (abstract)
├── AgentBrain - +interruptions, +planStack
└── AnimalBrain - simple
```

## Implementations

| Class | Interface | Brain |
|-------|-----------|-------|
| GOAPAgent | IGoapAgent | AgentBrain |
| AnimalAgent | IGoapAnimalAgent | AnimalBrain |

## Usage Pattern

Beliefs/strategies use `IGoapAgentCore`. For specific features:
```csharp
if (agent is not IInventoryAgent inv) return () => false;
return () => inv.inventory.HasItem(tag);
```
