using Content.Scripts.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent {
  public interface IAgentFactory : IPrefabFactory<GOAPAgent> {
  }


  public class AgentFactory : IAgentFactory {
    [Inject] private IObjectResolver _objectResolver;
    [Inject] private GOAPAgent _prefab;
    
    public GOAPAgent Spawn(Vector3 position) {
      var instance = _objectResolver.Instantiate(_prefab, position, Quaternion.identity);
      instance.OnCreated();
      return instance;
    }
  }
}