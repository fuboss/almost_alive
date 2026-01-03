using Reflex.Core;
using UnityEngine;

namespace Content.Scripts.Ui {
  public class UiInstaller : MonoBehaviour, IInstaller {
    public Transform uiRoot;
    public UILayer[] uiLayers;

    public void InstallBindings(ContainerBuilder builder) {
      if (uiRoot == null) {
        uiRoot = new GameObject("UI Root").transform;
        DontDestroyOnLoad(uiRoot.gameObject);
      }

      foreach (var uiLayer in uiLayers) {
        var instance = Instantiate(uiLayer, uiRoot);
        builder.AddSingleton(instance);
        instance.SetVisible(false);
      }
    }
  }
}