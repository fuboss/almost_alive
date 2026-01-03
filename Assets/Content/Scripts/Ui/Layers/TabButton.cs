using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers {
  public class TabButton : MonoBehaviour {
    public Image iconImage;
    public Button button;
    public TMP_Text titleText;


    public void Setup(MainInfoPanel mainInfoPanel, InfoPanel infoPanel) {
      titleText.text = infoPanel.tabName;
    }
  }
}