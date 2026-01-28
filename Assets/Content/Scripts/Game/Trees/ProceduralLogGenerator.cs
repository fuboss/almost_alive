using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.Game.Trees {
  public static class ProceduralLogGenerator {
    public static Mesh CreateLogMesh(Bounds treeBounds, TreeFallConfigSO configSO) {
      var config = configSO.Data;
      var mesh = new Mesh { name = "Log_Procedural" };

      float length = treeBounds.size.y * 0.85f;
      float diameter = Mathf.Max(treeBounds.size.x, treeBounds.size.z) * config.logDiameterRatio;
      float radius = diameter * 0.5f;
      int sides = config.logSides;

      Debug.Log($"[ProceduralLogGenerator] Creating log mesh: length={length:F2}, diameter={diameter:F2}, sides={sides}");

      var vertices = new List<Vector3>();
      var triangles = new List<int>();
      var uvs = new List<Vector2>();

      float halfLength = length * 0.5f;

      // Create vertices for cylinder sides (log lies along X axis when spawned, will be rotated)
      for (int i = 0; i < sides; i++) {
        float angle = i * Mathf.PI * 2f / sides;
        float y = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        // Left cap vertex
        vertices.Add(new Vector3(-halfLength, y, z));
        // Right cap vertex
        vertices.Add(new Vector3(halfLength, y, z));

        float u = (float)i / sides;
        uvs.Add(new Vector2(u, 0));
        uvs.Add(new Vector2(u, 1));
      }

      int capCenterLeft = vertices.Count;
      vertices.Add(new Vector3(-halfLength, 0, 0));
      uvs.Add(new Vector2(0.5f, 0.5f));

      int capCenterRight = vertices.Count;
      vertices.Add(new Vector3(halfLength, 0, 0));
      uvs.Add(new Vector2(0.5f, 0.5f));

      // Build triangles - OUTSIDE faces (correct winding for outward normals)
      for (int i = 0; i < sides; i++) {
        int next = (i + 1) % sides;

        int bl = i * 2;       // bottom-left
        int tl = i * 2 + 1;   // top-left
        int br = next * 2;    // bottom-right
        int tr = next * 2 + 1; // top-right

        // Side quad - outward facing
        triangles.Add(bl); triangles.Add(tl); triangles.Add(br);
        triangles.Add(br); triangles.Add(tl); triangles.Add(tr);

        // Left cap - outward facing (pointing -X)
        triangles.Add(capCenterLeft); triangles.Add(br); triangles.Add(bl);
        // Right cap - outward facing (pointing +X)
        triangles.Add(capCenterRight); triangles.Add(tl); triangles.Add(tr);
      }

      // Add INSIDE faces for double-sided rendering
      int vertexOffset = vertices.Count;
      
      // Duplicate vertices for inside
      for (int i = 0; i < sides; i++) {
        float angle = i * Mathf.PI * 2f / sides;
        float y = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        vertices.Add(new Vector3(-halfLength, y, z));
        vertices.Add(new Vector3(halfLength, y, z));

        float u = (float)i / sides;
        uvs.Add(new Vector2(u, 0));
        uvs.Add(new Vector2(u, 1));
      }

      int capCenterLeftInner = vertices.Count;
      vertices.Add(new Vector3(-halfLength, 0, 0));
      uvs.Add(new Vector2(0.5f, 0.5f));

      int capCenterRightInner = vertices.Count;
      vertices.Add(new Vector3(halfLength, 0, 0));
      uvs.Add(new Vector2(0.5f, 0.5f));

      // Build inside triangles (reversed winding)
      for (int i = 0; i < sides; i++) {
        int next = (i + 1) % sides;

        int bl = vertexOffset + i * 2;
        int tl = vertexOffset + i * 2 + 1;
        int br = vertexOffset + next * 2;
        int tr = vertexOffset + next * 2 + 1;

        // Side quad - inward facing (reversed winding)
        triangles.Add(bl); triangles.Add(br); triangles.Add(tl);
        triangles.Add(br); triangles.Add(tr); triangles.Add(tl);

        // Left cap - inward facing
        triangles.Add(capCenterLeftInner); triangles.Add(bl); triangles.Add(br);
        // Right cap - inward facing
        triangles.Add(capCenterRightInner); triangles.Add(tr); triangles.Add(tl);
      }

      mesh.SetVertices(vertices);
      mesh.SetTriangles(triangles, 0);
      mesh.SetUVs(0, uvs);
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();

      Debug.Log($"[ProceduralLogGenerator] Mesh created: {vertices.Count} verts, {triangles.Count / 3} tris");

      return mesh;
    }

    public static void ApplyLogMesh(GameObject target, Mesh logMesh, Material material) {
      Debug.Log($"[ProceduralLogGenerator] Applying log mesh to {target.name}");
      
      var meshFilter = target.GetComponent<MeshFilter>();
      if (meshFilter == null) {
        meshFilter = target.AddComponent<MeshFilter>();
      }
      meshFilter.sharedMesh = logMesh;

      var meshRenderer = target.GetComponent<MeshRenderer>();
      if (meshRenderer == null) {
        meshRenderer = target.AddComponent<MeshRenderer>();
      }
      meshRenderer.sharedMaterial = material;

      var collider = target.GetComponent<MeshCollider>();
      if (collider != null) {
        collider.sharedMesh = logMesh;
        collider.convex = true;
      }
    }
  }
}
