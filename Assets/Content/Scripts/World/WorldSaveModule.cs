using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using UnityEngine;
using VContainer;

namespace Content.Scripts.World {
  public class WorldSaveModule {
    private const string SAVE_FOLDER = "Saves";
    private const string DEFAULT_SLOT = "autosave";

    [Inject] private readonly ActorCreationModule _actorCreation;
    [Inject] private readonly SimulationTimeController _simTime;

    public event Action OnSaveCompleted;
    public event Action OnLoadCompleted;

    private string GetSavePath(string slot) {
      var folder = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
      if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
      return Path.Combine(folder, $"{slot}.json");
    }

    public bool HasSave(string slot = DEFAULT_SLOT) {
      return File.Exists(GetSavePath(slot));
    }

    public void Save(string slot = DEFAULT_SLOT) {
      var data = CollectSaveData();
      var json = JsonUtility.ToJson(data, true);

      var path = GetSavePath(slot);
      File.WriteAllText(path, json);

      Debug.Log($"[WorldSave] Saved to {path} ({data.actors.Count} actors, {data.agents.Count} agents)");
      OnSaveCompleted?.Invoke();
    }

    public bool Load(string slot = DEFAULT_SLOT) {
      var path = GetSavePath(slot);
      if (!File.Exists(path)) {
        Debug.LogWarning($"[WorldSave] No save file at {path}");
        return false;
      }

      try {
        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<WorldSaveData>(json);
        RestoreSaveData(data);

        Debug.Log($"[WorldSave] Loaded from {path} ({data.actors.Count} actors, {data.agents.Count} agents)");
        OnLoadCompleted?.Invoke();
        return true;
      }
      catch (Exception e) {
        Debug.LogError($"[WorldSave] Failed to load: {e.Message}");
        return false;
      }
    }

    public void DeleteSave(string slot = DEFAULT_SLOT) {
      var path = GetSavePath(slot);
      if (File.Exists(path)) {
        File.Delete(path);
        Debug.Log($"[WorldSave] Deleted {path}");
      }
    }

    private WorldSaveData CollectSaveData() {
      var data = new WorldSaveData {
        timestamp = DateTime.Now.ToString("o"),
        simTime = _simTime?.totalSimTime ?? 0f
      };

      // Collect all actors (non-agent)
      var allActors = UnityEngine.Object.FindObjectsByType<ActorDescription>(FindObjectsSortMode.None);
      foreach (var actor in allActors) {
        // Skip agents - they're saved separately
        if (actor.GetComponent<GOAPAgent>() != null) continue;

        var actorData = new ActorSaveData {
          actorKey = actor.actorKey,
          position = actor.transform.position,
          rotation = actor.transform.rotation,
          scale = actor.transform.localScale,
          stackCount = actor.GetStackData()?.current ?? 1
        };
        data.actors.Add(actorData);
      }

      // Collect agents
      var agents = UnityEngine.Object.FindObjectsByType<GOAPAgent>(FindObjectsSortMode.None);
      foreach (var agent in agents) {
        var agentData = new AgentSaveData {
          agentId = agent.GetComponent<ActorId>()?.id ?? 0,
          position = agent.transform.position,
          rotation = agent.transform.rotation,
          stats = CollectAgentStats(agent)
        };
        data.agents.Add(agentData);
      }

      return data;
    }

    private AgentStatsSaveData CollectAgentStats(GOAPAgent agent) {
      var stats = new AgentStatsSaveData();
      var body = agent.body;
      if (body == null) return stats;

      foreach (var stat in body.GetStatsInfo()) {
        if (stat is not FloatAgentStat floatStat) continue;

        switch (floatStat.type) {
          case StatType.HUNGER:
            stats.hunger = floatStat.value;
            break;
          case StatType.FATIGUE:
            stats.fatigue = floatStat.value;
            break;
          // Add more as needed
        }
      }

      stats.level = agent.experience.level;
      stats.experience = agent.experience.currentXP;
      return stats;
    }

    private void RestoreSaveData(WorldSaveData data) {
      // Restore sim time
      if (_simTime != null && data.simTime > 0) {
        _simTime.SetSimTime(data.simTime);
      }

      // Clear existing world actors (but not agents)
      var existingActors = UnityEngine.Object.FindObjectsByType<ActorDescription>(FindObjectsSortMode.None);
      foreach (var actor in existingActors) {
        if (actor.GetComponent<GOAPAgent>() != null) continue;
        UnityEngine.Object.Destroy(actor.gameObject);
      }

      // Spawn saved actors
      foreach (var actorData in data.actors) {
        if (_actorCreation.TrySpawnActorOnGround(actorData.actorKey, actorData.position, out var actor, (ushort)actorData.stackCount)) {
          actor.transform.rotation = actorData.rotation;
          actor.transform.localScale = actorData.scale;
        }
      }

      // Restore agents
      RestoreAgents(data.agents);
    }

    private void RestoreAgents(List<AgentSaveData> agentDatas) {
      var agents = UnityEngine.Object.FindObjectsByType<GOAPAgent>(FindObjectsSortMode.None);

      foreach (var agentData in agentDatas) {
        // Find agent by ID or just take first available
        var agent = agents.FirstOrDefault(a => a.GetComponent<ActorId>()?.id == agentData.agentId)
                    ?? agents.FirstOrDefault();

        if (agent == null) {
          Debug.LogWarning($"[WorldSave] Could not find agent for ID {agentData.agentId}");
          continue;
        }

        // Restore position
        agent.navMeshAgent.Warp(agentData.position);
        agent.transform.rotation = agentData.rotation;

        // Restore stats
        RestoreAgentStats(agent, agentData.stats);
      }
    }

    private void RestoreAgentStats(GOAPAgent agent, AgentStatsSaveData stats) {
      var body = agent.body;
      if (body == null) return;

      foreach (var stat in body.GetStatsInfo()) {
        if (stat is not FloatAgentStat floatStat) continue;

        switch (floatStat.type) {
          case StatType.HUNGER:
            floatStat.value = stats.hunger;
            break;
          case StatType.FATIGUE:
            floatStat.value = stats.fatigue;
            break;
        }
      }

      // Restore level/XP through experience component
      // Note: This may need additional API on AgentExperience
    }
  }
}
