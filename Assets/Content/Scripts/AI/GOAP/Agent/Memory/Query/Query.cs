using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Memory.Query {
  public class MemoryQuery {
    private string[] _requiredTags;
    private Vector3? _center;
    private float _radius = float.MaxValue;
    private float _minConfidence;
    private bool _includeOutdated;
    private Func<MemorySnapshot, bool> _predicate;
    private Func<MemorySnapshot, float> _sortKey;
    private bool _ascending;
    private int _limit = -1;

    public MemoryQuery WithTags(params string[] tags) {
      _requiredTags = tags;
      return this;
    }

    public MemoryQuery InRadius(Vector3 center, float radius) {
      _center = center;
      _radius = radius;
      return this;
    }

    public MemoryQuery MinConfidence(float confidence) {
      _minConfidence = confidence;
      return this;
    }

    public MemoryQuery IncludeOutdated(bool include = true) {
      _includeOutdated = include;
      return this;
    }

    public MemoryQuery Where(Func<MemorySnapshot, bool> predicate) {
      _predicate = predicate;
      return this;
    }

    public MemoryQuery OrderBy(Func<MemorySnapshot, float> keySelector, bool ascending = true) {
      _sortKey = keySelector;
      _ascending = ascending;
      return this;
    }

    public MemoryQuery Take(int count) {
      _limit = count;
      return this;
    }

    public MemorySnapshot[] Execute(AgentMemory memory) {
      IEnumerable<MemorySnapshot> results;

      // Start with tag filtering if specified
      if (_requiredTags != null && _requiredTags.Length > 0) {
        results = memory.GetWithAllTags(_requiredTags, _includeOutdated);
      }
      else {
        results = memory.GetAll(_includeOutdated);
      }

      // Apply filters
      if (_center.HasValue) {
        var center = _center.Value;
        var radius = _radius;
        results = results.Where(s => Vector3.Distance(s.location, center) <= radius);
      }

      if (_minConfidence > 0f) {
        results = results.Where(s => s.confidence >= _minConfidence);
      }

      if (_predicate != null) {
        results = results.Where(_predicate);
      }

      // Sort if specified
      if (_sortKey != null) {
        results = _ascending
          ? results.OrderBy(_sortKey)
          : results.OrderByDescending(_sortKey);
      }

      // Limit if specified
      if (_limit > 0) {
        results = results.Take(_limit);
      }

      return results.ToArray();
    }

    public MemorySnapshot ExecuteFirst(AgentMemory memory) {
      return Execute(memory).FirstOrDefault();
    }
  }
}