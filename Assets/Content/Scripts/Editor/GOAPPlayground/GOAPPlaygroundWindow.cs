using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Content.Scripts.Editor.GOAPPlayground {
  public class GOAPPlaygroundWindow : EditorWindow {
    private const string PREFS_KEY = "GOAPPlayground_SelectedFeatures";
    
    private GOAPPlaygroundGraphView _graphView;
    private VisualElement _blackboardPanel;
    private VisualElement _toolbar;
    
    private string[] _availableFeatures;
    private List<string> _selectedFeatures = new();
    private Dictionary<string, Toggle> _featureToggles = new();

    [MenuItem("GOAP/Playground", false, 100)]
    public static void OpenWindow() {
      var window = GetWindow<GOAPPlaygroundWindow>();
      window.titleContent = new GUIContent("GOAP Playground", EditorGUIUtility.IconContent("d_SceneViewFx").image);
      window.minSize = new Vector2(800, 600);
    }

    private void OnEnable() {
      LoadFeatureList();
      LoadSavedFeatures();
      CreateUI();
      RefreshGraph();
      
      Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable() {
      Undo.undoRedoPerformed -= OnUndoRedo;
      
      if (_graphView != null) {
        rootVisualElement.Remove(_graphView);
        _graphView = null;
      }
    }

    private void OnUndoRedo() {
      RefreshGraph();
    }

    private void LoadFeatureList() {
      var features = Resources.LoadAll<AI.GOAP.GoapFeatureSO>("GOAP");
      _availableFeatures = features.Select(f => f.name).ToArray();
    }

    private void LoadSavedFeatures() {
      var saved = EditorPrefs.GetString(PREFS_KEY, "");
      if (!string.IsNullOrEmpty(saved)) {
        _selectedFeatures = saved.Split(',').Where(f => _availableFeatures.Contains(f)).ToList();
      } else {
        _selectedFeatures = new List<string>(_availableFeatures);
      }
    }

    private void SaveFeatures() {
      EditorPrefs.SetString(PREFS_KEY, string.Join(",", _selectedFeatures));
    }

    private void CreateUI() {
      rootVisualElement.Clear();

      var mainContainer = new VisualElement {
        style = {
          flexDirection = FlexDirection.Row,
          flexGrow = 1
        }
      };
      rootVisualElement.Add(mainContainer);

      CreateBlackboardPanel(mainContainer);

      var rightSide = new VisualElement {
        style = { flexGrow = 1, flexDirection = FlexDirection.Column }
      };
      mainContainer.Add(rightSide);

      CreateToolbar(rightSide);
      CreateGraphView(rightSide);
    }

    private void CreateBlackboardPanel(VisualElement parent) {
      _blackboardPanel = new VisualElement {
        style = {
          width = 220,
          backgroundColor = new Color(0.15f, 0.15f, 0.15f),
          borderRightWidth = 1,
          borderRightColor = new Color(0.1f, 0.1f, 0.1f),
          paddingTop = 5,
          paddingBottom = 5,
          paddingLeft = 5,
          paddingRight = 5
        }
      };
      parent.Add(_blackboardPanel);

      // Header
      var header = new Label("Blackboard (Beliefs)") {
        style = {
          fontSize = 14,
          unityFontStyleAndWeight = FontStyle.Bold,
          marginBottom = 10,
          color = new Color(0.8f, 0.8f, 0.8f)
        }
      };
      _blackboardPanel.Add(header);

      // Feature filter
      var filterFoldout = new Foldout { text = "Features", value = true };
      _blackboardPanel.Add(filterFoldout);

      _featureToggles.Clear();
      foreach (var feature in _availableFeatures) {
        var toggle = new Toggle(feature) { value = _selectedFeatures.Contains(feature) };
        toggle.RegisterValueChangedCallback(evt => OnFeatureToggled(feature, evt.newValue));
        filterFoldout.Add(toggle);
        _featureToggles[feature] = toggle;
      }

      // Select All / None buttons
      var buttonRow = new VisualElement {
        style = { flexDirection = FlexDirection.Row, marginTop = 5, marginBottom = 10 }
      };
      filterFoldout.Add(buttonRow);

      var selectAllBtn = new Button(() => SetAllFeatures(true)) { 
        text = "All", 
        style = { flexGrow = 1 } 
      };
      buttonRow.Add(selectAllBtn);

      var selectNoneBtn = new Button(() => SetAllFeatures(false)) { 
        text = "None", 
        style = { flexGrow = 1 } 
      };
      buttonRow.Add(selectNoneBtn);

      // Separator
      _blackboardPanel.Add(new VisualElement {
        style = { height = 1, backgroundColor = new Color(0.3f, 0.3f, 0.3f), marginTop = 5, marginBottom = 10 }
      });

      // Beliefs section
      var beliefsHeader = new Label("Belief Overrides") {
        style = { fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 }
      };
      _blackboardPanel.Add(beliefsHeader);

      var beliefsScroll = new ScrollView {
        style = { flexGrow = 1 }
      };
      beliefsScroll.name = "beliefs-scroll";
      _blackboardPanel.Add(beliefsScroll);

      // Simulate button
      var simulateBtn = new Button(OnSimulateClick) {
        text = "▶ Simulate Plan",
        style = {
          height = 30,
          marginTop = 10,
          backgroundColor = new Color(0.2f, 0.5f, 0.3f)
        }
      };
      _blackboardPanel.Add(simulateBtn);
    }

    private void SetAllFeatures(bool value) {
      Undo.RecordObject(this, value ? "Select All Features" : "Deselect All Features");
      
      _selectedFeatures.Clear();
      if (value) {
        _selectedFeatures.AddRange(_availableFeatures);
      }
      
      foreach (var kvp in _featureToggles) {
        kvp.Value.SetValueWithoutNotify(value);
      }
      
      SaveFeatures();
      RefreshGraph();
    }

    private void OnFeatureToggled(string feature, bool value) {
      Undo.RecordObject(this, $"Toggle Feature {feature}");
      
      if (value && !_selectedFeatures.Contains(feature)) {
        _selectedFeatures.Add(feature);
      } else if (!value && _selectedFeatures.Contains(feature)) {
        _selectedFeatures.Remove(feature);
      }
      
      SaveFeatures();
      RefreshGraph();
    }

    private void CreateToolbar(VisualElement parent) {
      _toolbar = new VisualElement {
        style = {
          flexDirection = FlexDirection.Row,
          height = 25,
          backgroundColor = new Color(0.2f, 0.2f, 0.2f),
          paddingLeft = 5,
          paddingRight = 5
        }
      };
      parent.Add(_toolbar);

      var refreshBtn = new Button(RefreshGraph) { text = "↻ Refresh" };
      _toolbar.Add(refreshBtn);

      var fitBtn = new Button(FitGraphToView) { text = "⊡ Fit View" };
      _toolbar.Add(fitBtn);

      var clearHighlightBtn = new Button(ClearHighlight) { text = "Clear Highlight" };
      _toolbar.Add(clearHighlightBtn);
    }

    private void CreateGraphView(VisualElement parent) {
      _graphView = new GOAPPlaygroundGraphView {
        style = { flexGrow = 1 }
      };
      parent.Add(_graphView);
    }

    private void RefreshGraph() {
      if (_graphView == null) return;
      
      var features = _selectedFeatures.Count > 0 ? _selectedFeatures.ToArray() : null;
      _graphView.LoadFromResources(features);
      
      PopulateBeliefsList();
    }

    private void PopulateBeliefsList() {
      var scrollView = _blackboardPanel.Q<ScrollView>("beliefs-scroll");
      if (scrollView == null) return;
      
      scrollView.Clear();

      var beliefs = _graphView.GetAllBeliefs();

      foreach (var belief in beliefs.OrderBy(b => b)) {
        var row = new VisualElement {
          style = { flexDirection = FlexDirection.Row, marginBottom = 2, alignItems = Align.Center }
        };

        var dropdown = new DropdownField(new List<string> { "—", "✓", "✗" }, 0) {
          style = { width = 40, height = 18 }
        };
        dropdown.name = $"belief-{belief}";
        row.Add(dropdown);

        var label = new Label(belief) {
          style = { 
            flexGrow = 1, 
            fontSize = 10,
            overflow = Overflow.Hidden,
            textOverflow = TextOverflow.Ellipsis,
            marginLeft = 4
          }
        };
        label.tooltip = belief;
        row.Add(label);

        scrollView.Add(row);
      }
    }

    private void OnSimulateClick() {
      Debug.Log("[GOAP Playground] Simulate clicked - not implemented yet");
    }

    private void FitGraphToView() {
      _graphView?.FrameAll();
    }

    private void ClearHighlight() {
      _graphView?.HighlightPlan(new List<string>());
    }
  }
}
