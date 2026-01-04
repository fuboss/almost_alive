using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public interface IInfoPanel {
    bool active { get; set; }
    string tab { get; }
    GameObject content { get; }
    void SetAgent(IGoapAgent agent);
    void Setup(MainInfoPanel mainInfoPanel);
    void Repaint();
  }

  public abstract class BaseInfoPanel : MonoBehaviour, IInfoPanel {
    public string tabName;
    [SerializeField] protected GameObject _content;
    protected IGoapAgent agent;
    private bool _active;
    protected MainInfoPanel parent;

    public bool active {
      get => _active;
      set {
        if (value == _active) return;
        _active = value;
        _content.SetActive(value);
        Repaint();
      }
    }

    public string tab => tabName;
    public GameObject content => _content;

    public void SetAgent(IGoapAgent newAgent) {
      agent = newAgent;
      Repaint();
    }

    public void Setup(MainInfoPanel mainInfoPanel) {
      parent = mainInfoPanel;
    }

    public virtual void Repaint() {
      if (!active) return;
    }
  }
}