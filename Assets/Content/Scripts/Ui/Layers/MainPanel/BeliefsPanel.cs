using UnityEngine;
using UnityUtils;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class BeliefsPanel : BaseInfoPanel {
    [SerializeField] private BeliefPresenter _beliefPrefab;
    [SerializeField] private Transform _container;

    public override void Repaint() {
      base.Repaint();
      _container.DestroyChildren();
      if (agent == null) {
        return;
      }

      var beliefs = agent.agentBrain.beliefs;

      foreach (var belief in beliefs.Values) {
        if (!belief.lastEvaluation) continue;
        var presenter = Instantiate(_beliefPrefab, _container);
        presenter.Setup(belief);
      }
    }
  }
}