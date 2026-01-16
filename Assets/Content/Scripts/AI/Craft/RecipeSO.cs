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

    public bool isCampRecipe;
    [Range(0, 100)] public int campBuildPriority = 0;
    public RecipeData recipe => data;

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