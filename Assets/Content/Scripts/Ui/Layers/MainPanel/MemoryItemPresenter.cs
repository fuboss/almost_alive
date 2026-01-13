using Content.Scripts.AI.GOAP.Agent.Memory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class MemoryItemPresenter : MonoBehaviour {
    public TMP_Text tagsText;
    public TMP_Text locationText;
    public TMP_Text confidenceText;
    public Image confidenceBar;

    public void Setup(MemorySnapshot memory) {
      tagsText.text = string.Join(", ", memory.tags ?? new System.Collections.Generic.List<string>());
      locationText.text = $"({memory.location.x:F1}, {memory.location.y:F1}, {memory.location.z:F1})";
      confidenceText.text = $"{memory.confidence:P0}";
      
      confidenceBar.fillAmount = memory.confidence;
      confidenceBar.color = Color.Lerp(Color.red, Color.green, memory.confidence);
    }
  }
}