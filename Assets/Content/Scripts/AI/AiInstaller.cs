using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Planning;
using Reflex.Core;
using Reflex.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Content.Scripts.Core {
  public class AiInstaller : MonoBehaviour, IInstaller {
    public GOAPAgent agentPrefab;

    public void InstallBindings(ContainerBuilder builder) {
      //Debug.Log("AI INSTALLER");

      builder.AddSingleton(new GoapPlanFactory());
      builder.AddSingleton(GoatFeatureBankModule.GetFromResources());
      builder.AddSingleton(new AgentFactory(agentPrefab));
    }

    // // // TEST // // //
    private void Update() {
      var jump = InputSystem.actions.FindAction("Jump");
      if (!jump.WasReleasedThisFrame()) return;

      var agentFactory = gameObject.scene.GetSceneContainer().Resolve<AgentFactory>();
      if (agentFactory != null) {
        var position = transform.position + Random.onUnitSphere * 5;
        agentFactory.Spawn(position);
      }
      else {
        Debug.LogError("np factory resolved");
      }
    }
  }
}