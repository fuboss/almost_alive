using _Content.Scripts.AI.GOAP.Core;
using DependencyInjection;
using UnityEngine;
using UnityServiceLocator;

// https://github.com/adammyhre/Unity-Dependency-Injection-Lite

// https://github.com/adammyhre/Unity-Service-Locator

namespace _Content.Scripts.AI.GOAP {
  public class GoapFactory : MonoBehaviour, IDependencyProvider {
    private void Awake() {
      ServiceLocator.Global.Register(this);
    }

    [Provide]
    public GoapFactory ProvideFactory() {
      return this;
    }

    public IGoapPlanner CreatePlanner() {
      return new GoapPlanner();
    }
  }
}