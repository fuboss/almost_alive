using Content.Scripts.AI.GOAP.Beliefs;
using TMPro;
using UnityEngine;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class BeliefPresenter : MonoBehaviour {
    public TMP_Text nameLabel;
    public void Setup(AgentBelief belief) {
      nameLabel.text = belief.GetPresenterString();
    }
  }
}