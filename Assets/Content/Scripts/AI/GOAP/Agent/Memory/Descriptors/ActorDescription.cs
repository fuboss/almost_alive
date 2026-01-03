using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Descriptors {
  public class ActorDescription : SerializedMonoBehaviour {
    [SerializeField] private DescriptionData _descriptionData;

    public DescriptionData descriptionData => _descriptionData;
  }
}