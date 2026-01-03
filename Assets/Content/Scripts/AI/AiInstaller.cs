using System.Collections.Generic;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Planning;
using Reflex.Core;
using Reflex.Extensions;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Content.Scripts.Core {
  public class AiInstaller : MonoBehaviour, IInstaller {
    public Transform agentsRoot;
    public GOAPAgent agentPrefab;

    public void InstallBindings(ContainerBuilder builder) {
      //Debug.Log("AI INSTALLER");

      builder.AddSingleton(new GoapPlanFactory());
      builder.AddSingleton(GoatFeatureBankModule.GetFromResources());
      builder.AddSingleton(new AgentFactory(agentPrefab, agentsRoot));
    }

    // // // TEST // // //
    private void Update() {
      var jump = InputSystem.actions.FindAction("Jump");
      if (!jump.WasReleasedThisFrame()) return;

      var agentFactory = gameObject.scene.GetSceneContainer().Resolve<AgentFactory>();
      if (agentFactory != null) {
        var position = transform.position + Random.onUnitSphere * 5;
        var instance = agentFactory.Spawn(position);

        var camGroup = agentsRoot.GetComponent<CinemachineTargetGroup>();
        camGroup.Targets ??= new List<CinemachineTargetGroup.Target>();
        camGroup.Targets.Add(new CinemachineTargetGroup.Target() {
          Object = instance.transform,
          Radius = 5,
          Weight = 1
        });
      }
      else {
        Debug.LogError("np factory resolved");
      }
    }
  }
}