using TMPro;
using UnityEngine;

namespace Content.Scripts.Ui.Layers.WorldSpaceUI {
  public class LabelWorldSpaceWidget : BaseWorldSpaceWidget {
    [SerializeField] private TMP_Text _label;

    public string LabelText {
      get => _label.text;
      set => _label.text = value;
    }
  }
}