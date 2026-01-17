using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class MainInfoPanel : UILayer {
    public HorizontalLayoutGroup tabsGroup;
    public TabButton tabPrefab;
    public float repaintInterval = 0.5f;
    //[Inject] private AgentUIModule _agentUIModule;

    public List<IInfoPanel> panels = new();
    private List<TabButton> _tabs = new();
    private IGoapAgent _agent;
    private float _lastRepaintTime;

    public override void Initialize() {
      base.Initialize();
      Init();
    }

    public override void Show() {
      base.Show();
    }

    private void Init() {
      InitTabs();
    }

    public override void Hide() {
      base.Hide();
    }

    public override void OnUpdate() {
      base.OnUpdate();
      if (Time.time > _lastRepaintTime + repaintInterval) {
        var activePanel = panels.First(p => p.active);
        activePanel?.Repaint();
      }
    }

    private void InitTabs() {
      foreach (var infoPanel in panels) {
        var tabBtnInstance = Instantiate(tabPrefab, tabsGroup.transform);
        tabBtnInstance.Setup(this, infoPanel);
        _tabs.Add(tabBtnInstance);
        infoPanel.Setup(this);
      }

      panels.First().active = true;
    }

    public void SelectTab(IInfoPanel selected) {
      foreach (var tabButton in _tabs) {
        var select = selected == tabButton.infoPanel;
        tabButton.isSelected = select;
      }
    }

    public void SetAgent(IGoapAgent selectedAgent) {
      _agent = selectedAgent;
      foreach (var panel in panels) {
        panel.SetAgent(_agent);
      }
    }
  }
}