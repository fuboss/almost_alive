using Content.Scripts.AI.GOAP.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class StatPresenter : MonoBehaviour {
    public TMP_Text statName;
    public Slider statSlider;
    public TMP_Text statValue;

    public void Setup(AgentStat stat) {
      statName.text = stat.type.ToString();
      statSlider.value = stat.Normalized;
      statSlider.minValue = 0;
      statSlider.maxValue = 1;

      var value = stat is FloatAgentStat floatStat ? floatStat.value : stat.Normalized;
      statValue.text = $"{value:f1}";
    }
  }
}