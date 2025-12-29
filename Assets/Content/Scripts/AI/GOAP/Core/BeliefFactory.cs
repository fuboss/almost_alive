using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Core {
  public class BeliefFactory {
    private readonly GOAPAgent _agent;
    private readonly Dictionary<string, AgentBelief> _beliefs;

    public BeliefFactory(GOAPAgent agent, Dictionary<string, AgentBelief> beliefs) {
      _agent = agent;
      _beliefs = beliefs;
    }

    public void AddBelief(string key, Func<bool> condition) {
      _beliefs.Add(key, new AgentBelief.Builder(key)
        .WithCondition(condition)
        .Build());
    }

    public void AddSensorBelief(string key, Sensor sensor) {
      _beliefs.Add(key, new AgentBelief.Builder(key)
        .WithCondition(() => sensor.IsTargetInRange)
        .WithLocation(() => sensor.TargetPosition)
        .Build());
    }

    public void AddLocationBelief(string key, float distance, Transform locationCondition) {
      AddLocationBelief(key, distance, locationCondition.position);
    }

    public void AddLocationBelief(string key, float distance, Vector3 locationCondition) {
      _beliefs.Add(key, new AgentBelief.Builder(key)
        .WithCondition(() => InRangeOf(locationCondition, distance))
        .WithLocation(() => locationCondition)
        .Build());
    }

    private bool InRangeOf(Vector3 pos, float range) {
      return Vector3.Distance(_agent.transform.position, pos) < range;
    }
  }
}