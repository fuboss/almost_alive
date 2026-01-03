using System.Collections.Generic;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers {
  public class MainInfoPanel : UILayer {
    public HorizontalLayoutGroup tabsGroup;
    public TabButton tabPrefab;

    public List<InfoPanel> panels = new List<InfoPanel>();

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
      }
    }
  }
}