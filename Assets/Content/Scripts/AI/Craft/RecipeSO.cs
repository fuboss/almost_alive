using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Content.Scripts.AI.Craft {
  [CreateAssetMenu(fileName = "Recipe", menuName = "GOAP/Recipe", order = 0)]
  public class RecipeSO : SerializedScriptableObject {
    [InlineProperty, HideLabel] [OdinSerialize]
    private RecipeData data = new();

    /// <summary>Unique recipe ID (asset name).</summary>
    public string recipeId => name;

    [Title("Display")]
    [Tooltip("Category for UI grouping (e.g. 'Furniture', 'Production', 'Storage')")]
    public string category = "Other";
    
    [Tooltip("Display name in UI (if empty, uses recipeId)")]
    public string displayName;
    
    [PreviewField(50), HideLabel]
    public Sprite icon;

    [Title("Priority")]
    [Range(0, 100)] public int buildPriority = 0;
    
    public RecipeData recipe => data;

    /// <summary>Gets display name for UI.</summary>
    public string GetDisplayName() => string.IsNullOrEmpty(displayName) ? recipeId : displayName;

#if UNITY_EDITOR
    [Button("Validate", ButtonSizes.Small), PropertyOrder(-1), GUIColor(0.4f, 0.8f, 0.4f)]
    private void Validate() {
      var actorKeys = GOAP.GOAPEditorHelper.GetActorKeys();
      if (string.IsNullOrEmpty(data.resultActorKey)) {
        Debug.LogError($"[{name}] resultActorKey is empty!", this);
        return;
      }

      if (!actorKeys.Contains(data.resultActorKey)) {
        Debug.LogError($"[{name}] Actor '{data.resultActorKey}' not found in Addressables!", this);
      }
      else {
        Debug.Log($"[{name}] Valid ✓", this);
      }
    }
#endif

    private void OnValidate() {
      if (string.IsNullOrEmpty(data.resultActorKey)) {
        data.resultActorKey = name;
      }
    }
  }
}
