using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Interaction;
using Content.Scripts.Ui.Layers.Inspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  /// <summary>
  /// Inspector view for generic actors (trees, resources, items).
  /// Shows basic info + context actions.
  /// </summary>
  public class ActorInspectorView : MonoBehaviour, IInspectorView {
    [Header("Header")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _tagsText;

    [Header("Context Actions")]
    [SerializeField] private Transform _actionsContainer;
    [SerializeField] private ContextActionButton _actionButtonPrefab;

    private ISelectableActor _actor;
    private ActorDescription _actorDesc;

    public bool CanHandle(ISelectableActor actor) {
      // Handle anything that's NOT a GOAPAgent (agents go to AgentInspectorView)
      return actor != null && actor is not IGoapAgent;
    }

    public void Show(ISelectableActor actor) {
      _actor = actor;
      _actorDesc = actor.gameObject.GetComponent<ActorDescription>();
      gameObject.SetActive(true);
      Repaint();
      RefreshContextActions();
    }

    public void Hide() {
      gameObject.SetActive(false);
      _actor = null;
      _actorDesc = null;
    }

    public void OnUpdate() {
      // Could refresh health bars etc here
    }

    private void Repaint() {
      if (_actor == null) return;

      // Name
      _nameText.text = _actorDesc != null 
        ? _actorDesc.actorKey 
        : _actor.gameObject.name;

      // Description
      if (_descriptionText != null && _actorDesc != null) {
        _descriptionText.text = "#TODO#";
      }

      // Tags
      if (_tagsText != null && _actorDesc != null) {
        var tags = _actorDesc.descriptionData.tags;
        _tagsText.text = tags != null && tags.Length > 0 
          ? string.Join(", ", tags) 
          : "No tags";
      }
    }

    private void RefreshContextActions() {
      if (_actionsContainer == null || _actionButtonPrefab == null) return;

      // Clear existing
      foreach (Transform child in _actionsContainer) {
        Destroy(child.gameObject);
      }

      if (_actor == null) return;

      // Get available actions for this actor
      var actions = ContextActionRegistry.GetActionsFor(_actor);
      foreach (var action in actions) {
        var btn = Instantiate(_actionButtonPrefab, _actionsContainer);
        btn.Setup(action, _actor);
      }
    }
  }
}
