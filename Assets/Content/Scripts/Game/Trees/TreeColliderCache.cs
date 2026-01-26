using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.Game.Trees {
  public static class TreeColliderCache {
    private static readonly Dictionary<string, Mesh> _colliderMeshes = new();

    public static Mesh GetOrCreateColliderMesh(string actorKey, MeshFilter sourceMeshFilter, float groundOffset = 0.5f) {
      if (string.IsNullOrEmpty(actorKey) || sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null) {
        return null;
      }

      var cacheKey = $"{actorKey}_{groundOffset:F2}";
      if (_colliderMeshes.TryGetValue(cacheKey, out var cached)) {
        return cached;
      }

      var sourceMesh = sourceMeshFilter.sharedMesh;
      var colliderMesh = CreateSimplifiedColliderMesh(sourceMesh, groundOffset);
      _colliderMeshes[cacheKey] = colliderMesh;

      Debug.Log($"[TreeColliderCache] Created collider mesh for '{actorKey}', verts: {colliderMesh.vertexCount}, groundOffset: {groundOffset}");
      return colliderMesh;
    }

    private static Mesh CreateSimplifiedColliderMesh(Mesh source, float groundOffset) {
      var bounds = source.bounds;
      var mesh = new Mesh { name = "TreeCollider_Generated" };

      float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.4f;
      float height = bounds.size.y;
      int segments = 6;

      var vertices = new List<Vector3>();
      var triangles = new List<int>();

      var center = bounds.center;
      // Raise bottom to prevent terrain intersection (trees are embedded in ground)
      float bottom = center.y - bounds.extents.y + groundOffset;
      float top = center.y + bounds.extents.y;

      for (int i = 0; i < segments; i++) {
        float angle = i * Mathf.PI * 2f / segments;
        float x = Mathf.Cos(angle) * radius + center.x;
        float z = Mathf.Sin(angle) * radius + center.z;
        vertices.Add(new Vector3(x, bottom, z));
        vertices.Add(new Vector3(x, top, z));
      }

      int bottomCenter = vertices.Count;
      vertices.Add(new Vector3(center.x, bottom, center.z));
      int topCenter = vertices.Count;
      vertices.Add(new Vector3(center.x, top, center.z));

      for (int i = 0; i < segments; i++) {
        int next = (i + 1) % segments;
        int bl = i * 2;
        int tl = i * 2 + 1;
        int br = next * 2;
        int tr = next * 2 + 1;

        triangles.Add(bl); triangles.Add(tl); triangles.Add(br);
        triangles.Add(br); triangles.Add(tl); triangles.Add(tr);

        triangles.Add(bottomCenter); triangles.Add(br); triangles.Add(bl);
        triangles.Add(topCenter); triangles.Add(tl); triangles.Add(tr);
      }

      mesh.SetVertices(vertices);
      mesh.SetTriangles(triangles, 0);
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();

      return mesh;
    }

    public static void Clear() {
      foreach (var mesh in _colliderMeshes.Values) {
        if (mesh != null) Object.Destroy(mesh);
      }
      _colliderMeshes.Clear();
    }
  }
}
