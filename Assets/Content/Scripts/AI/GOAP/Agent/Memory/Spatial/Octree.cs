using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Memory {
  public class Octree<T> where T : class {
    private readonly OctreeNode<T> _root;
    private readonly float _minNodeSize;

    public Octree(Bounds bounds, float minNodeSize) {
      _root = new OctreeNode<T>(bounds, minNodeSize);
      _minNodeSize = minNodeSize;
    }

    public void Add(T item, Vector3 position) {
      _root.Add(item, position);
    }

    public void Remove(T item) {
      _root.Remove(item);
    }

    public List<T> Query(Bounds bounds) {
      var results = new List<T>();
      _root.Query(bounds, results);
      return results;
    }

    public void Clear() {
      _root.Clear();
    }
  }
}