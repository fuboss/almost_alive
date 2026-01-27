using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUtils;

namespace Content.Scripts.Ui.Layers.BottomBar {
  /// <summary>
  /// Submenu that appears above category button.
  /// Supports hierarchical navigation via "/" in command labels.
  /// </summary>
  public class CommandSubmenu : MonoBehaviour {
    [SerializeField] private RectTransform _panel;
    [SerializeField] private Transform _itemsContainer;
    [SerializeField] private CommandMenuItem _menuItemPrefab;
    [SerializeField] private TMP_Text _breadcrumbText;

    [Header("Layout")]
    [SerializeField] private float _offsetY = 8f;
    [SerializeField] private float _maxHeight = 400f;

    public event Action OnCommandExecuted;

    private readonly List<CommandMenuItem> _itemPool = new();
    private readonly List<MenuEntry> _currentEntries = new();
    private readonly Stack<string> _pathStack = new();
    private IReadOnlyList<ICommand> _allCommands;
    private RectTransform _anchorButton;
    private int _activeItemCount;

    public void Show(IReadOnlyList<ICommand> commands, RectTransform anchorButton) {
      _allCommands = commands;
      _anchorButton = anchorButton;
      _pathStack.Clear();
      
      ShowCurrentLevel();
      gameObject.SetActive(true);
    }

    public void Hide() {
      gameObject.SetActive(false);
      _pathStack.Clear();
    }

    private void ShowCurrentLevel() {
      _currentEntries.Clear();
      var currentPath = string.Join("/", _pathStack);
      
      var groups = new Dictionary<string, List<ICommand>>();
      var directCommands = new List<ICommand>();

      foreach (var cmd in _allCommands) {
        var relativePath = GetRelativePath(cmd.label, currentPath);
        if (string.IsNullOrEmpty(relativePath)) continue;

        var slashIndex = relativePath.IndexOf('/');
        if (slashIndex > 0) {
          // Has subgroup
          var groupName = relativePath.Substring(0, slashIndex);
          if (!groups.TryGetValue(groupName, out var list)) {
            list = new List<ICommand>();
            groups[groupName] = list;
          }
          list.Add(cmd);
        }
        else {
          // Direct command at this level
          directCommands.Add(cmd);
        }
      }

      // Add back button if in submenu
      if (_pathStack.Count > 0) {
        _currentEntries.Add(new MenuEntry {
          displayText = "⬅ Back",
          isBack = true
        });
      }

      // Add group entries (folders)
      foreach (var kvp in groups.OrderBy(k => k.Key)) {
        _currentEntries.Add(new MenuEntry {
          displayText = $"📁 {kvp.Key}",
          isGroup = true,
          groupName = kvp.Key,
          childCommands = kvp.Value
        });
      }

      // Add direct command entries
      foreach (var cmd in directCommands.OrderBy(c => c.order)) {
        var displayText = GetLastSegment(cmd.label);
        _currentEntries.Add(new MenuEntry {
          displayText = displayText,
          isGroup = false,
          command = cmd
        });
      }

      RebuildUI();
    }

    private void RebuildUI() {
      // Hide all pooled items
      foreach (var item in _itemPool) {
        item.gameObject.SetActive(false);
      }

      // Update breadcrumb
      if (_breadcrumbText != null) {
        if (_pathStack.Count > 0) {
          _breadcrumbText.text = string.Join(" / ", _pathStack);
          _breadcrumbText.gameObject.SetActive(true);
        }
        else {
          _breadcrumbText.gameObject.SetActive(false);
        }
      }

      // Create/reuse items
      _activeItemCount = _currentEntries.Count;
      for (int i = 0; i < _currentEntries.Count; i++) {
        var entry = _currentEntries[i];
        var item = GetOrCreateItem(i);
        item.Setup(entry, OnEntryClicked);
        item.gameObject.SetActive(true);
      }

      // Position and size panel
      PositionAbove(_anchorButton);
    }

    private CommandMenuItem GetOrCreateItem(int index) {
      if (index < _itemPool.Count) {
        return _itemPool[index];
      }

      var item = Instantiate(_menuItemPrefab, _itemsContainer);
      _itemPool.Add(item);
      return item;
    }

    private void OnEntryClicked(MenuEntry entry) {
      if (entry.isBack) {
        if (_pathStack.Count > 0) _pathStack.Pop();
        ShowCurrentLevel();
      }
      else if (entry.isGroup) {
        _pathStack.Push(entry.groupName);
        ShowCurrentLevel();
      }
      else if (entry.command != null) {
        if (entry.command.CanExecute()) {
          entry.command.Execute();
          OnCommandExecuted?.Invoke();
        }
      }
    }

    private string GetRelativePath(string fullPath, string currentPath) {
      if (string.IsNullOrEmpty(currentPath)) return fullPath;
      if (!fullPath.StartsWith(currentPath + "/")) return null;
      return fullPath.Substring(currentPath.Length + 1);
    }

    private string GetLastSegment(string path) {
      var lastSlash = path.LastIndexOf('/');
      return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
    }

    private void PositionAbove(RectTransform anchor) {
      if (_panel == null || anchor == null) return;

      var anchorPos = anchor.position;
      var anchorRect = anchor.rect;
      
      _panel.position = new Vector3(
        anchorPos.x,
        anchorPos.y + (anchorRect.height * 0.5f) + _offsetY,
        anchorPos.z
      );
      _panel.pivot = new Vector2(0.5f, 0f);

      // Adjust height based on content
      var itemHeight = _menuItemPrefab.GetComponent<RectTransform>().sizeDelta.y + 2f;
      var contentHeight = _currentEntries.Count * itemHeight + 20f;
      contentHeight = Mathf.Min(contentHeight, _maxHeight);
      
      var size = _panel.sizeDelta;
      size.y = contentHeight;
      _panel.sizeDelta = size;
    }

    public void Refresh() {
      foreach (var item in _itemPool) {
        if (item.gameObject.activeSelf) {
          item.Refresh();
        }
      }
    }

    public class MenuEntry {
      public string displayText;
      public bool isGroup;
      public bool isBack;
      public string groupName;
      public ICommand command;
      public List<ICommand> childCommands;
    }
  }
}
