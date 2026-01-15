using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  public abstract class UtilitySO : SerializedScriptableObject, IUtilityEvaluatorProvider {
    public abstract IUtilityEvaluator CopyEvaluator();
  }

  public abstract class UtilitySO<TEval> : UtilitySO
    where TEval : IUtilityEvaluator, new() {
    [SerializeReference] public TEval evaluator = new TEval();

    public override IUtilityEvaluator CopyEvaluator() {
      return evaluator.CloneViaSerialization();
    }
  }

  public interface IUtilityCompositeEvaluatorProvider {
    IEnumerable<IUtilityEvaluatorProvider> Get();
  }

  public abstract class CompositeUtilitySO : SerializedScriptableObject {
  }

  public abstract class CompositeUtilitySO<TEval> : CompositeUtilitySO, IUtilityCompositeEvaluatorProvider
    where TEval : IUtilityEvaluator, new() {
    [SerializeReference] protected List<UtilitySO<TEval>> evaluators = new();

    public IEnumerable<IUtilityEvaluatorProvider> Get() {
      return evaluators;
    }

    private bool GetPath(out string path) {
      path = UnityEditor.AssetDatabase.GetAssetPath(this);
      if (string.IsNullOrEmpty(path)) {
        var savePath = UnityEditor.EditorUtility.SaveFilePanelInProject(
          "Save Utility Asset",
          name + ".asset",
          "asset",
          "Asset must be saved to add sub-assets"
        );
        if (string.IsNullOrEmpty(savePath)) return false;
        UnityEditor.AssetDatabase.CreateAsset(this, savePath);
        path = savePath;
      }

      return true;
    }

    [Button]
    public void CreateDefault() {
#if !UNITY_EDITOR
      return;
#endif
      if (!GetPath(out var path)) return;
      CreateDefaultSOs(path);
    }

    protected abstract void CreateDefaultSOs(string path);
  }

  public abstract class CompositeUtilityByStatsSO<TEval, TSO> : CompositeUtilitySO<TEval>
    where TEval : IUtilityEvaluator, new()
    where TSO : UtilitySO<TEval> {
    public StatType[] desiredTags = Array.Empty<StatType>();

    protected override void CreateDefaultSOs(string path) {
      var anyAdded = false;
      foreach (var stat in desiredTags) {
        var existing = FindExisting(stat);
        if (existing != null) continue;

        var instance = CreateInstance<TSO>();
        instance.name = $"{name}/{stat}";
        Init(instance.evaluator, stat);
        evaluators.Add(instance);

        UnityEditor.AssetDatabase.AddObjectToAsset(instance, path);
        UnityEditor.EditorUtility.SetDirty(instance);
        anyAdded = true;
      }

      if (anyAdded) {
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
      }
    }

    private void OnValidate() {
      evaluators.RemoveAll(e => e == null);
    }

    protected abstract TSO FindExisting(StatType statType);

    protected abstract void Init(TEval ev, StatType tag);
  }

  public abstract class CompositeUtilityByTagsSO<TEval, TSO> : CompositeUtilitySO<TEval>
    where TEval : IUtilityEvaluator, new()
    where TSO : UtilitySO<TEval> {
    public string[] desiredTags = Tag.ALL_TAGS;

    protected override void CreateDefaultSOs(string path) {
      var anyAdded = false;
      foreach (var tag in desiredTags) {
        var existing = FindExisting(tag);
        if (existing != null) continue;

        var instance = CreateInstance<TSO>();
        instance.name = $"{name}/{tag}";
        Init(instance.evaluator, tag);
        evaluators.Add(instance);

        UnityEditor.AssetDatabase.AddObjectToAsset(instance, path);
        UnityEditor.EditorUtility.SetDirty(instance);
        anyAdded = true;
      }

      if (anyAdded) {
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
      }
    }

    private void OnValidate() {
      evaluators.RemoveAll(e => e == null);
    }

    protected abstract TSO FindExisting(string tag);

    protected abstract void Init(TEval ev, string tag);
  }
}