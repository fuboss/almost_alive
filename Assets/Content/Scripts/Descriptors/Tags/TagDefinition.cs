using Sirenix.OdinInspector;

namespace Content.Scripts.Game {
  public abstract class TagDefinition : SerializedMonoBehaviour {
    public abstract string Tag { get; }
  }
}