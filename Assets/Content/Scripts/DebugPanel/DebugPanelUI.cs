using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Content.Scripts.DebugPanel {
  public class DebugPanelUI : MonoBehaviour {
    private DebugModule _debugModule;
    
    // UI Elements
    private GameObject _contentPanel;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _breadcrumbText;
    private Dictionary<DebugCategory, GameObject> _categoryButtons = new();
    private GameObject _activeDropdown;
    private readonly List<GameObject> _dropdownButtonPool = new();
    private readonly List<MenuEntry> _currentMenuEntries = new();
    private int _activePooledButtonsCount;
    
    // Navigation state
    private DebugCategory? _activeCategory;
    private readonly Stack<string> _pathStack = new();
    
    // Cached references
    private RectTransform _rectTransform;
    private readonly Vector3[] _cornersBuffer = new Vector3[4];

    public void Initialize(DebugModule debugModule) {
      _debugModule = debugModule;
      _rectTransform = transform as RectTransform;
      
      BuildUI();
      
      _debugModule.OnActionSelected += OnActionSelected;
      _debugModule.OnActionCancelled += OnActionCancelled;
      _debugModule.OnStateChanged += OnStateChanged;
    }

    private void BuildUI() {
      // Main panel (top center)
      var mainPanel = CreatePanel("MainPanel", transform);
      var mainLayout = mainPanel.AddComponent<HorizontalLayoutGroup>();
      mainLayout.childAlignment = TextAnchor.UpperCenter;
      mainLayout.spacing = DebugPanelStyles.Spacing;
      mainLayout.padding = new RectOffset(
        (int)DebugPanelStyles.Padding, (int)DebugPanelStyles.Padding, 
        (int)DebugPanelStyles.Padding, (int)DebugPanelStyles.Padding);
      mainLayout.childForceExpandWidth = true;
      mainLayout.childForceExpandHeight = true;
      mainLayout.childControlHeight = true;
      mainLayout.childControlWidth = true;
      
      var mainRect = mainPanel.GetComponent<RectTransform>();
      mainRect.anchorMin = new Vector2(0.5f, 1f);
      mainRect.anchorMax = new Vector2(0.5f, 1f);
      mainRect.pivot = new Vector2(0.5f, 1f);
      mainRect.anchoredPosition = new Vector2(0, DebugPanelStyles.PanelTopOffset);
      
      var mainFitter = mainPanel.AddComponent<ContentSizeFitter>();
      mainFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
      mainFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

      // Content panel
      _contentPanel = CreatePanel("ContentPanel", mainPanel.transform);
      var contentLayout = _contentPanel.AddComponent<HorizontalLayoutGroup>();
      contentLayout.spacing = DebugPanelStyles.Spacing;
      contentLayout.childForceExpandWidth = true;
      contentLayout.childForceExpandHeight = true;
      contentLayout.childControlHeight = true;
      contentLayout.childControlWidth = true;
      
      // Status text
      var statusObj = new GameObject("StatusText", typeof(RectTransform));
      statusObj.transform.SetParent(_contentPanel.transform, false);
      _statusText = statusObj.AddComponent<TextMeshProUGUI>();
      _statusText.text = "Ready";
      _statusText.fontSize = DebugPanelStyles.FontSize;
      _statusText.color = DebugPanelStyles.TextColor;
      _statusText.alignment = TextAlignmentOptions.Center;
      var statusLayout = statusObj.AddComponent<LayoutElement>();
      statusLayout.preferredWidth = DebugPanelStyles.StatusTextWidth;
      statusLayout.preferredHeight = DebugPanelStyles.ButtonHeight;

      // Category buttons
      foreach (DebugCategory category in System.Enum.GetValues(typeof(DebugCategory))) {
        CreateCategoryButton(category, _contentPanel.transform);
      }
    }

    private void CreateCategoryButton(DebugCategory category, Transform parent) {
      var buttonObj = CreateButton($"Btn_{category}", parent, category.ToString(), () => OnCategoryClicked(category));
      var buttonLayout = buttonObj.AddComponent<LayoutElement>();
      buttonLayout.preferredWidth = DebugPanelStyles.ButtonWidth;
      buttonLayout.preferredHeight = DebugPanelStyles.ButtonHeight;
      _categoryButtons[category] = buttonObj;
    }

    private GameObject CreatePanel(string panelName, Transform parent) {
      var panel = new GameObject(panelName, typeof(RectTransform));
      panel.transform.SetParent(parent, false);
      var image = panel.AddComponent<Image>();
      image.color = DebugPanelStyles.PanelColor;
      return panel;
    }

    private GameObject CreateButton(string buttonName, Transform parent, string text, System.Action onClick) {
      var buttonObj = new GameObject(buttonName, typeof(RectTransform));
      buttonObj.transform.SetParent(parent, false);
      
      var image = buttonObj.AddComponent<Image>();
      image.color = DebugPanelStyles.ButtonNormal;
      
      var button = buttonObj.AddComponent<Button>();
      button.targetGraphic = image;
      var colors = button.colors;
      colors.normalColor = DebugPanelStyles.ButtonNormal;
      colors.highlightedColor = DebugPanelStyles.ButtonHover;
      colors.pressedColor = DebugPanelStyles.ButtonActive;
      button.colors = colors;
      button.onClick.AddListener(() => onClick?.Invoke());
      
      var textObj = new GameObject("Text", typeof(RectTransform));
      textObj.transform.SetParent(buttonObj.transform, false);
      var tmp = textObj.AddComponent<TextMeshProUGUI>();
      tmp.text = text;
      tmp.fontSize = DebugPanelStyles.FontSize;
      tmp.color = DebugPanelStyles.TextColor;
      tmp.alignment = TextAlignmentOptions.Center;
      
      var textRect = textObj.GetComponent<RectTransform>();
      textRect.anchorMin = Vector2.zero;
      textRect.anchorMax = Vector2.one;
      textRect.offsetMin = Vector2.zero;
      textRect.offsetMax = Vector2.zero;
      
      return buttonObj;
    }

    private void Update() {
      if (_activeDropdown != null && Mouse.current.leftButton.wasPressedThisFrame) {
        if (!IsPointerOverDropdown()) {
          CloseDropdown();
        }
      }
    }

    private bool IsPointerOverDropdown() {
      if (_activeDropdown == null) return false;
      
      var pointerEventData = new PointerEventData(EventSystem.current) {
        position = Mouse.current.position.ReadValue()
      };
      
      var raycastResults = new List<RaycastResult>();
      EventSystem.current.RaycastAll(pointerEventData, raycastResults);
      
      foreach (var result in raycastResults) {
        if (result.gameObject.transform.IsChildOf(_activeDropdown.transform) ||
            result.gameObject == _activeDropdown ||
            _categoryButtons.Values.Any(btn => result.gameObject.transform.IsChildOf(btn.transform) || result.gameObject == btn)) {
          return true;
        }
      }
      return false;
    }

    private void OnCategoryClicked(DebugCategory category) {
      CloseDropdown();
      _activeCategory = category;
      _pathStack.Clear();
      
      var actions = _debugModule.Registry.GetActionsByCategory(category).ToList();
      if (actions.Count == 0) {
        Debug.Log($"[DebugPanelUI] No actions for category {category}");
        return;
      }

      var categoryButton = _categoryButtons[category];
      ShowMenuLevel(actions, categoryButton);
    }

    private void ShowMenuLevel(List<IDebugAction> actions, GameObject anchorButton) {
      // Build hierarchical entries from actions
      _currentMenuEntries.Clear();
      var currentPath = string.Join("/", _pathStack);
      
      var groups = new Dictionary<string, List<IDebugAction>>();
      var directActions = new List<IDebugAction>();

      foreach (var action in actions) {
        var relativePath = GetRelativePath(action.displayName, currentPath);
        if (string.IsNullOrEmpty(relativePath)) continue;

        var slashIndex = relativePath.IndexOf('/');
        if (slashIndex > 0) {
          // Has subgroup
          var groupName = relativePath.Substring(0, slashIndex);
          if (!groups.TryGetValue(groupName, out var list)) {
            list = new List<IDebugAction>();
            groups[groupName] = list;
          }
          list.Add(action);
        }
        else {
          // Direct action at this level
          directActions.Add(action);
        }
      }

      // Add group entries (folders)
      foreach (var kvp in groups.OrderBy(k => k.Key)) {
        _currentMenuEntries.Add(new MenuEntry {
          displayText = $"📁 {kvp.Key}",
          isGroup = true,
          groupName = kvp.Key,
          childActions = kvp.Value
        });
      }

      // Add direct action entries
      foreach (var action in directActions) {
        var displayText = GetLastSegment(action.displayName);
        _currentMenuEntries.Add(new MenuEntry {
          displayText = displayText,
          isGroup = false,
          action = action
        });
      }

      // Add back button if in submenu
      if (_pathStack.Count > 0) {
        _currentMenuEntries.Insert(0, new MenuEntry {
          displayText = "⬅ Back",
          isBack = true
        });
      }

      CreateDropdown(anchorButton, _currentMenuEntries);
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

    private void CreateDropdown(GameObject parentButton, List<MenuEntry> entries) {
      _activeDropdown = CreatePanel("Dropdown", transform);
      
      var layout = _activeDropdown.AddComponent<VerticalLayoutGroup>();
      layout.spacing = DebugPanelStyles.DropdownSpacing;
      layout.padding = new RectOffset(
        DebugPanelStyles.DropdownPadding, DebugPanelStyles.DropdownPadding, 
        DebugPanelStyles.DropdownPadding, DebugPanelStyles.DropdownPadding);
      layout.childForceExpandWidth = true;
      layout.childForceExpandHeight = false;
      layout.childControlWidth = true;
      layout.childControlHeight = false;
      
      var rect = _activeDropdown.GetComponent<RectTransform>();
      var parentRect = parentButton.GetComponent<RectTransform>();
      rect.anchorMin = new Vector2(0.5f, 1f);
      rect.anchorMax = new Vector2(0.5f, 1f);
      rect.pivot = new Vector2(0.5f, 1f);
      
      parentRect.GetWorldCorners(_cornersBuffer);
      Vector2 buttonBottomCenter = (_cornersBuffer[0] + _cornersBuffer[3]) / 2f;
      
      RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _rectTransform, buttonBottomCenter, null, out Vector2 localPoint);
      localPoint.y = DebugPanelStyles.DropdownYOffset;
      rect.anchoredPosition = localPoint;
      
      // Calculate height based on entries
      var entryHeight = DebugPanelStyles.OptionHeight + DebugPanelStyles.DropdownSpacing;
      var totalHeight = entries.Count * entryHeight + DebugPanelStyles.DropdownPadding * 2;
      totalHeight = Mathf.Min(totalHeight, 400); // Max height
      rect.sizeDelta = new Vector2(DebugPanelStyles.DropdownWidth, totalHeight);

      // Add breadcrumb if in submenu
      if (_pathStack.Count > 0) {
        var breadcrumb = new GameObject("Breadcrumb", typeof(RectTransform));
        breadcrumb.transform.SetParent(_activeDropdown.transform, false);
        _breadcrumbText = breadcrumb.AddComponent<TextMeshProUGUI>();
        _breadcrumbText.text = string.Join(" / ", _pathStack);
        _breadcrumbText.fontSize = DebugPanelStyles.FontSize - 2;
        _breadcrumbText.color = DebugPanelStyles.TextMuted;
        _breadcrumbText.alignment = TextAlignmentOptions.Left;
        var bcLayout = breadcrumb.AddComponent<LayoutElement>();
        bcLayout.preferredHeight = 20;
      }

      // Create buttons for entries
      _activePooledButtonsCount = entries.Count;
      for (int i = 0; i < entries.Count; i++) {
        var entry = entries[i];
        var optionButton = GetOrCreatePooledButton(i, entry);
        optionButton.transform.SetParent(_activeDropdown.transform, false);
        optionButton.SetActive(true);
      }
    }

    private GameObject GetOrCreatePooledButton(int index, MenuEntry entry) {
      GameObject buttonObj;
      
      if (index < _dropdownButtonPool.Count) {
        buttonObj = _dropdownButtonPool[index];
        UpdatePooledButton(buttonObj, entry);
      }
      else {
        buttonObj = CreateButton($"PooledOption_{index}", transform, entry.displayText, null);
        var optionRect = buttonObj.GetComponent<RectTransform>();
        optionRect.sizeDelta = new Vector2(DebugPanelStyles.DropdownOptionWidth, DebugPanelStyles.OptionHeight);
        _dropdownButtonPool.Add(buttonObj);
        
        var button = buttonObj.GetComponent<Button>();
        int capturedIndex = index;
        button.onClick.AddListener(() => OnPooledButtonClicked(capturedIndex));
      }
      
      // Style based on entry type
      var image = buttonObj.GetComponent<Image>();
      if (entry.isGroup) {
        image.color = DebugPanelStyles.ButtonGroupColor;
      }
      else if (entry.isBack) {
        image.color = DebugPanelStyles.ButtonBackColor;
      }
      else {
        image.color = DebugPanelStyles.ButtonNormal;
      }
      
      return buttonObj;
    }

    private void UpdatePooledButton(GameObject buttonObj, MenuEntry entry) {
      var tmp = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
      if (tmp != null) {
        tmp.text = entry.displayText;
        tmp.alignment = entry.isGroup || entry.isBack ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
      }
    }

    private void OnPooledButtonClicked(int index) {
      if (index < 0 || index >= _currentMenuEntries.Count) return;
      
      var entry = _currentMenuEntries[index];
      
      if (entry.isBack) {
        // Go back one level
        if (_pathStack.Count > 0) _pathStack.Pop();
        var actions = _debugModule.Registry.GetActionsByCategory(_activeCategory.Value).ToList();
        var anchorButton = _categoryButtons[_activeCategory.Value];
        CloseDropdownKeepState();
        ShowMenuLevel(actions, anchorButton);
      }
      else if (entry.isGroup) {
        // Enter submenu
        _pathStack.Push(entry.groupName);
        var anchorButton = _categoryButtons[_activeCategory.Value];
        CloseDropdownKeepState();
        ShowMenuLevel(entry.childActions, anchorButton);
      }
      else if (entry.action != null) {
        // Execute action
        Debug.Log($"[DebugPanelUI] Action clicked: {entry.action.displayName}");
        _debugModule.SelectAction(entry.action);
        CloseDropdown();
      }
    }

    private void CloseDropdownKeepState() {
      if (_activeDropdown != null) {
        for (int i = 0; i < _activePooledButtonsCount && i < _dropdownButtonPool.Count; i++) {
          var button = _dropdownButtonPool[i];
          button.transform.SetParent(transform, false);
          button.SetActive(false);
        }
        _activePooledButtonsCount = 0;
        Destroy(_activeDropdown);
        _activeDropdown = null;
      }
    }

    private void CloseDropdown() {
      CloseDropdownKeepState();
      _currentMenuEntries.Clear();
      _pathStack.Clear();
      _activeCategory = null;
    }

    private void OnActionSelected(IDebugAction action) {
      if (action.actionType == DebugActionType.Instant) {
        _statusText.text = $"Executed: {GetLastSegment(action.displayName)}";
        _statusText.color = DebugPanelStyles.TextColor;
      }
      else {
        _statusText.text = $"{action.category} → {GetLastSegment(action.displayName)}";
        _statusText.color = DebugPanelStyles.TextActive;
      }
    }

    private void OnActionCancelled() {
      _statusText.text = "Ready";
      _statusText.color = DebugPanelStyles.TextColor;
    }

    private void OnStateChanged(DebugState newState) {
      if (newState == DebugState.Idle) {
        CloseDropdown();
      }
      else if (newState == DebugState.Browsing) {
        _statusText.text = "Ready";
        _statusText.color = DebugPanelStyles.TextColor;
      }
    }

    private void OnDisable() => UnsubscribeEvents();
    private void OnDestroy() => UnsubscribeEvents();

    private void UnsubscribeEvents() {
      if (_debugModule != null) {
        _debugModule.OnActionSelected -= OnActionSelected;
        _debugModule.OnActionCancelled -= OnActionCancelled;
        _debugModule.OnStateChanged -= OnStateChanged;
      }
    }

    private class MenuEntry {
      public string displayText;
      public bool isGroup;
      public bool isBack;
      public string groupName;
      public IDebugAction action;
      public List<IDebugAction> childActions;
    }
  }
}
