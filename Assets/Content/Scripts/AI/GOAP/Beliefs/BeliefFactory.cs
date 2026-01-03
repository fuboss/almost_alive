using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  public class BeliefFactory {
    private readonly IGoapAgent _agent;
    private readonly Dictionary<string, AgentBelief> _beliefs;
    private readonly AgentMemory _memory;

    public BeliefFactory(IGoapAgent agent, Dictionary<string, AgentBelief> beliefs) {
      _agent = agent;
      _beliefs = beliefs;
      _memory = _agent.memory;
    }

    public void AddBelief(string key, Func<bool> condition) {
      _beliefs.Add(key, new AgentBelief.Builder(key)
        .WithCondition(condition)
        .Build());
    }
    
    public void AddStatBelief(string key, StatType statName, Func<float, bool> condition) {
      _beliefs.Add(key, new AgentBelief.Builder(key)
        .WithCondition(() => condition(_agent.body.GetStat(statName).Normalized))
        .Build());
    }
    
    public void AddInteractSensorBelief(string key, SimpleSensor sensor) {
      _beliefs.Add(key, new AgentBelief.Builder(key)
        .WithCondition(() => sensor.IsTargetInRange)
        .Build());
    }
    
    public void AddVisionSensorBelief(string key, string[] tags) {
      var sensor = _agent.agentBrain.visionSensor;
      _beliefs.Add(key, new AgentBelief.Builder(key)
        .WithCondition(() => sensor.HasObjectsWithTagsInView(tags))
        //.WithLocation(() => sensor.TargetPosition)
        .Build());
    }

    // public void AddLocationBelief(string key, float distance, Transform locationCondition) {
    //   AddLocationBelief(key, distance, locationCondition.position);
    // }
    //
    // public void AddLocationBelief(string key, float distance, Vector3 locationCondition) {
    //   _beliefs.Add(key, new AgentBelief.Builder(key)
    //     .WithCondition(() => InRangeOf(locationCondition, distance))
    //     .WithLocation(() => locationCondition)
    //     .Build());
    // }

    private bool InRangeOf(Vector3 pos, float range) {
      return Vector3.Distance(_agent.position, pos) < range;
    }

    public void AddMemoryBelief(string key, string[] tags) {
      var memory = _agent.memory;
      _beliefs.Add(key, new AgentBelief.Builder(key)
        .WithCondition(() => memory.GetWithAllTags(tags).Length > 0)
        //.WithLocation(() => sensor.TargetPosition)
        .Build());
    }
  }
}