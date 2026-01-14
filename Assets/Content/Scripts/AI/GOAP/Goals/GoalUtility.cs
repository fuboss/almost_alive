using System;
using Content.Scripts.AI.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Goals {
  [Serializable]
  [HideReferenceObjectPicker]
  [InlineEditor]
  public class GoalUtility {
    [OnValueChanged("ResetEvaluator")] [SerializeField]
    public UtilitySO utility;

    [HideLabel] [ShowIf("@utility != null")] [VerticalGroup("Eval")] [SerializeReference]
    public IUtilityEvaluator evaluator;

    [VerticalGroup("Eval")]
    [Button(SdfIconType.ArrowClockwise, Style = ButtonStyle.CompactBox, Name = "Reset", Stretch = false,
      Expanded = false)]
    [ShowIf("@utility != null")]
    private void ResetToDefault() {
      ResetEvaluator();
    }

    private void ResetEvaluator() {
      if (utility == null) {
        evaluator = null;
        return;
      }

      evaluator = utility.CopyEvaluator();
    }
  }
}