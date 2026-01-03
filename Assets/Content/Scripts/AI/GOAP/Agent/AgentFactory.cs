using Content.Scripts.Core;
using Reflex.Extensions;
using Reflex.Injectors;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  public interface IAgentFactory : IPrefabFactory<GOAPAgent> {
  }

  public class AgentFactory : IAgentFactory {
    private readonly GOAPAgent _prefab;
    private readonly Transform _root;

    public AgentFactory(GOAPAgent prefab, Transform agentsRoot) {
      _prefab = prefab;
      _root = agentsRoot;
    }

    public GOAPAgent Spawn(Vector3 position) {
      var instance = Object.Instantiate(_prefab, position, Quaternion.identity);
      GameObjectInjector.InjectRecursive(instance.gameObject, instance.gameObject.scene.GetSceneContainer());
      //instance.transform.SetParent(_root, true);
      instance.OnCreated();
      return instance;
    }
  }
}