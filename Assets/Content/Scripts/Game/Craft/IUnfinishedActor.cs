using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.Game.Craft {
  public interface IUnfinishedActor : IProgressProvider {
    Transform transform { get; }
    ActorInventory inventory { get; }
    
    float workProgress { get; }
    float workRequired { get; }
    bool workComplete { get; }
    bool hasAllResources { get; }
    bool isReadyToComplete { get; }
    string name { get; }

    bool AddWork(float amount);
    int GetRemainingResourceCount(string tag);
    (string tag, int remaining)[] GetRemainingResources();
    bool CheckAllResourcesDelivered();
    ActorDescription TryComplete();
  }
}

