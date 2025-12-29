using Content.Scripts.AI.GOAP;
using Reflex.Core;
using UnityEngine;

namespace Content.Scripts.Core {
  public class AiInstaller : MonoBehaviour, IInstaller {
    public void InstallBindings(ContainerBuilder builder) {
      Debug.LogError("AI INSTALLER");
      builder.AddSingleton(_ => new GoapFactory());
    }
  }
}