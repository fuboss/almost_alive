using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent.Camera {
  public class CameraModule : IInitializable, ILateTickable, IStartable {
    [Inject] private CinemachineCamera _camera;
    [Inject] private CinemachineTargetGroup _agentsRoot;

    void IInitializable.Initialize() {
    }

    void ILateTickable.LateTick() {
      UpdateZoom();
    }

    private static void UpdateZoom() {
      var value = Mouse.current.scroll.value;
    }

    void IStartable.Start() {
      _camera.Target = new CameraTarget() {
        CustomLookAtTarget = false,
        LookAtTarget = _agentsRoot.transform,
        TrackingTarget = _agentsRoot.transform
      };
      _camera.UpdateTargetCache();
    }
    
    public void AddToCameraGroup(IGoapAgent agent) {
      var camGroup = _agentsRoot.GetComponent<CinemachineTargetGroup>();
      camGroup.Targets ??= new List<CinemachineTargetGroup.Target>();
      camGroup.Targets.Add(new CinemachineTargetGroup.Target() {
        Object = agent.transform,
        Radius = 5,
        Weight = 1
      });
    }
  }
}