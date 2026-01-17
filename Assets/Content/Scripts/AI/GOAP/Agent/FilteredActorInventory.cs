using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Agent {
  public class FilteredActorInventory : ActorInventory {
    [ValueDropdown("GetTags")] public string[] availableTags;

    public override bool TryPutItemInInventory(ActorDescription target) {
      var filtered = availableTags.Length == 0 || availableTags.Any(t => target.descriptionData.tags.Contains(t));

      return filtered && base.TryPutItemInInventory(target);
    }

#if UNITY_EDITOR
    public List<string> GetTags() {
      return GOAPEditorHelper.GetTags();
    }
#endif
  }
}