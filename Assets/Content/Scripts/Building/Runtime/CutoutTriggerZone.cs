using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Triggers cutout when agents/camera enter structure bounds.
  /// </summary>
  [RequireComponent(typeof(StructureCutoutController))]
  public class CutoutTriggerZone : MonoBehaviour {
    [Title("Trigger Mode")]
    [SerializeField] private CutoutTriggerMode _mode = CutoutTriggerMode.AgentsInside;
    
    [Title("Detection")]
    [SerializeField, ShowIf(nameof(UsesPhysicsTrigger))] 
    private BoxCollider _triggerCollider;
    
    [SerializeField, ShowIf(nameof(_mode), CutoutTriggerMode.AgentsInside)] 
    private float _checkInterval = 0.5f;
    
    [Title("Cutout Behavior")]
    [Tooltip("Auto-update cutout center to average agent position")]
    [SerializeField] private bool _dynamicCenter = true;
    
    [Tooltip("Base radius + per-agent expansion")]
    [SerializeField] private float _baseRadius = 3f;
    [SerializeField] private float _radiusPerAgent = 1f;
    
    [ShowInInspector, ReadOnly] private readonly HashSet<GOAPAgent> _agentsInside = new();
    
    private StructureCutoutController _controller;
    private Structure _structure;
    private float _nextCheckTime;
    
    private bool UsesPhysicsTrigger => _mode is CutoutTriggerMode.PhysicsTrigger or CutoutTriggerMode.CameraInside;

    #region Lifecycle

    private void Awake() {
      _controller = GetComponent<StructureCutoutController>();
      _structure = GetComponent<Structure>();
      
      if (_triggerCollider == null && UsesPhysicsTrigger) {
        _triggerCollider = gameObject.AddComponent<BoxCollider>();
        _triggerCollider.isTrigger = true;
        AutoSizeTrigger();
      }
    }

    private void Update() {
      if (_mode == CutoutTriggerMode.AgentsInside) {
        CheckAgentsInside();
      }
    }

    #endregion

    #region Physics Trigger

    private void OnTriggerEnter(Collider other) {
      if (!UsesPhysicsTrigger) return;
      
      if (_mode == CutoutTriggerMode.CameraInside) {
        if (other.CompareTag("MainCamera")) {
          _controller.EnableCutout();
        }
        return;
      }
      
      var agent = other.GetComponentInParent<GOAPAgent>();
      if (agent != null) {
        _agentsInside.Add(agent);
        UpdateCutoutState();
      }
    }
    
    private void OnTriggerExit(Collider other) {
      if (!UsesPhysicsTrigger) return;
      
      if (_mode == CutoutTriggerMode.CameraInside) {
        if (other.CompareTag("MainCamera")) {
          _controller.DisableCutout();
        }
        return;
      }
      
      var agent = other.GetComponentInParent<GOAPAgent>();
      if (agent != null) {
        _agentsInside.Remove(agent);
        UpdateCutoutState();
      }
    }

    #endregion

    #region Agent Detection

    private void CheckAgentsInside() {
      if (Time.time < _nextCheckTime) return;
      _nextCheckTime = Time.time + _checkInterval;
      
      _agentsInside.Clear();
      
      // Check all registered agents vs structure bounds
      var bounds = GetStructureBounds();
      foreach (var agent in ActorRegistry<GOAPAgent>.all) {
        if (bounds.Contains(agent.transform.position)) {
          _agentsInside.Add(agent);
        }
      }
      
      UpdateCutoutState();
    }
    
    private Bounds GetStructureBounds() {
      if (_triggerCollider != null) return _triggerCollider.bounds;
      
      // Fallback: calculate from structure footprint
      var footprint = _structure != null ? _structure.footprint : Vector2Int.one;
      var cellSize = BuildingConstants.CellSize;
      
      var center = transform.position + new Vector3(
        footprint.x * cellSize * 0.5f,
        cellSize * 0.5f,
        footprint.y * cellSize * 0.5f
      );
      
      var size = new Vector3(
        footprint.x * cellSize,
        cellSize * 2f,
        footprint.y * cellSize
      );
      
      return new Bounds(center, size);
    }

    #endregion

    #region Cutout Update

    private void UpdateCutoutState() {
      var hasAgents = _agentsInside.Count > 0;
      
      if (hasAgents) {
        _controller.EnableCutout();
        
        if (_dynamicCenter) {
          var avgPos = Vector3.zero;
          foreach (var agent in _agentsInside) {
            avgPos += agent.transform.position;
          }
          avgPos /= _agentsInside.Count;
          _controller.SetCutoutCenter(avgPos);
        }
        
        var radius = _baseRadius + _agentsInside.Count * _radiusPerAgent;
        _controller.SetCutoutRadius(radius);
      } else {
        _controller.DisableCutout();
      }
    }

    #endregion

    #region Editor

    [Button("Auto-Size Trigger"), ShowIf(nameof(UsesPhysicsTrigger))]
    private void AutoSizeTrigger() {
      if (_triggerCollider == null || _structure == null) return;
      
      var footprint = _structure.footprint;
      var cellSize = BuildingConstants.CellSize;
      
      _triggerCollider.center = new Vector3(
        footprint.x * cellSize * 0.5f,
        cellSize * 0.5f,
        footprint.y * cellSize * 0.5f
      );
      
      _triggerCollider.size = new Vector3(
        footprint.x * cellSize,
        cellSize * 2f,
        footprint.y * cellSize
      );
    }

    #endregion
  }
  
  public enum CutoutTriggerMode {
    AgentsInside,    // check all agents vs bounds
    PhysicsTrigger,  // use OnTrigger with agent colliders
    CameraInside,    // camera-based trigger
    Manual           // controlled externally
  }
}
