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
    private Dictionary<DebugCategory, GameObject> _categoryButtons = new();
    private GameObject _activeDropdown;
    private readonly List<GameObject> _dropdownButtonPool = new();
    private List<IDebugAction> _currentDropdownActions = new();
    private int _activePooledButtonsCount;
    
    // Cached references to avoid allocations
    private RectTransform _rectTransform;
    private readonly Vector3[] _cornersBuffer = new Vector3[4];

    public void Initialize(DebugModule debugModule) {
      _debugModule = debugModule;
      _rectTransform = transform as RectTransform;
      
      BuildUI();
      
      // Subscribe to events
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
      
      // Add ContentSizeFitter for automatic sizing
      var mainFitter = mainPanel.AddComponent<ContentSizeFitter>();
      mainFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
      mainFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

      // Content panel (main content)
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

      // Create category buttons dynamically
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
      
      // Setup colors
      var colors = button.colors;
      colors.normalColor = DebugPanelStyles.ButtonNormal;
      colors.highlightedColor = DebugPanelStyles.ButtonHover;
      colors.pressedColor = DebugPanelStyles.ButtonActive;
      button.colors = colors;
      
      button.onClick.AddListener(() => onClick?.Invoke());
      
      // Text
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
      // Close dropdown when clicking outside of it
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
        // Check if clicked on dropdown or any of its children, or on category buttons
        if (result.gameObject.transform.IsChildOf(_activeDropdown.transform) ||
            result.gameObject == _activeDropdown ||
            _categoryButtons.Values.Any(btn => result.gameObject.transform.IsChildOf(btn.transform) || result.gameObject == btn)) {
          return true;
        }
      }
      
      return false;
    }

    private void OnCategoryClicked(DebugCategory category) {
      // Close previous dropdown if exists
      CloseDropdown();
      
      // Get actions for this category
      _currentDropdownActions = _debugModule.Registry.GetActionsByCategory(category).ToList();
      
      if (_currentDropdownActions.Count == 0) {
        Debug.Log($"[DebugPanelUI] No actions for category {category}");
        return;
      }
      
      // Create dropdown under category button
      var categoryButton = _categoryButtons[category];
      CreateDropdown(categoryButton, _currentDropdownActions);
    }

    private void CreateDropdown(GameObject parentButton, List<IDebugAction> actions) {
      _activeDropdown = CreatePanel("Dropdown", transform);
      
      var layout = _activeDropdown.AddComponent<VerticalLayoutGroup>();
      layout.spacing = DebugPanelStyles.DropdownSpacing;
      layout.padding = new RectOffset(
        DebugPanelStyles.DropdownPadding, DebugPanelStyles.DropdownPadding, 
        DebugPanelStyles.DropdownPadding, DebugPanelStyles.DropdownPadding);
      layout.childForceExpandWidth = true;
      layout.childForceExpandHeight = true;
      
      var rect = _activeDropdown.GetComponent<RectTransform>();
      
      // Position under button
      var parentRect = parentButton.GetComponent<RectTransform>();
      rect.anchorMin = new Vector2(0.5f, 1f);
      rect.anchorMax = new Vector2(0.5f, 1f);
      rect.pivot = new Vector2(0.5f, 1f);
      
      // Calculate world position of button and convert to canvas local
      parentRect.GetWorldCorners(_cornersBuffer);
      Vector2 buttonBottomCenter = (_cornersBuffer[0] + _cornersBuffer[3]) / 2f;
      
      RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _rectTransform, 
        buttonBottomCenter, 
        null, 
        out Vector2 localPoint
      );
      localPoint.y = DebugPanelStyles.DropdownYOffset;
      rect.anchoredPosition = localPoint;
      rect.sizeDelta = new Vector2(DebugPanelStyles.DropdownWidth, actions.Count * (DebugPanelStyles.OptionHeight + 2) + 10);
      
      // Use pooled buttons for each action
      _activePooledButtonsCount = actions.Count;
      for (int i = 0; i < actions.Count; i++) {
        var action = actions[i];
        var optionButton = GetOrCreatePooledButton(i, action);
        optionButton.transform.SetParent(_activeDropdown.transform, false);
        optionButton.SetActive(true);
      }
    }

    private GameObject GetOrCreatePooledButton(int index, IDebugAction action) {
      GameObject buttonObj;
      
      if (index < _dropdownButtonPool.Count) {
        // Reuse existing button from pool
        buttonObj = _dropdownButtonPool[index];
        UpdatePooledButton(buttonObj, action);
      } else {
        // Create new button and add to pool
        buttonObj = CreateButton($"PooledOption_{index}", transform, action.displayName, null);
        var optionRect = buttonObj.GetComponent<RectTransform>();
        optionRect.sizeDelta = new Vector2(DebugPanelStyles.DropdownOptionWidth, DebugPanelStyles.OptionHeight);
        _dropdownButtonPool.Add(buttonObj);
        
        // Setup click handler that reads current action
        var button = buttonObj.GetComponent<Button>();
        int capturedIndex = index;
        button.onClick.AddListener(() => OnPooledButtonClicked(capturedIndex));
      }
      
      return buttonObj;
    }

    private void UpdatePooledButton(GameObject buttonObj, IDebugAction action) {
      var tmp = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
      if (tmp != null) {
        tmp.text = action.displayName;
      }
    }

    private void OnPooledButtonClicked(int index) {
      if (index >= 0 && index < _currentDropdownActions.Count) {
        OnActionClicked(_currentDropdownActions[index]);
      }
    }

    private void OnActionClicked(IDebugAction action) {
      Debug.Log($"[DebugPanelUI] Action clicked: {action.displayName}");
      _debugModule.SelectAction(action);
      CloseDropdown();
    }

    private void CloseDropdown() {
      if (_activeDropdown != null) {
        // Return pooled buttons to inactive state (reparent to this transform)
        for (int i = 0; i < _activePooledButtonsCount && i < _dropdownButtonPool.Count; i++) {
          var button = _dropdownButtonPool[i];
          button.transform.SetParent(transform, false);
          button.SetActive(false);
        }
        _activePooledButtonsCount = 0;
        _currentDropdownActions.Clear();
        
        Destroy(_activeDropdown);
        _activeDropdown = null;
      }
    }

    private void OnActionSelected(IDebugAction action) {
      if (action.actionType == DebugActionType.Instant) {
        _statusText.text = $"Executed: {action.displayName}";
        _statusText.color = DebugPanelStyles.TextColor;
      } else {
        _statusText.text = $"{action.category} → {action.displayName}";
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
      } else if (newState == DebugState.Browsing) {
        _statusText.text = "Ready";
        _statusText.color = DebugPanelStyles.TextColor;
      }
    }

    private void OnDisable() {
      UnsubscribeEvents();
    }

    private void OnDestroy() {
      UnsubscribeEvents();
    }

    private void UnsubscribeEvents() {
      if (_debugModule != null) {
        _debugModule.OnActionSelected -= OnActionSelected;
        _debugModule.OnActionCancelled -= OnActionCancelled;
        _debugModule.OnStateChanged -= OnStateChanged;
      }
    }
  }
}


