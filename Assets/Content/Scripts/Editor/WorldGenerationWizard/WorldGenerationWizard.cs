#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard {
  /// <summary>
  /// Central wizard for world generation configuration.
  /// Provides unified access to biomes, scatters, vegetation, and generation settings.
  /// </summary>
  public class WorldGenerationWizard : OdinMenuEditorWindow {
    private const string MENU_PATH = "AA/World Generation Wizard";

    [MenuItem(MENU_PATH, priority = 1)]
    private static void OpenWindow() {
      var window = GetWindow<WorldGenerationWizard>();
      window.titleContent = new GUIContent("World Generation", EditorGUIUtility.IconContent("Terrain Icon").image);
      window.minSize = new Vector2(950, 750);
    }

    protected override OdinMenuTree BuildMenuTree() {
      var tree = new OdinMenuTree {
        Config = {
          DrawSearchToolbar = true,
          DefaultMenuStyle = OdinMenuStyle.TreeViewStyle
        }
      };

      tree.Add("Config", new GenerationConfigComposite());
      tree.Add("Database/Biomes", new BiomeDatabaseComposite());
      tree.Add("Database/Scatters", new ScatterDatabaseComposite());
      tree.Add("Database/Vegetation", new VegetationDatabaseComposite());

      return tree;
    }
  }
}
#endif
