using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Camp {
  public class CampSetup : MonoBehaviour {
    [SerializeField] private CampSpot[] _campSpots;

    public IReadOnlyList<CampSpot> spots => _campSpots;
    public int spotCount => _campSpots?.Length ?? 0;

    /// <summary>Returns first empty spot matching any of the tags, or null.</summary>
    public CampSpot GetEmptySpot(params string[] tags) {
      if (_campSpots == null) return null;
      return _campSpots.FirstOrDefault(s => s.isEmpty && s.AcceptsAny(tags));
    }

    /// <summary>Returns first empty spot regardless of tags.</summary>
    public CampSpot GetAnyEmptySpot() {
      return _campSpots?.FirstOrDefault(s => s.isEmpty);
    }

    /// <summary>Returns all empty spots.</summary>
    public IEnumerable<CampSpot> GetEmptySpots() {
      return _campSpots?.Where(s => s.isEmpty) ?? Enumerable.Empty<CampSpot>();
    }

    /// <summary>Returns all spots that need a specific tag built.</summary>
    public IEnumerable<CampSpot> GetSpotsNeedingTag(string tag) {
      return _campSpots?.Where(s => s.isEmpty && s.preferredTags.Contains(tag)) 
             ?? Enumerable.Empty<CampSpot>();
    }

    public bool allSpotsFilled => _campSpots != null && _campSpots.All(s => !s.isEmpty);

    [Button]
    private void CollectSpots() {
      _campSpots = GetComponentsInChildren<CampSpot>();
    }
  }
}
