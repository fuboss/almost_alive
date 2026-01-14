using System.Collections.Generic;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  public interface IUtilityEvaluator {
    float Evaluate(IGoapAgent agent);
  }

  public abstract class UtilitySO : SerializedScriptableObject, IUtilityEvaluator {
    public abstract float Evaluate(IGoapAgent agent);
#if UNITY_EDITOR
    public List<string> Tags() {
      return GOAPEditorHelper.GetTags();
    }
#endif
    public abstract IUtilityEvaluator CopyEvaluator();
  }
  
  public abstract class UtilitySO<TData> : UtilitySO where TData : IUtilityEvaluator, new() {
    [SerializeReference] protected TData evaluator = new TData();
    public override float Evaluate(IGoapAgent agent) {
      return evaluator.Evaluate(agent);
    }

    public override IUtilityEvaluator CopyEvaluator() {
      return evaluator.CloneViaSerialization();
    }
  }
}