using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.World.Grid.Presentation {
  /// <summary>
  /// Efficient tile mesh renderer using GPU instancing.
  /// Renders multiple grid tiles with per-instance colors, terrain height adaptation.
  /// Use for hover highlights, footprints, selection overlays, slot visualization.
  /// </summary>
  public class TileMeshRenderer : MonoBehaviour {
    [Header("Settings")] [SerializeField] private Material _tileMaterial;
    [SerializeField] private float _heightOffset = 0.05f;
    [SerializeField] private bool _adaptToTerrain = true;

    private Mesh _quadMesh;
    private Terrain _terrain;

    // Active tiles: coord -> tile data
    private readonly Dictionary<GroundCoord, TileData> _activeTiles = new();

    // Shader property IDs
    private static readonly int InstanceColorProperty = Shader.PropertyToID("_InstanceColor");
    private static readonly int InstanceBorderOnlyProperty = Shader.PropertyToID("_InstanceBorderOnly");

    // GPU Instancing buffers
    private Matrix4x4[] _matrices;
    private MaterialPropertyBlock _propertyBlock;
    private Vector4[] _colors;
    private float[] _borderOnly;
    private bool _isDirty;

    // Hybrid approach: use GameObjects for small counts, instancing for large
    private const int HYBRID_THRESHOLD = 4;
    private readonly List<GameObject> _tileObjects = new();
    private readonly Stack<GameObject> _objectPool = new();

    private struct TileData {
      public Color color;
      public bool borderOnly;
      public float[] cornerHeights; // 4 corners for terrain adaptation
    }

    public void Initialize(Material material = null) {
      if (material != null) {
        _tileMaterial = material;
      }

      _terrain = Terrain.activeTerrain;
      _quadMesh = CreateQuadMesh();
      _propertyBlock = new MaterialPropertyBlock();

      // Pre-allocate buffers for instancing
      _matrices = new Matrix4x4[64];
      _colors = new Vector4[64];
      _borderOnly = new float[64];

      Debug.Log("[TileMeshRenderer] Initialized");
    }

    #region Public API

    /// <summary>
    /// Show a single tile at grid coordinate.
    /// </summary>
    public void ShowTile(GroundCoord coord, Color color, bool borderOnly = false) {
      var data = new TileData {
        color = color,
        borderOnly = borderOnly,
        cornerHeights = _adaptToTerrain ? SampleCornerHeights(coord, 0.2f) : null
      };

      _activeTiles[coord] = data;
      _isDirty = true;
    }

    /// <summary>
    /// Show multiple tiles for a footprint (structure placement).
    /// </summary>
    public void ShowFootprint(GroundCoord origin, Vector2Int size, Color color, bool borderOnly = false) {
      for (int x = 0; x < size.x; x++) {
        for (int z = 0; z < size.y; z++) {
          var coord = new GroundCoord(origin.x + x, origin.z + z);
          ShowTile(coord, color, borderOnly);
        }
      }
    }

    /// <summary>
    /// Show footprint centered at world position (for structures).
    /// </summary>
    public void ShowFootprintAtWorldPos(Vector3 worldPos, Vector2Int size, Color color, bool borderOnly = false) {
      // Calculate origin so footprint is centered on worldPos
      var centerCoord = GroundCoord.FromWorld(worldPos);
      var originX = centerCoord.x - (size.x - 1) / 2;
      var originZ = centerCoord.z - (size.y - 1) / 2;
      var origin = new GroundCoord(originX, originZ);

      ShowFootprint(origin, size, color, borderOnly);
    }

    /// <summary>
    /// Hide a single tile.
    /// </summary>
    public void HideTile(GroundCoord coord) {
      if (_activeTiles.Remove(coord)) {
        _isDirty = true;
      }
    }

    /// <summary>
    /// Hide all tiles.
    /// </summary>
    public void HideAll() {
      _activeTiles.Clear();
      _isDirty = true;

      // Return all objects to pool
      foreach (var obj in _tileObjects) {
        obj.SetActive(false);
        _objectPool.Push(obj);
      }

      _tileObjects.Clear();
    }

    /// <summary>
    /// Update tile color without recreating.
    /// </summary>
    public void UpdateTileColor(GroundCoord coord, Color newColor) {
      if (_activeTiles.TryGetValue(coord, out var data)) {
        data.color = newColor;
        _activeTiles[coord] = data;
        _isDirty = true;
      }
    }

    /// <summary>
    /// Refresh terrain heights for all active tiles.
    /// Call this if terrain has changed or camera moved significantly.
    /// </summary>
    public void RefreshTerrainHeights() {
      if (!_adaptToTerrain) return;

      var coords = new List<GroundCoord>(_activeTiles.Keys);
      foreach (var coord in coords) {
        var data = _activeTiles[coord];
        data.cornerHeights = SampleCornerHeights(coord);
        _activeTiles[coord] = data;
      }

      _isDirty = true;
    }

    public int ActiveTileCount => _activeTiles.Count;

    #endregion

    #region Rendering

    private void LateUpdate() {
      if (_activeTiles.Count == 0) return;

      if (_activeTiles.Count <= HYBRID_THRESHOLD) {
        // Use GameObjects for small tile counts (simpler, good for hover)
        RenderWithGameObjects();
      }
      else {
        // Use GPU instancing for larger counts (footprints, selections)
        RenderWithInstancing();
      }
    }

    private void RenderWithGameObjects() {
      // Hide instancing objects
      // Ensure we have enough GameObjects
      while (_tileObjects.Count < _activeTiles.Count) {
        var obj = GetOrCreateTileObject();
        _tileObjects.Add(obj);
      }

      // Hide excess objects
      for (int i = _activeTiles.Count; i < _tileObjects.Count; i++) {
        _tileObjects[i].SetActive(false);
      }

      // Update active objects
      int idx = 0;
      foreach (var kvp in _activeTiles) {
        var coord = kvp.Key;
        var data = kvp.Value;
        var obj = _tileObjects[idx];

        // Position and scale
        var worldPos = coord.ToWorld();
        var y = GetAverageHeight(data.cornerHeights) + _heightOffset;
        obj.transform.position = new Vector3(worldPos.x, y, worldPos.z);
        obj.transform.localScale = new Vector3(WorldGrid.cellSize, 1f, WorldGrid.cellSize);

        // Color via MaterialPropertyBlock
        var renderer = obj.GetComponent<MeshRenderer>();
        _propertyBlock.SetVector(InstanceColorProperty, data.color);
        _propertyBlock.SetFloat(InstanceBorderOnlyProperty, data.borderOnly ? 1f : 0f);
        renderer.SetPropertyBlock(_propertyBlock);

        obj.SetActive(true);
        idx++;
      }

      _isDirty = false;
    }

    private void RenderWithInstancing() {
      // Hide GameObjects
      foreach (var obj in _tileObjects) {
        obj.SetActive(false);
      }

      // Ensure buffer capacity
      if (_matrices.Length < _activeTiles.Count) {
        int newSize = Mathf.NextPowerOfTwo(_activeTiles.Count);
        _matrices = new Matrix4x4[newSize];
        _colors = new Vector4[newSize];
        _borderOnly = new float[newSize];
      }

      // Build instance data
      int idx = 0;
      foreach (var kvp in _activeTiles) {
        var coord = kvp.Key;
        var data = kvp.Value;

        var worldPos = coord.ToWorld();
        var y = GetAverageHeight(data.cornerHeights) + _heightOffset;
        var pos = new Vector3(worldPos.x, y, worldPos.z);
        var scale = new Vector3(WorldGrid.cellSize, 1f, WorldGrid.cellSize);

        _matrices[idx] = Matrix4x4.TRS(pos, Quaternion.identity, scale);
        _colors[idx] = data.color;
        _borderOnly[idx] = data.borderOnly ? 1f : 0f;
        idx++;
      }

      // Set per-instance data
      _propertyBlock.SetVectorArray(InstanceColorProperty, _colors);
      _propertyBlock.SetFloatArray(InstanceBorderOnlyProperty, _borderOnly);

      // Draw instanced
      Graphics.DrawMeshInstanced(
        _quadMesh,
        0,
        _tileMaterial,
        _matrices,
        _activeTiles.Count,
        _propertyBlock,
        UnityEngine.Rendering.ShadowCastingMode.Off,
        false
      );

      _isDirty = false;
    }

    #endregion

    #region Terrain Sampling

    private float[] SampleCornerHeights(GroundCoord coord, float extraYOffset = 0f) {
      var cellSize = WorldGrid.cellSize;
      var worldPos = coord.ToWorld();
      var halfSize = cellSize * 0.5f;

      // 4 corners: BL, BR, TR, TL
      var heights = new float[4];
      heights[0] = GetTerrainHeight(worldPos.x - halfSize, worldPos.z - halfSize) + extraYOffset;
      heights[1] = GetTerrainHeight(worldPos.x + halfSize, worldPos.z - halfSize) + extraYOffset;
      heights[2] = GetTerrainHeight(worldPos.x + halfSize, worldPos.z + halfSize) + extraYOffset;
      heights[3] = GetTerrainHeight(worldPos.x - halfSize, worldPos.z + halfSize) + extraYOffset;

      return heights;
    }

    private readonly RaycastHit[] buffer = new RaycastHit[32];

    private float GetTerrainHeight(float x, float z) {
      if (_terrain != null) {
        return _terrain.SampleHeight(new Vector3(x, 0, z));
      }

      // Fallback: raycast

      if (Physics.RaycastNonAlloc(new Vector3(x, 1000f, z), Vector3.down, buffer) > 0) {
        return buffer[0].point.y;
      }

      return 0f;
    }

    private float GetAverageHeight(float[] cornerHeights) {
      if (cornerHeights == null || cornerHeights.Length < 4) return 0f;
      return (cornerHeights[0] + cornerHeights[1] + cornerHeights[2] + cornerHeights[3]) * 0.25f;
    }

    #endregion

    #region Mesh & Object Creation

    private Mesh CreateQuadMesh() {
      var mesh = new Mesh { name = "TileQuad" };

      // Vertices (XZ plane, Y=0, centered at origin)
      mesh.vertices = new Vector3[] {
        new(-0.5f, 0f, -0.5f),
        new(0.5f, 0f, -0.5f),
        new(0.5f, 0f, 0.5f),
        new(-0.5f, 0f, 0.5f)
      };

      // UVs (0-1 range for border calculation)
      mesh.uv = new Vector2[] {
        new(0, 0),
        new(1, 0),
        new(1, 1),
        new(0, 1)
      };

      // Triangles
      mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

      // Normals (up)
      mesh.normals = new Vector3[] {
        Vector3.up, Vector3.up, Vector3.up, Vector3.up
      };

      mesh.RecalculateBounds();
      return mesh;
    }

    private GameObject GetOrCreateTileObject() {
      if (_objectPool.Count > 0) {
        return _objectPool.Pop();
      }

      var obj = new GameObject("Tile");
      obj.transform.SetParent(transform, false);

      var filter = obj.AddComponent<MeshFilter>();
      filter.sharedMesh = _quadMesh;

      var renderer = obj.AddComponent<MeshRenderer>();
      renderer.sharedMaterial = _tileMaterial;
      renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      renderer.receiveShadows = false;

      return obj;
    }

    #endregion

    private void OnDestroy() {
      if (_quadMesh != null) {
        Destroy(_quadMesh);
      }

      foreach (var obj in _tileObjects) {
        if (obj != null) Destroy(obj);
      }

      while (_objectPool.Count > 0) {
        var obj = _objectPool.Pop();
        if (obj != null) Destroy(obj);
      }
    }
  }
}