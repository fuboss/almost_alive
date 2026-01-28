using System;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Effects;
using Content.Scripts.Game.Trees.Strategies;
using UnityEngine;
using UnityEngine.AI;
using VContainer;
using Random = UnityEngine.Random;

namespace Content.Scripts.Game.Trees {
  public class TreeModule : IDisposable {
    [Inject] private ActorDestructionModule _actorDestruction;
    [Inject] private ActorCreationModule _creationModule;
    [Inject] private EffectsModule _effectsModule;
    [Inject] private TreeFallConfigSO _config;

    private TreeFallConfig Config => _config.Data;

    //todo: inject all strategies and randomize them
    private readonly ITreeFallDirectionStrategy _fallStrategy = new DefaultFallStrategy();

    public void ChopDownTree(ChoppingProgress choppingProgress, IGoapAgentCore byAgent) {
      if (choppingProgress == null || choppingProgress.actor == null) return;
      if (!choppingProgress.isComplete) return;

      var actor = choppingProgress.actor;
      var treeDef = actor.GetDefinition<TreeTag>();

      Debug.Log($"[TreeModule] Tree {actor.name} chopped down, starting fall");

      var chopperPos = byAgent?.navMeshAgent?.transform.position ?? actor.transform.position + Vector3.right;
      StartTreeFall(actor, treeDef, chopperPos);
    }

    public void StartTreeFall(ActorDescription treeActor, TreeTag treeDef, Vector3 chopperPosition) {
      var treeGO = treeActor.gameObject;

      DisableTreeComponents(treeGO);

      var meshFilter = treeGO.GetComponentInChildren<MeshFilter>();
      var meshRenderer = treeGO.GetComponentInChildren<MeshRenderer>();

      if (meshFilter == null || meshRenderer == null) {
        Debug.LogWarning($"[TreeModule] Tree {treeActor.name} has no MeshFilter/Renderer, destroying directly");
        _actorDestruction.DestroyActor(treeActor);
        return;
      }

      var worldBounds = meshRenderer.bounds;
      var material = meshRenderer.sharedMaterial;

      SetupPhysics(treeGO, treeActor.actorKey, meshFilter, treeDef?.mass ?? Config.defaultMass);

      var fallDirection = _fallStrategy.GetFallDirection(treeGO.transform, chopperPosition, _config);

      var fallingBehaviour = treeGO.AddComponent<FallingTreeBehaviour>();
      fallingBehaviour.Initialize(
        _config,
        _effectsModule,
        worldBounds,
        material,
        treeDef?.crownTransform,
        fb => OnTreeSettled(fb, treeActor, treeDef)
      );
      fallingBehaviour.ApplyFallImpulse(fallDirection);
    }

    private void DisableTreeComponents(GameObject treeGO) {
      var navObstacle = treeGO.GetComponentInChildren<NavMeshObstacle>();
      if (navObstacle != null) {
        navObstacle.enabled = false;
      }

      var existingColliders = treeGO.GetComponentsInChildren<Collider>();
      foreach (var col in existingColliders) {
        col.enabled = false;
      }
    }

    private void SetupPhysics(GameObject treeGO, string actorKey, MeshFilter meshFilter, float mass) {
      var colliderMesh = TreeColliderCache.GetOrCreateColliderMesh(actorKey, meshFilter, Config.groundEmbedOffset);

      var treeRenderer = treeGO.GetComponentInChildren<MeshRenderer>();
      var meshCollider = treeRenderer.gameObject.AddComponent<MeshCollider>();
      meshCollider.sharedMesh = colliderMesh;
      meshCollider.convex = true;

      var rb = treeGO.AddComponent<Rigidbody>();
      rb.mass = mass;
      rb.angularDamping = Config.angularDrag;
      rb.linearDamping = Config.linearDrag;
      rb.interpolation = RigidbodyInterpolation.Interpolate;
      rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

      // Prevent initial physics jitter - freeze position briefly
      rb.constraints = RigidbodyConstraints.FreezePositionY;
    }

    private void OnTreeSettled(FallingTreeBehaviour fallingTree, ActorDescription originalActor, TreeTag treeDef) {
      Debug.Log($"[TreeModule] Tree {originalActor.name} settled, converting to log");

      var treeTransform = fallingTree.transform;
      var bounds = fallingTree.originalBounds;

      // Calculate log spawn position - center of the fallen tree
      // The tree's "up" is now roughly horizontal, so we offset along that axis
      var treeCenter = treeTransform.position + treeTransform.up * (bounds.extents.y * 0.5f);

      // Log rotation: align log's X axis with tree's up (the fallen direction)
      // Log mesh is created along X axis, so we rotate it to match tree's orientation
      var logRotation = Quaternion.LookRotation(treeTransform.up, Vector3.up) * Quaternion.Euler(0, 90, 0);

      Debug.Log($"[TreeModule] Tree bounds: {bounds}, center calc: {treeCenter}");
      Debug.Log($"[TreeModule] Tree transform.up: {treeTransform.up}, position: {treeTransform.position}");

      // Destroy the original tree AFTER capturing all data
      if (originalActor != null) {
        Debug.Log($"[TreeModule] Destroying original tree actor: {originalActor.name}");
        _actorDestruction.DestroyActor(originalActor);
      }
      else {
        Debug.LogWarning("[TreeModule] Original actor is null, cannot destroy!");
      }

      SpawnLog(treeCenter, logRotation, bounds, fallingTree.originalMaterial, treeDef);
    }

    private void SpawnLog(Vector3 position, Quaternion rotation, Bounds treeBounds, Material material,
      TreeTag treeDef) {
      Debug.Log($"[TreeModule] SpawnLog at {position}, rotation: {rotation.eulerAngles}");

      if (!_creationModule.TrySpawnActor(Config.logActorKey, position, out var logActor)) {
        Debug.LogError($"[TreeModule] Failed to spawn log actor '{Config.logActorKey}'");
        return;
      }

      Debug.Log($"[TreeModule] Log actor created: {logActor.name}");

      logActor.transform.position = position;
      logActor.transform.rotation = rotation;

      var logMesh = ProceduralLogGenerator.CreateLogMesh(treeBounds, _config);
      ProceduralLogGenerator.ApplyLogMesh(logActor.gameObject, logMesh, material);

      var logTag = logActor.GetDefinition<LogTag>();
      if (logTag != null && treeDef != null) {
        logTag.plankYield = Mathf.Max(1, treeDef.woodYield / 2);
        Debug.Log($"[TreeModule] LogTag configured: plankYield={logTag.plankYield}");
      }

      Debug.Log($"[TreeModule] Log spawned successfully at {position}");
    }

    public void LogChopped(ChoppingProgress choppingProgress, IGoapAgentCore agent) {
      var logData = choppingProgress.actor.GetDefinition<LogTag>();
      SpawnYieldWood(choppingProgress, agent, logData.woodSource, logData.woodActorID);

      _actorDestruction.DestroyActor(choppingProgress.actor);
    }

    private void SpawnYieldWood(ChoppingProgress choppingProgress, IGoapAgentCore byAgent, int yield, string actorID) {
      var shift = Vector3.zero;
      for (int i = 0; i < yield; i++) {
        if (!_creationModule.TrySpawnActorOnGround(actorID, choppingProgress.transform.position + shift,
              out var woodActor)) continue;
        shift = NextShift();
        byAgent.agentBrain.TryRemember(woodActor, out var _);
      }
    }

    private static Vector3 NextShift() {
      var shift = Random.onUnitSphere;
      shift.y = Mathf.Abs(shift.y);
      shift += Vector3.up * 0.66f;
      return shift;
    }

    void IDisposable.Dispose() {
      TreeColliderCache.Clear();
    }
  }
}
