using System.Collections.Generic;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Game.Camera;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Building.Services {
  /// <summary>
  /// Centralized cutout management via camera spherecast.
  /// Detects obstructions between camera and focus point, applies cutout.
  /// </summary>
  public class CutoutModule : ILateTickable, IInitializable {
    [Inject] private CameraModule _cameraModule;
    private Camera _mainCamera;

    private readonly HashSet<StructureCutoutController> _activeControllers = new();
    private readonly Dictionary<GameObject, StructureCutoutController> _controllerCache = new();

    private float _spherecastRadius = 1.5f;
    private float _maxDistance = 15f;
    private int _obstructionLayer;
    private float _checkInterval = 0.1f;
    private float _nextCheckTime;
    private readonly RaycastHit[] _castBuffer = new RaycastHit[24];

    // Gizmo helper
#if UNITY_EDITOR
    private CutoutModuleGizmoBehaviour _gizmoBehaviour;
#endif

    void IInitializable.Initialize() {
      _obstructionLayer = LayerMask.GetMask("Default", "Actor"); // adjust as needed
      _mainCamera = Camera.main;

      // Create a hidden GameObject to draw Gizmos (visible in Scene view)
#if UNITY_EDITOR
      var existing = GameObject.Find("CutoutModuleGizmo");
      GameObject go;
      if (existing != null) {
        go = existing;
        // make sure it's visible in hierarchy so user can tweak it
        go.hideFlags = HideFlags.None;
        go.SetActive(true);
      } else {
        go = new GameObject("CutoutModuleGizmo");
        go.hideFlags = HideFlags.None; // keep in scene so settings persist
        go.SetActive(true);
      }

      _gizmoBehaviour = go.GetComponent<CutoutModuleGizmoBehaviour>();
      if (_gizmoBehaviour == null) _gizmoBehaviour = go.AddComponent<CutoutModuleGizmoBehaviour>();
      _gizmoBehaviour.Initialize(this);
#endif
    }

    void ILateTickable.LateTick() {
      if (Time.time < _nextCheckTime) return;
      _nextCheckTime = Time.time + _checkInterval;

      UpdateCutout();
    }

    private void UpdateCutout() {
      var cameraState = _cameraModule.GetState();
      var cameraPos = _mainCamera.transform.position;
      var focusPoint = cameraState.FocusPoint;

      // Use camera forward as direction to cast (where camera is looking)
      var cameraForward = _mainCamera.transform.forward;
      var distanceToFocus = Vector3.Distance(cameraPos, focusPoint);
      var drawDistance = Mathf.Min(distanceToFocus, _maxDistance);

      var newActive = new HashSet<StructureCutoutController>();
      // Spherecast from camera in the camera's forward direction
      var size = Physics.SphereCastNonAlloc(cameraPos, _spherecastRadius, cameraForward, _castBuffer,
        drawDistance, _obstructionLayer);

      // gather hit points for gizmo
      var hitPoints = new List<Vector3>(size);
      for (int i = 0; i < size; i++) {
        var hit = _castBuffer[i];
        var controller = GetOrCacheController(hit.collider.gameObject);
        if (controller == null) continue;
        newActive.Add(controller);

        // Update cutout center to hit point
        controller.SetCutoutCenter(hit.point);
        controller.SetCutoutRadius(3f); // configurable

        if (!_activeControllers.Contains(controller)) {
          controller.EnableCutout();
        }

        hitPoints.Add(hit.point);
      }
      
      // Disable controllers that are no longer hit
      foreach (var controller in _activeControllers) {
        if (!newActive.Contains(controller)) {
          controller.DisableCutout();
        }
      }

      _activeControllers.Clear();
      foreach (var c in newActive) _activeControllers.Add(c);

      // Update gizmo visualizer with latest data
#if UNITY_EDITOR
      if (_gizmoBehaviour != null) {
        _gizmoBehaviour.UpdateData(cameraPos, focusPoint, cameraForward, _spherecastRadius, drawDistance, hitPoints.ToArray());
      }
#endif
    }

    private StructureCutoutController GetOrCacheController(GameObject go) {
      if (_controllerCache.TryGetValue(go, out var cached)) return cached;

      // Try hierarchy (object or parents)
      var controller = go.GetComponentInParent<StructureCutoutController>();

      if (controller != null) {
        _controllerCache[go] = controller;
      }

      return controller;
    }

    public void SetSphereCastRadius(float radius) => _spherecastRadius = radius;
    public void SetCheckInterval(float interval) => _checkInterval = interval;
  }

  // Gizmo drawer as a hidden MonoBehaviour. Draws in Scene view using Gizmos.
#if UNITY_EDITOR
  internal class CutoutModuleGizmoBehaviour : MonoBehaviour {
    private Vector3 _cameraPos;
    private Vector3 _focusPoint;
    private float _sphereRadius;
    private float _drawDistance;
    private Vector3[] _hitPoints = new Vector3[0];
    private Vector3 _cameraForward = Vector3.forward;
     // note: no persistent initialized flag; gizmo falls back to Camera/SceneView if data missing

    // Projection mode so user can switch in inspector
    private enum ProjectionMode { CameraY, FocusY, AverageY }
    [SerializeField] private ProjectionMode _projectionMode = ProjectionMode.CameraY;

    public void Initialize(CutoutModule module) {
      // placeholder - data arrives via UpdateData from CutoutModule
    }

    // Now also accept camera forward so gizmo draws along camera look direction.
    public void UpdateData(Vector3 cameraPos, Vector3 focusPoint, Vector3 cameraForward, float sphereRadius, float drawDistance, Vector3[] hitPoints) {
      _cameraPos = cameraPos;
      _focusPoint = focusPoint;
      _cameraForward = (cameraForward.sqrMagnitude > 1e-6f) ? cameraForward.normalized : (focusPoint - cameraPos).normalized;
      _sphereRadius = Mathf.Max(0.001f, sphereRadius);
      _drawDistance = drawDistance;
      _hitPoints = hitPoints ?? new Vector3[0];
    }

    private float GetProjectionY() {
      return _projectionMode switch {
        ProjectionMode.CameraY => _cameraPos.y,
        ProjectionMode.FocusY => _focusPoint.y,
        ProjectionMode.AverageY => (_cameraPos.y + _focusPoint.y) * 0.5f,
        _ => _cameraPos.y,
      };
    }

#pragma warning disable 414
     private void OnDrawGizmos() {
       // Always try to ensure we have sensible camera/focus values.
       Camera cam = Camera.main;
 #if UNITY_EDITOR
       if (cam == null) {
         // try last active
         if (UnityEditor.SceneView.lastActiveSceneView != null && UnityEditor.SceneView.lastActiveSceneView.camera != null) cam = UnityEditor.SceneView.lastActiveSceneView.camera;
         else {
           // iterate scene views and pick first available camera
           foreach (UnityEditor.SceneView sv in UnityEditor.SceneView.sceneViews) {
             if (sv.camera != null) { cam = sv.camera; break; }
           }
         }
       }
 #endif

       // If stored positions are uninitialized (near zero), try to fill them from an editor or main camera.
       if ((_cameraPos.sqrMagnitude < 1e-6f || _focusPoint.sqrMagnitude < 1e-6f) && cam != null) {
         _cameraPos = cam.transform.position;
         _cameraForward = cam.transform.forward;
         if (_drawDistance <= 0f) _drawDistance = 10f;
         _focusPoint = cam.transform.position + cam.transform.forward * Mathf.Min(_drawDistance, 10f);
         if (_sphereRadius <= 0.001f) _sphereRadius = 1.0f;
       }

      // Draw camera marker for debugging
      Gizmos.color = Color.green;
      Gizmos.DrawWireSphere(_cameraPos, Mathf.Max(0.05f, _sphereRadius * 0.1f));
#if UNITY_EDITOR
      Handles.color = Color.white;
      Handles.Label(_cameraPos + Vector3.up * 0.2f, $"CamY: {_cameraPos.y:F2}\nFocusY: {_focusPoint.y:F2}\nMode: {_projectionMode}");
#endif

      // Compute projection Y according to mode
      var projY = GetProjectionY();

      // ensure projY has sensible default when using fallback
      if (float.IsNaN(projY)) projY = _cameraPos.y;

#if UNITY_EDITOR
      // helper label to confirm gizmo active (show after projY is known)
      Handles.Label(new Vector3(_cameraPos.x, projY + 0.25f, _cameraPos.z), "Cutout Gizmo Active");
#endif

      // Draw a marker at camera projection level to make height reference obvious
      Gizmos.color = Color.black;
      Gizmos.DrawWireSphere(new Vector3(_cameraPos.x, projY, _cameraPos.z), Mathf.Max(0.03f, _sphereRadius * 0.08f));

      // Draw line camera -> focus (projected to projection Y for clarity)
      Gizmos.color = Color.cyan;
      var cameraProjFocus = new Vector3(_focusPoint.x, projY, _focusPoint.z);
      Gizmos.DrawLine(_cameraPos, cameraProjFocus);

      // Draw paths along camera forward (real) and projected on projection Y for clarity.
      var dirWorld = (_cameraForward.sqrMagnitude > 1e-6f) ? _cameraForward.normalized : (_focusPoint - _cameraPos).normalized;
      var steps = Mathf.Clamp(Mathf.CeilToInt(_drawDistance / (_sphereRadius * 0.5f)), 1, 256);

      // primary path: use camera forward (yellow) — matches actual SphereCast direction
      Gizmos.color = Color.yellow;
      for (int i = 0; i <= steps; i++) {
        var t = (float)i / steps;
        var pos = _cameraPos + dirWorld * (_drawDistance * t);
        Gizmos.DrawWireSphere(pos, _sphereRadius);
      }

      // overlay smaller blue markers for reference (real positions)
      Gizmos.color = Color.blue;
      for (int i = 0; i <= steps; i++) {
        var t = (float)i / steps;
        var pos = _cameraPos + dirWorld * (_drawDistance * t);
        Gizmos.DrawWireSphere(pos, Mathf.Max(0.02f, _sphereRadius * 0.15f));
      }

      // Draw hit points at their real positions and a projected marker at projection Y with a connector line
      foreach (var p in _hitPoints) {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(p, Mathf.Max(0.05f, _sphereRadius * 0.2f));

        var proj = new Vector3(p.x, projY, p.z);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(proj, _sphereRadius * 0.5f);
        
        // connector
        Gizmos.color = new Color(1f, 0.5f, 0.0f, 1f);
        Gizmos.DrawLine(proj, p);
      }
   }
  }
 #endif

#if UNITY_EDITOR
  // Editor helpers: menu items to select the gizmo object and switch projection modes.
  internal static class CutoutModuleGizmoEditor {
    private const string GizmoName = "CutoutModuleGizmo";

    private static CutoutModuleGizmoBehaviour GetInstance() {
      var go = GameObject.Find(GizmoName);
      if (go == null) return null;
      return go.GetComponent<CutoutModuleGizmoBehaviour>();
    }

    [MenuItem("Tools/Cutout Gizmo/Select Gizmo")] 
    private static void SelectGizmo() {
      var go = GameObject.Find(GizmoName);
      if (go == null) {
        EditorUtility.DisplayDialog("Cutout Gizmo", "Gizmo object not found. Ensure the game is running or module initialized.", "OK");
        return;
      }
      Selection.activeGameObject = go;
    }

    [MenuItem("Tools/Cutout Gizmo/Projection/CameraY")] 
    private static void SetCameraY() => SetProjectionMode(0);

    [MenuItem("Tools/Cutout Gizmo/Projection/FocusY")] 
    private static void SetFocusY() => SetProjectionMode(1);

    [MenuItem("Tools/Cutout Gizmo/Projection/AverageY")] 
    private static void SetAverageY() => SetProjectionMode(2);

    private static void SetProjectionMode(int index) {
      var inst = GetInstance();
      if (inst == null) {
        EditorUtility.DisplayDialog("Cutout Gizmo", "Gizmo object not found. Ensure the game is running or module initialized.", "OK");
        return;
      }
      var so = new SerializedObject(inst);
      var prop = so.FindProperty("_projectionMode");
      if (prop != null) {
        prop.intValue = index;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(inst);
      }
    }

    [InitializeOnLoadMethod]
    private static void EnsureGizmoExists() {
      // Runs on editor load / assembly reload to ensure gizmo object exists in scene.
      var go = GameObject.Find(GizmoName);
      if (go == null) {
        go = new GameObject(GizmoName);
        go.hideFlags = HideFlags.None;
        go.AddComponent<CutoutModuleGizmoBehaviour>();
        go.SetActive(true);
      } else {
        // ensure component exists
        if (go.GetComponent<CutoutModuleGizmoBehaviour>() == null) go.AddComponent<CutoutModuleGizmoBehaviour>();
        go.hideFlags = HideFlags.None;
        go.SetActive(true);
      }
    }
   }
 #endif

 }
