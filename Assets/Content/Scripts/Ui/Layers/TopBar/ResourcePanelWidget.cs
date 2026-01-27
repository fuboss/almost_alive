using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.Ui.Layers.TopBar {
  /// <summary>
  /// Displays colony resources. Easily extensible via ResourceEntry.
  /// </summary>
  public class ResourcePanelWidget : MonoBehaviour {
    [SerializeField] private Transform _container;
    [SerializeField] private ResourceEntryWidget _entryPrefab;

    private readonly Dictionary<string, ResourceEntryWidget> _entries = new();

    /// <summary>
    /// Register a resource to display. Call from resource managers.
    /// </summary>
    public void RegisterResource(string id, Sprite icon, Func<int> getter) {
      if (_entries.ContainsKey(id)) return;

      var entry = Instantiate(_entryPrefab, _container);
      entry.Setup(id, icon, getter);
      _entries[id] = entry;
    }

    /// <summary>
    /// Remove a resource from display.
    /// </summary>
    public void UnregisterResource(string id) {
      if (!_entries.TryGetValue(id, out var entry)) return;
      _entries.Remove(id);
      Destroy(entry.gameObject);
    }

    /// <summary>
    /// Force refresh all resource values.
    /// </summary>
    public void RefreshAll() {
      foreach (var entry in _entries.Values) {
        entry.Refresh();
      }
    }

    private void Update() {
      // Refresh periodically (could optimize with dirty flag)
      RefreshAll();
    }
  }
}
