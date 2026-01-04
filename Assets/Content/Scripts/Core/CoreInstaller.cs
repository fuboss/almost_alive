using Content.Scripts.Core.Loop;
using Reflex.Core;
using UnityEngine;

namespace Content.Scripts.Core {
  public class CoreInstaller : MonoBehaviour, IInstaller {
    public void InstallBindings(ContainerBuilder containerBuilder) {
      var instance = new GameObject("CORE", typeof(CoreLoopModule)).GetComponent<CoreLoopModule>();
      containerBuilder.AddSingleton(instance);
    }
  }
}