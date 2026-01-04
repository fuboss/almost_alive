using UnityEngine;
using UnityUtils;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class StatsPanel : BaseInfoPanel {
    [SerializeField] private StatPresenter _statPresenterPrefab;
    [SerializeField] private Transform _statsContainer;
    [SerializeField] private BeliefPresenter _beliefPrefab;
    [SerializeField] private Transform _container;

    public override void Repaint() {
      base.Repaint();
      _statsContainer.DestroyChildren();
      _container.DestroyChildren();
      if (agent == null) {
        return;
      }
      
      var stats = agent.body.GetStatsInfo();
      foreach (var stat in stats) {
        var presenter = Instantiate(_statPresenterPrefab, _statsContainer);
        presenter.Setup(stat);
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