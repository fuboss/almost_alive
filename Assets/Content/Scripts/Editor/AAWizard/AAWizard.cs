#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.AAWizard {
  public class AAWizard : OdinMenuEditorWindow {
    [MenuItem("AA/Wizards", priority = 0)]
    private static void OpenWindow() {
      var window = GetWindow<AAWizard>();
      window.titleContent = new GUIContent("AA Wizards");
      window.minSize = new Vector2(900, 700);
    }

    protected override OdinMenuTree BuildMenuTree() {
      var tree = new OdinMenuTree {
        Config = {
          DrawSearchToolbar = true,
          DefaultMenuStyle = OdinMenuStyle.TreeViewStyle
        }
      };

      // Add all wizards directly as composite objects
      tree.Add("Tag Manager", new TagWizardComposite());
      tree.Add("GOAP Feature", new GOAPFeatureWizardComposite());
      tree.Add("Actor Integration", new ActorIntegrationWizardComposite());
      tree.Add("Recipe Creator", new RecipeWizard());

      return tree;
    }
  }
}
#endif
