using System;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Simulation;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace Content.Scripts.Game.Harvesting {
  /// <summary>
  /// Manages harvestable actors: initialization, harvesting logic, and yield spawning.
  /// </summary>
  public class HarvestModule : IInitializable, IDisposable {
    private const float DEFAULT_WORK_PER_UNIT = 2f;

    [Inject] private readonly ActorCreationModule _creationModule;
    [Inject] private readonly SimulationLoop _simulationLoop;


    void IInitializable.Initialize() {
      // Subscribe to new harvestable actors being registered
      ActorRegistry<HarvestableTag>.onRegistered += OnHarvestableRegistered;

      // Initialize existing harvestables
      foreach (var harvestable in ActorRegistry<HarvestableTag>.all) {
        InitializeHarvestable(harvestable);
      }
    }

    void IDisposable.Dispose() {
      ActorRegistry<HarvestableTag>.onRegistered -= OnHarvestableRegistered;
    }

    private void OnHarvestableRegistered(HarvestableTag harvestable) {
      InitializeHarvestable(harvestable);
    }

    private void InitializeHarvestable(HarvestableTag harvestable) {
      var go = harvestable.gameObject;

      // Add GrowthProgress if not present
      var growth = go.GetComponent<GrowthProgress>();
      if (growth == null) {
        growth = go.AddComponent<GrowthProgress>();
      }
      growth.Initialize(harvestable, _simulationLoop);
    }

    /// <summary>
    /// Attempt to harvest one unit from target. Spawns yield on ground.
    /// </summary>
    /// <param name="harvestingProgress">Work progress component on target</param>
    /// <param name="agent">Agent performing the harvest</param>
    /// <returns>True if harvest was successful</returns>
    public bool TryHarvestUnit(HarvestingProgress harvestingProgress, IGoapAgentCore agent) {
      if (harvestingProgress == null) return false;

      var growth = harvestingProgress.growthProgress;
      if (growth == null || !growth.hasYield) return false;

      var harvestableTag = harvestingProgress.actor.GetDefinition<HarvestableTag>();
      if (harvestableTag == null) return false;

      // Consume one unit from growth
      var consumed = growth.ConsumeYield(1);
      if (consumed <= 0) return false;

      // Spawn yield on ground
      SpawnYield(harvestableTag, harvestingProgress.transform.position, agent);

      // Reset work progress for next unit
      harvestingProgress.ConsumeUnit();

      Debug.Log($"[HarvestModule] Harvested 1 {harvestableTag.harvestableActorKey} from {harvestingProgress.actor.name}, remaining: {growth.currentYield}");
      return true;
    }

    private void SpawnYield(HarvestableTag harvestableTag, Vector3 position, IGoapAgentCore agent) {
      var spawnPos = position + GetRandomOffset();

      if (!_creationModule.TrySpawnActorOnGround(harvestableTag.harvestableActorKey, spawnPos, out var yieldActor)) {
        Debug.LogError($"[HarvestModule] Failed to spawn yield: {harvestableTag.harvestableActorKey}");
        return;
      }

      // Agent remembers the spawned yield for potential pickup
      agent?.agentBrain?.TryRemember(yieldActor, out _);
    }

    private static Vector3 GetRandomOffset() {
      var offset = Random.insideUnitSphere * 0.5f;
      offset.y = Mathf.Abs(offset.y) + 0.3f;
      return offset;
    }

    /// <summary>
    /// Check if harvestable has any yield available.
    /// </summary>
    public static bool HasYield(ActorDescription actor) {
      var growth = actor?.GetComponent<GrowthProgress>();
      return growth != null && growth.hasYield;
    }

    /// <summary>
    /// Get current yield count for harvestable.
    /// </summary>
    public static int GetYield(ActorDescription actor) {
      var growth = actor?.GetComponent<GrowthProgress>();
      return growth?.currentYield ?? 0;
    }

    /// <summary>
    /// Get or create HarvestingProgress for work tracking.
    /// </summary>
    public static HarvestingProgress GetOrCreateWorkProgress(ActorDescription target, float workPerUnit = DEFAULT_WORK_PER_UNIT) {
      return HarvestingProgress.GetOrCreate(target.gameObject, workPerUnit);
    }
  }
}
