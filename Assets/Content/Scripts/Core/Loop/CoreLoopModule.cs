using System.Collections;
using System.Collections.Generic;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;

namespace Content.Scripts.Core.Loop {
  public class CoreLoopModule : MonoBehaviour {
    [Inject] private IEnumerable<IInitializable> _initializables;
    [Inject] private IEnumerable<IUpdatable> _updatables;
    [Inject] private IEnumerable<IFixedUpdatable> _fixedUpdatables;
    private bool _inited;

    private void Start() {
      _inited = false;

      StartCoroutine(Init());
    }

    private IEnumerator Init() {
      _inited = false;
      if (_initializables == null) {
        _inited = true;
        yield break;
      }


      foreach (var initializable in _initializables) {
        yield return initializable.Initialize();
      }

      _inited = true;
      DontDestroyOnLoad(gameObject);
    }

    private void Update() {
      if (_updatables == null) return;
      if (!_inited) return;
      foreach (var updatable in _updatables) {
        updatable.OnUpdate();
      }
    }

    private void FixedUpdate() {
      if (_fixedUpdatables == null) return;
      if (!_inited) return;
      foreach (var fixedUpdatable in _fixedUpdatables) {
        fixedUpdatable.OnFixedUpdate();
      }
    }

    public IEnumerator Initialize() {
      yield break;
    }
  }
}