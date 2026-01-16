using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP {
  public class ActorPartsDescription : SerializedMonoBehaviour {
    [DictionaryDrawerSettings(KeyLabel = "ActorID", ValueLabel = "Count")]
    public Dictionary<string, ushort> parts = new();
  }
}