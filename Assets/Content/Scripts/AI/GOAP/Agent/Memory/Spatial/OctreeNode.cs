using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Memory {
  public class OctreeNode<T> where T : class {
    private readonly Bounds _bounds;
    private readonly float _minSize;
    private readonly List<(T item, Vector3 position)> _objects = new();
    private OctreeNode<T>[] _children;
    private const int MaxObjects = 8;

    public OctreeNode(Bounds bounds, float minSize) {
      _bounds = bounds;
      _minSize = minSize;
    }

    public void Add(T item, Vector3 position) {
      if (!_bounds.Contains(position)) return;

      if (_children != null) {
        GetChildNode(position)?.Add(item, position);
        return;
      }

      _objects.Add((item, position));

      if (_objects.Count > MaxObjects && _bounds.size.x > _minSize) {
        Subdivide();
      }
    }

    public void Remove(T item) {
      _objects.RemoveAll(o => o.item == item);
      
      if (_children != null) {
        foreach (var child in _children) {
          child?.Remove(item);
        }
      }
    }

    public void Query(Bounds bounds, List<T> results) {
      if (!_bounds.Intersects(bounds)) return;

      foreach (var obj in _objects) {
        if (bounds.Contains(obj.position)) {
          results.Add(obj.item);
        }
      }

      if (_children != null) {
        foreach (var child in _children) {
          child?.Query(bounds, results);
        }
      }
    }

    public void Clear() {
      _objects.Clear();
      _children = null;
    }

    private void Subdivide() {
      _children = new OctreeNode<T>[8];
      var halfSize = _bounds.size / 2f;
      var quarterSize = halfSize / 2f;

      for (int i = 0; i < 8; i++) {
        var offset = new Vector3(
          (i & 1) == 0 ? -quarterSize.x : quarterSize.x,
          (i & 2) == 0 ? -quarterSize.y : quarterSize.y,
          (i & 4) == 0 ? -quarterSize.z : quarterSize.z
        );
        
        var childBounds = new Bounds(_bounds.center + offset, halfSize);
        _children[i] = new OctreeNode<T>(childBounds, _minSize);
      }

      foreach (var obj in _objects) {
        GetChildNode(obj.position)?.Add(obj.item, obj.position);
      }

      _objects.Clear();
    }

    private OctreeNode<T> GetChildNode(Vector3 position) {
      if (_children == null) return null;

      int index = 0;
      if (position.x > _bounds.center.x) index |= 1;
      if (position.y > _bounds.center.y) index |= 2;
      if (position.z > _bounds.center.z) index |= 4;

      return _children[index];
    }
  }
}