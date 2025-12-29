using Reflex.Core;
using UnityEngine;

namespace Content.Scripts.Core {
  public class DebugSceneInstaller : MonoBehaviour, IInstaller {
    public void InstallBindings(ContainerBuilder builder) {
      Debug.Log("DEBUG INSTALLER");
    }
  }
}