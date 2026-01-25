using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using VContainer.Unity;

namespace Content.Scripts.AI.Navigation {
  public class NavigationModule : IInitializable, ITickable, IDisposable {
    private const float DefaultDebounceDelay = 0.4f;

    private NavMeshSurface _terrainSurface;
    private readonly List<NavMeshSurface> _structureSurfaces = new();
    private CancellationTokenSource _debounceCts;
    private CancellationTokenSource _bakeCts;
    
    private bool _isBaking;
    private bool _pendingRequest;
    private float _debounceDelay = DefaultDebounceDelay;

    public bool IsBaking => _isBaking;
    public float BakeProgress { get; private set; }
    
    public event Action OnBakeStarted;
    public event Action OnBakeCompleted;

    void IInitializable.Initialize() {
      var terrain = Terrain.activeTerrain;
      if (terrain != null) {
        _terrainSurface = terrain.GetComponentInChildren<NavMeshSurface>();
      }

      if (_terrainSurface == null) {
        Debug.LogWarning("[NavigationModule] NavMeshSurface not found on terrain");
      }
    }

    public void RegisterSurface(NavMeshSurface surface) {
      if (surface == null || _structureSurfaces.Contains(surface)) return;
      _structureSurfaces.Add(surface);
      
      if (surface.navMeshData == null) {
        surface.BuildNavMesh();
      }
    }

    public void UnregisterSurface(NavMeshSurface surface) {
      _structureSurfaces.Remove(surface);
    }

    void ITickable.Tick() {
      // Tick не используется для async логики, но оставлен для возможного расширения
    }

    void IDisposable.Dispose() {
      _debounceCts?.Cancel();
      _debounceCts?.Dispose();
      _bakeCts?.Cancel();
      _bakeCts?.Dispose();
    }

    public void SetDebounceDelay(float delay) {
      _debounceDelay = delay;
    }

    public void RequestBake() {
      if (_terrainSurface == null && _structureSurfaces.Count == 0) return;

      _pendingRequest = true;
      _debounceCts?.Cancel();
      _debounceCts = new CancellationTokenSource();
      
      DebouncedBakeAsync(_debounceCts.Token).Forget();
    }

    public void RequestBakeImmediate() {
      if (_terrainSurface == null && _structureSurfaces.Count == 0) return;

      _debounceCts?.Cancel();
      _pendingRequest = false;
      
      StartBakeAsync().Forget();
    }

    private async UniTaskVoid DebouncedBakeAsync(CancellationToken ct) {
      try {
        await UniTask.Delay(TimeSpan.FromSeconds(_debounceDelay), cancellationToken: ct);
        
        if (ct.IsCancellationRequested) return;
        
        _pendingRequest = false;
        await StartBakeAsync();
      }
      catch (OperationCanceledException) {
        // Debounce был отменён новым запросом — ожидаемое поведение
      }
    }

    private async UniTask StartBakeAsync() {
      if (_isBaking) {
        _pendingRequest = true;
        return;
      }

      _isBaking = true;
      BakeProgress = 0f;
      OnBakeStarted?.Invoke();

      _bakeCts?.Cancel();
      _bakeCts = new CancellationTokenSource();

      try {
        await BakeSurfaceAsync(_bakeCts.Token);
      }
      catch (OperationCanceledException) {
        Debug.Log("[NavigationModule] Bake cancelled");
      }
      finally {
        _isBaking = false;
        BakeProgress = 1f;
        OnBakeCompleted?.Invoke();

        if (_pendingRequest) {
          _pendingRequest = false;
          await StartBakeAsync();
        }
      }
    }

    private async UniTask BakeSurfaceAsync(CancellationToken ct) {
      var surfaces = new List<NavMeshSurface>();
      
      if (_terrainSurface != null) {
        surfaces.Add(_terrainSurface);
      }
      surfaces.AddRange(_structureSurfaces);

      var totalSurfaces = surfaces.Count;
      for (var i = 0; i < totalSurfaces; i++) {
        var surface = surfaces[i];
        if (surface == null || !surface.isActiveAndEnabled) continue;

        var asyncOp = surface.UpdateNavMesh(surface.navMeshData);

        while (!asyncOp.isDone) {
          ct.ThrowIfCancellationRequested();
          BakeProgress = (i + asyncOp.progress) / totalSurfaces;
          await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
      }

      Debug.Log($"[NavigationModule] NavMesh bake completed ({totalSurfaces} surfaces)");
    }

    public void CancelBake() {
      _bakeCts?.Cancel();
      _debounceCts?.Cancel();
      _pendingRequest = false;
    }
  }
}

