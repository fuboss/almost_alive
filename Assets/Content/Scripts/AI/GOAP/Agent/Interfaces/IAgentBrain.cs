using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Agent.Sensors;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Planning;
using Content.Scripts.Game;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Core interface for agent brains (human, animal, NPC).
  /// Provides memory, beliefs, planning, and sensors.
  /// </summary>
  public interface IAgentBrain {
    // Memory
    AgentMemory memory { get; }

    // GOAP data
    Dictionary<string, AgentBelief> beliefs { get; }
    HashSet<AgentAction> actions { get; }
    HashSet<GoalTemplate> goalTemplates { get; }
    AgentGoal currentGoal { get; }
    ActionPlan actionPlan { get; }

    // Core methods
    AgentBelief Get(string beliefName);
    void Tick(float deltaTime);
    void TryRemember(ActorDescription visibleActor);

    // Sensors
    InteractionSensor interactSensor { get; }
    VisionSensor visionSensor { get; }

    // Debug/History
    ActionHistoryTracker actionHistory { get; }
  }
}
