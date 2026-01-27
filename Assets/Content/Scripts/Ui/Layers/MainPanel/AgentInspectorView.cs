using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Interaction;
using Content.Scripts.Ui.Layers.Inspector;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  /// <summary>
  /// Inspector view for GOAP agents. Shows tabs: Plan, Needs, Beliefs.
  /// Implements IInspectorView to integrate with InspectorLayer.
  /// </summary>
  public class AgentInspectorView : SerializedMonoBehaviour, IInspectorView {
    [Header("Header")]
    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _classText;
    [SerializeField] private TMP_Text _moodText;

    [Header("Tabs")]
    [SerializeField] private HorizontalLayoutGroup _tabsGroup;
    [SerializeField] private TabButton _tabPrefab;
    [SerializeField] private List<MonoBehaviour> _panelComponents = new();

    [Header("Settings")]
    [SerializeField] private float _repaintInterval = 0.5f;

    private readonly List<IInfoPanel> _panels = new();
    private readonly List<TabButton> _tabs = new();
    private IGoapAgent _agent;
    private float _lastRepaintTime;
    private bool _initialized;

    private void Awake() {
      InitTabs();
    }

    private void InitTabs() {
      if (_initialized) return;
      _initialized = true;

      foreach (var component in _panelComponents) {
        if (component is IInfoPanel panel)
          _panels.Add(panel);
      }

      foreach (var panel in _panels) {
        var tabBtn = Instantiate(_tabPrefab, _tabsGroup.transform);
        tabBtn.Setup(this, panel);
        _tabs.Add(tabBtn);
        panel.Setup(null); // Will be set properly on Show
      }

      if (_panels.Count > 0)
        _panels[0].active = true;
    }

    public bool CanHandle(ISelectableActor actor) {
      return actor is IGoapAgent;
    }

    public void Show(ISelectableActor actor) {
      _agent = actor as IGoapAgent;
      gameObject.SetActive(true);
      
      foreach (var panel in _panels) {
        panel.SetAgent(_agent);
      }

      RepaintHeader();
    }

    public void Hide() {
      gameObject.SetActive(false);
      _agent = null;
    }

    public void OnUpdate() {
      if (Time.time < _lastRepaintTime + _repaintInterval) return;
      _lastRepaintTime = Time.time;

      RepaintHeader();
      var activePanel = _panels.FirstOrDefault(p => p.active);
      activePanel?.Repaint();
    }

    private void RepaintHeader() {
      if (_agent == null) return;

      _nameText.text = _agent.gameObject.name;
      
      // Class/level from experience
      if (_classText != null) {
        var exp = _agent.experience;
        _classText.text = $"Level {exp?.level ?? 1}";
      }

      // Mood placeholder
      if (_moodText != null) {
        _moodText.text = "😊 Content"; // TODO: actual mood system
      }
    }

    public void SelectTab(IInfoPanel selected) {
      foreach (var tab in _tabs) {
        tab.isSelected = tab.infoPanel == selected;
      }
    }
  }
}
