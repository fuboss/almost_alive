using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine.UI;
using VContainer;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class MainInfoPanel : UILayer {
    public HorizontalLayoutGroup tabsGroup;
    public TabButton tabPrefab;

    //[Inject] private AgentUIModule _agentUIModule;

    public List<IInfoPanel> panels = new();
    private List<TabButton> _tabs = new();
    private IGoapAgent _agent;

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

    private void InitTabs() {
      foreach (var infoPanel in panels) {
        var tabBtnInstance = Instantiate(tabPrefab, tabsGroup.transform);
        tabBtnInstance.Setup(this, infoPanel);
        _tabs.Add(tabBtnInstance);
        infoPanel.active = false;
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