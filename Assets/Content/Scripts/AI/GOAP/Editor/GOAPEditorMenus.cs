#if UNITY_EDITOR
using UnityEditor;

namespace Content.Scripts.AI.GOAP.Editor {
  public static class GOAPEditorMenus {
    [MenuItem("GOAP/Invalidate Editor Cache")]
    public static void InvalidateCache() {
      GOAPEditorHelper.InvalidateCache();
      UnityEngine.Debug.Log("[GOAP] Editor cache invalidated");
    }
  }
}
#endif
