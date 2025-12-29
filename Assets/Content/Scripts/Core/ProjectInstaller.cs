using Reflex.Core;
using UnityEngine;

namespace Content.Scripts.Core {
  public class ProjectInstaller : MonoBehaviour, IInstaller {
    public void InstallBindings(ContainerBuilder builder) {
      builder.AddSingleton("Hello");
    }
  }
}