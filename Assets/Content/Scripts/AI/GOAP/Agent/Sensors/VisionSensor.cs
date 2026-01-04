using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.Game;
using UnityEngine;
using UnityEngine.Rendering;

namespace Content.Scripts.AI.GOAP.Agent.Sensors {
  //todo: перенести логику гизмо в отдельный модуль
  [RequireComponent(typeof(SphereCollider))]
  public class VisionSensor : MonoBehaviour {
    [SerializeField] private float _detectionRadius = 5f;

    [SerializeField] [Tooltip("Field of view in degrees (cone centered on forward)")]
    private float _fieldOfViewAngle = 90f;

    [SerializeField] [Tooltip("How often to re-evaluate actors inside the trigger")]
    private float _checkInterval = 0.2f;


    [SerializeField] [Tooltip("Also draw FoV in Game view during Play Mode")]
    private bool _drawInGameView = true;

    [SerializeField] [Tooltip("Draw FoV gizmo in editor/game view")]
    private bool _drawGizmos = true;

    [SerializeField] [Tooltip("Draw solid (semi-transparent) mesh for FoV")]
    private bool _drawSolid = true;

    [SerializeField] private Color _solidColor = new(0f, 0.6f, 1f, 0.12f);
    [SerializeField] private Color _wireColor = new(0f, 0.6f, 1f, 0.9f);
    [SerializeField] [Range(6, 64)] private int _coneSegments = 24;


    private readonly HashSet<ActorDescription> _candidates = new();
    private readonly HashSet<ActorDescription> _visible = new();

    private Mesh _fovMesh;

    private Material _runtimeMaterial;
    private float _timer;
    private SphereCollider _trigger;

    // Public read-only access to currently visible actors
    public IReadOnlyCollection<ActorDescription> VisibleActors => _visible;

    private void Awake() {
      _trigger = GetComponent<SphereCollider>();
      _trigger.isTrigger = true;
      _trigger.radius = _detectionRadius;
      CreateRuntimeMaterial();
      RebuildFovMesh();
    }

    private void Update() {
      OnRenderObject();

      _timer += Time.deltaTime;
      if (_timer < _checkInterval) return;
      _timer = 0f;
      ReevaluateCandidates();
    }

    private void OnDisable() {
      if (_runtimeMaterial == null) return;
      DestroyImmediate(_runtimeMaterial);
      _runtimeMaterial = null;
    }

    private void OnDrawGizmos() {
      if (!_drawGizmos || _fovMesh == null) return;

      // draw solid semi-transparent mesh first
      if (_drawSolid) {
        var prevColor = Gizmos.color;
        Gizmos.color = _solidColor;
        Gizmos.DrawMesh(_fovMesh, transform.position, transform.rotation);
        Gizmos.color = prevColor;
      }

      // draw wireframe overlay
      var prevColor2 = Gizmos.color;
      Gizmos.color = _wireColor;
      Gizmos.DrawWireMesh(_fovMesh, transform.position, transform.rotation);
      Gizmos.color = prevColor2;
    }

    private void OnRenderObject() {
      if (!_drawInGameView || _fovMesh == null || _runtimeMaterial == null) return;
      if (!Application.isPlaying) return;
      var matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
      Graphics.DrawMesh(_fovMesh, matrix, _runtimeMaterial, gameObject.layer);
    }

    private void OnTriggerEnter(Collider other) {
      var actor = other.GetComponentInParent<ActorDescription>();
      if (actor == null) return;
      if (!_candidates.Add(actor)) return;
      if (!IsInFov(actor.transform.position)) return;
      if (_visible.Add(actor)) OnActorEntered.Invoke(actor);
    }

    private void OnTriggerExit(Collider other) {
      var actor = other.GetComponentInParent<ActorDescription>();
      if (actor == null) return;
      _candidates.Remove(actor);
      if (_visible.Remove(actor)) OnActorExited.Invoke(actor);
    }

    private void OnValidate() {
      // ensure collider radius and mesh update in editor when properties change
      if (TryGetComponent(out SphereCollider sc)) {
        sc.radius = Mathf.Max(0f, _detectionRadius);
        sc.isTrigger = true;
        _trigger = sc;
      }

      _fieldOfViewAngle = Mathf.Clamp(_fieldOfViewAngle, 0f, 360f);
      _coneSegments = Mathf.Clamp(_coneSegments, 6, 64);
      RebuildFovMesh();
      if (_runtimeMaterial != null) _runtimeMaterial.SetColor("_Color", _solidColor);
    }

    // Event fired when an actor becomes visible
    public event Action<ActorDescription> OnActorEntered = delegate { };

    // Event fired when an actor is no longer visible
    public event Action<ActorDescription> OnActorExited = delegate { };

    private void ReevaluateCandidates() {
      var snapshot = new List<ActorDescription>(_candidates);
      foreach (var actor in snapshot) {
        if (actor == null) {
          // cleanup destroyed objects
          _candidates.Remove(actor);
          if (_visible.Remove(actor)) OnActorExited.Invoke(actor);
          continue;
        }

        var inFov = IsInFov(actor.transform.position);
        var currentlyVisible = _visible.Contains(actor);

        if (inFov && !currentlyVisible) {
          _visible.Add(actor);
          OnActorEntered.Invoke(actor);
        }
        else if (!inFov && currentlyVisible) {
          _visible.Remove(actor);
          OnActorExited.Invoke(actor);
        }
      }
    }

    private bool IsInFov(Vector3 targetPosition) {
      var dir = targetPosition - transform.position;
      if (dir.sqrMagnitude <= Mathf.Epsilon) return true;
      dir.Normalize();
      var halfAngle = _fieldOfViewAngle * 0.5f;
      return Vector3.Angle(transform.forward, dir) <= halfAngle;
    }

    public void SetDetectionRadius(float radius) {
      _detectionRadius = Mathf.Max(0f, radius);
      if (_trigger != null) _trigger.radius = _detectionRadius;
    }

    public void SetFieldOfView(float degrees) {
      _fieldOfViewAngle = Mathf.Clamp(degrees, 0f, 360f);
    }

    public IEnumerable<ActorDescription> ObjectsWithTagsInView(string[] tags) {
      foreach (var actor in _visible) {
        if (actor == null || actor.descriptionData == null) continue;
        if (!IsInFov(actor.transform.position)) continue;
        if (!tags.All(t => actor.descriptionData.tags.Contains(t))) continue;

        yield return actor;
      }
    }

    public bool HasObjectsWithTagsInView(string[] tags) {
      return ObjectsWithTagsInView(tags).Any();
    }

    // create a simple transparent material for runtime drawing
    private void CreateRuntimeMaterial() {
      if (_runtimeMaterial != null) return;
      var shader = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Unlit/Color");
      if (shader == null) return;
      _runtimeMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
      _runtimeMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
      _runtimeMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
      _runtimeMaterial.SetInt("_ZWrite", 0);
      _runtimeMaterial.renderQueue = 3000;
      _runtimeMaterial.SetColor("_Color", _solidColor);
    }

    private void RebuildFovMesh() {
      if (_fovMesh != null) {
        DestroyImmediate(_fovMesh);
        _fovMesh = null;
      }

      if (_fieldOfViewAngle <= 0f || _detectionRadius <= 0f) return;

      var halfAngle = _fieldOfViewAngle * 0.5f;
      var segments = Mathf.Max(6, _coneSegments);

      var verts = new List<Vector3>();
      var tris = new List<int>();

      // center
      verts.Add(Vector3.zero);

      // outer arc (flat on Y=0, forward is +Z)
      for (var i = 0; i <= segments; i++) {
        var t = -halfAngle + (float)i / segments * _fieldOfViewAngle;
        var rad = Mathf.Deg2Rad * t;
        var x = Mathf.Sin(rad) * _detectionRadius;
        var z = Mathf.Cos(rad) * _detectionRadius;
        verts.Add(new Vector3(x, 0f, z));
      }

      // triangles center -> outer i+1 -> outer i+2 (matching winding used before)
      for (var i = 0; i < segments; i++) {
        var curr = 1 + i;
        var next = 1 + i + 1;
        tris.Add(0);
        tris.Add(next);
        tris.Add(curr);
      }

      _fovMesh = new Mesh {
        name = "VisionSensor_FOV_Mesh"
      };
      _fovMesh.SetVertices(verts);
      _fovMesh.SetTriangles(tris, 0);
      _fovMesh.RecalculateNormals();
      _fovMesh.RecalculateBounds();
    }
  }
}