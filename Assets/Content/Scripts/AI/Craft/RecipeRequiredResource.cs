using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.Craft {
  [Serializable]
  public class RecipeRequiredResource {
    [ValueDropdown("Tags")]
    public string tag;

    public ushort count = 1;
#if UNITY_EDITOR
    private IEnumerable<string> Tags() => Tag.ALL_TAGS;
#endif
  }
}