using System.Collections;
using Cysharp.Threading.Tasks;

namespace Content.Scripts.Core.Loop {
  public interface ILifeCycle {
  }

  public interface IInitializableAsync : ILifeCycle {
    UniTaskVoid Initialize();
  }

  public interface IInitializable : ILifeCycle {
    IEnumerator Initialize();
  }

  public interface IUnloadable : ILifeCycle {
    IEnumerator Initialize();
  }

  public interface IUpdatable : ILifeCycle {
    void OnUpdate();
  }

  public interface IFixedUpdatable : ILifeCycle {
    void OnFixedUpdate();
  }
}