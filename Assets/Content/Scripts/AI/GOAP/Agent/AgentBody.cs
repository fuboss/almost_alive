using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  public class AgentBody : SerializedMonoBehaviour {
    [SerializeField] private List<AgentStat> _stats = new() {
      new FloatAgentStat(StatConstants.HUNGER, 100f, 100f),
      new FloatAgentStat(StatConstants.FATIGUE, 0f, 100f),
      new FloatAgentStat(StatConstants.SLEEP, 100f, 100f),
      new FloatAgentStat(StatConstants.TOILET, 0f, 100f)
    };

    [ShowInInspector] private Dictionary<string, float> _perTickDelta = new();

    private IGoapAgent _agent;

    public void Initialize(IGoapAgent agent) {
      _agent = agent;
      _perTickDelta ??= new Dictionary<string, float>();
    }

    public void TickStats(float deltaTime) {
      foreach (var stat in _stats) {
        if (stat is not FloatAgentStat floatStat) continue;
        if (string.IsNullOrWhiteSpace(floatStat.name)) {
          Debug.LogError("FloatStat has no name assigned.", this);
          continue;
        }

        if (_perTickDelta.TryGetValue(floatStat.name, out var delta)) {
          floatStat.Value = Mathf.Clamp(floatStat.Value + delta * deltaTime, 0f, floatStat.MaxValue);
        }
      }
    }

    public AgentStat GetStat(string statName) {
      return _stats.FirstOrDefault(s => s.name == statName);
    }

    public void AdjustStatPerTickDelta(string statName, float delta) {
      if (!_perTickDelta.TryAdd(statName, delta)) {
        _perTickDelta[statName] += delta;
      }
    }

    public void SetPerTickDelta(string statName, float delta) {
      _perTickDelta[statName] = delta;
    }

    public void ConsumeFood(GameObject foodActor) {
      if (foodActor == null) return;
      Destroy(foodActor);

      if (GetStat(StatConstants.HUNGER) is FloatAgentStat hungerStat) {
        hungerStat.Value += 15f;
        Debug.LogError($"AgentBody: Consumed food, new hunger value: {hungerStat.Value}({hungerStat.Normalized})");
      }
    }
  }
}