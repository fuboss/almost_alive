using UnityEngine;

namespace Content.Scripts.Ui.Layers {
  public interface IInfoPanel {
    bool active { get; }
    string tab { get; }
    GameObject content { get; }
  }
  
  public abstract class BaseInfoPanel : MonoBehaviour, IInfoPanel {
    public string tabName;
    [SerializeField] protected GameObject _content;
    public bool active { get; protected set; }
    public string tab => tabName;
    public GameObject content => _content;
  }
}