using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers {
  public class MainInfoPanel : UILayer {
    public HorizontalLayoutGroup tabsGroup;
    public TabButton tabPrefab;

    public List<IInfoPanel> panels = new List<IInfoPanel>();
    private List<TabButton> _tabs = new();

    private void Awake() {
      Init();
    }

    public override void Show() {
      base.Show();
    }

    private void Init() {
      foreach (var infoPanel in panels) {
        var tabBtnInstance = Instantiate(tabPrefab, tabsGroup.transform);
        tabBtnInstance.Setup(this, infoPanel);
        _tabs.Add(tabBtnInstance);
      }
    }

    public void Select(IInfoPanel selected) {
      foreach (var tabButton in _tabs) {
        var select = selected == tabButton.infoPanel;
        tabButton.isSelected = select;
      }
    }
  }
}