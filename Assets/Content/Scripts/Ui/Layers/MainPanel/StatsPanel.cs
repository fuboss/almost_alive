using UnityEngine;
using UnityUtils;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class StatsPanel : BaseInfoPanel {
    [SerializeField] private StatPresenter _statPresenterPrefab;
    [SerializeField] private Transform _statsContainer;

    public override void Repaint() {
      base.Repaint();
      _statsContainer.DestroyChildren();
      if (agent == null) {
        return;
      }

      var body = agent.body;
      var stats = body.GetStatsInfo();

      foreach (var stat in stats) {
        var presenter = Instantiate(_statPresenterPrefab, _statsContainer);
        presenter.Setup(stat);
      }
    }
  }
}