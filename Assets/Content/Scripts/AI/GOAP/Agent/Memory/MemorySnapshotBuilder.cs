using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent.Descriptors;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Scripts.AI.GOAP.Agent {
  public class MemorySnapshotBuilder {
    private bool _hasCreationTimeSet;
    private MemorySnapshot _snap;

    public MemorySnapshotBuilder() {
      _snap = new MemorySnapshot();
      _hasCreationTimeSet = false;
    }

    public static MemorySnapshotBuilder Create() {
      return new MemorySnapshotBuilder();
    }

    public MemorySnapshotBuilder From(MemorySnapshot other) {
      if (other == null) return this;
      _snap.tags = other.tags != null ? new List<string>(other.tags) : null;
      _snap.location = other.location;
      _snap.target = other.target;
      _snap.optionalTargets = other.optionalTargets != null ? new List<Object>(other.optionalTargets) : null;
      _snap.isOutdated = other.isOutdated;
      _snap.confidence = other.confidence;
      _snap.lifetimeSeconds = other.lifetimeSeconds;
      _snap.creationTime = other.creationTime;
      _snap.lastUpdateTime = other.lastUpdateTime;
      _hasCreationTimeSet = true;
      return this;
    }

    public MemorySnapshotBuilder With(DescriptionData descriptionData) {
      if (descriptionData?.tags == null) return this;
      if (_snap.tags == null) _snap.tags = new List<string>();
      foreach (var tag in descriptionData.tags) {
        if (!string.IsNullOrEmpty(tag) && !_snap.tags.Contains(tag))
          _snap.tags.Add(tag);
      }

      _snap.lastUpdateTime = DateTime.UtcNow;
      return this;
    }

    public MemorySnapshotBuilder WithTag(string tag) {
      if (string.IsNullOrEmpty(tag)) return this;
      if (_snap.tags == null) _snap.tags = new List<string>();
      if (!_snap.tags.Contains(tag)) _snap.tags.Add(tag);
      _snap.lastUpdateTime = DateTime.UtcNow;
      return this;
    }

    public MemorySnapshotBuilder WithTags(IEnumerable<string> tags) {
      if (tags == null) return this;
      if (_snap.tags == null) _snap.tags = new List<string>();
      foreach (var t in tags)
        if (!string.IsNullOrEmpty(t) && !_snap.tags.Contains(t))
          _snap.tags.Add(t);

      _snap.lastUpdateTime = DateTime.UtcNow;
      return this;
    }

    public MemorySnapshotBuilder WithLocation(Vector3 location) {
      _snap.location = location;
      _snap.lastUpdateTime = DateTime.UtcNow;
      return this;
    }

    public MemorySnapshotBuilder WithOptionalTarget(Object target) {
      _snap.target = target;
      _snap.lastUpdateTime = DateTime.UtcNow;
      return this;
    }

    public MemorySnapshotBuilder WithOptionalTargets(IEnumerable<Object> targets) {
      if (targets == null)
        _snap.optionalTargets = null;
      else
        _snap.optionalTargets = new List<Object>(targets);

      _snap.lastUpdateTime = DateTime.UtcNow;
      return this;
    }

    public MemorySnapshotBuilder WithLifetime(float seconds) {
      _snap.lifetimeSeconds = seconds;
      _snap.lastUpdateTime = DateTime.UtcNow;
      return this;
    }

    public MemorySnapshotBuilder WithConfidence(float confidence) {
      _snap.confidence = Mathf.Clamp01(confidence);
      _snap.lastUpdateTime = DateTime.UtcNow;
      return this;
    }

    public MemorySnapshotBuilder MarkOutdated(bool outdated = true) {
      _snap.isOutdated = outdated;
      _snap.lastUpdateTime = DateTime.UtcNow;
      return this;
    }

    public MemorySnapshotBuilder WithCreationTime(DateTime time) {
      _snap.creationTime = time;
      _hasCreationTimeSet = true;
      return this;
    }

    public MemorySnapshot Build(bool resetBuilder = true) {
      if (!_hasCreationTimeSet) _snap.creationTime = DateTime.UtcNow;
      if (_snap.lastUpdateTime == DateTime.MinValue) _snap.lastUpdateTime = _snap.creationTime;
      var result = _snap;
      if (resetBuilder) {
        _snap = new MemorySnapshot();
        _hasCreationTimeSet = false;
      }

      return result;
    }

    public MemorySnapshot BuildClone() {
      var clone = new MemorySnapshot {
        tags = _snap.tags != null ? new List<string>(_snap.tags) : null,
        location = _snap.location,
        target = _snap.target,
        optionalTargets = _snap.optionalTargets != null ? new List<Object>(_snap.optionalTargets) : null,
        isOutdated = _snap.isOutdated,
        creationTime = _hasCreationTimeSet ? _snap.creationTime : DateTime.UtcNow,
        lastUpdateTime = _snap.lastUpdateTime == DateTime.MinValue
          ? _hasCreationTimeSet ? _snap.creationTime : DateTime.UtcNow
          : _snap.lastUpdateTime,
        confidence = _snap.confidence,
        lifetimeSeconds = _snap.lifetimeSeconds
      };
      return clone;
    }

    public MemorySnapshotBuilder Reset() {
      _snap.Reset();
      _hasCreationTimeSet = false;
      return this;
    }
  }
}