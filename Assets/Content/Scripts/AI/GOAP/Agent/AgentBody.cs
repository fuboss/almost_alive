using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  public class AgentBody : SerializedMonoBehaviour {
    [SerializeField] private List<AgentStat> _stats = new();

    [ShowInInspector] private Dictionary<StatType, float> _perTickDelta = new();
    public IReadOnlyDictionary<StatType, float> perTickDelta => _perTickDelta;

    private IGoapAgent _agent;

    public void Initialize(IGoapAgent agent) {
      _agent = agent;
      _perTickDelta ??= new Dictionary<StatType, float>();

      _stats = _agent.defaultStatSet.GetDefaultStats();
    }

    public void TickStats(float deltaTime) {
      foreach (var stat in _stats) {
        if (stat is not FloatAgentStat floatStat) continue;
        if (floatStat.type is StatType.UNDEFINED) {
          Debug.LogError("FloatStat has no name assigned.", this);
          continue;
        }

        if (_perTickDelta.TryGetValue(floatStat.type, out var delta)) {
          floatStat.value = Mathf.Clamp(floatStat.value + delta * deltaTime, 0f, floatStat.maxValue);
        }
      }
    }

    public AgentStat GetStat(StatType statName) {
      return _stats.FirstOrDefault(s => s.type == statName);
    }
    
    public IReadOnlyList<AgentStat> GetStatsInfo() {
      return _stats;
    }

    public void AdjustStatPerTickDelta(StatType statName, float delta) {
      // Debug.LogWarning($"try AdjustStatPerTick {statName} {delta}", this);
      if (!_perTickDelta.TryAdd(statName, delta)) {
        _perTickDelta[statName] += delta;
      }
    }

    public void AdjustStatPerTickDelta(List<PerTickStatChange> statsChange, float multiplier = 1f) {
      if (statsChange == null) return;
      foreach (var change in statsChange.Where(change => change != null)) {
        AdjustStatPerTickDelta(change.statType, change.delta * multiplier);
      }
    }

    public void SetPerTickDelta(StatType statName, float delta) {
      _perTickDelta[statName] = delta;
    }


    public void SetResting(bool isResting) {
      Debug.LogError($"BODY isResting: {isResting}", this);
    }

    public void SetToiletUse(bool inToilet) {
      Debug.LogError($"BODY inToilet: {inToilet}", this);
    }
  }
}