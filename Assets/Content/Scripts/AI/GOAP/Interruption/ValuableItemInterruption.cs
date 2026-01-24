using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Goals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Interruption {
//   /// <summary>
//   /// Interrupts when agent spots a valuable/rare item nearby.
//   /// </summary>
//   [Serializable]
//   public class ValuableItemInterruption : InterruptionSourceBase {
//     [Tooltip("Tag that marks valuable items")]
//     [ValueDropdown("GetTags")]
//     [SerializeField] private string _valuableTag = Tag.VALUABLE;
//     
//     [Tooltip("Max distance to check for items")]
//     [SerializeField] private float _detectionRadius = 15f;
//     
//     [Tooltip("Goal to pursue when valuable found")]
//     [SerializeField] private string _collectGoalName = "CollectItem";
//
//     private AgentGoal _cachedGoal;
//
// #if UNITY_EDITOR
//     private System.Collections.Generic.List<string> GetTags() => GOAPEditorHelper.GetTags();
// #endif
//
//     protected override bool CheckInterruptCondition(IGoapAgent agent, out AgentGoal interruptGoal) {
//       interruptGoal = null;
//       
//       // Check memory for valuable items
//       var valuables = agent.memory.GetWithAllTags(new[] { _valuableTag });
//       if (valuables.Length == 0) return false;
//
//       // Find one within range
//       var nearby = valuables.FirstOrDefault(m => 
//         m.target != null && 
//         Vector3.Distance(agent.position, m.location) <= _detectionRadius);
//       
//       if (nearby == null) return false;
//
//       // Find goal
//       interruptGoal = FindGoal(agent);
//       if (interruptGoal == null) {
//         Debug.LogWarning($"[ValuableItemInterruption] Goal '{_collectGoalName}' not found");
//         return false;
//       }
//
//       Debug.Log($"[ValuableItemInterruption] Spotted valuable: {nearby.target.name}");
//       return true;
//     }
//
//     protected override bool IsAlreadyPursuingGoal(IGoapAgent agent, AgentGoal currentGoal) {
//       if (currentGoal == null) return false;
//       
//       // Don't interrupt if already collecting
//       return currentGoal.Name == _collectGoalName || 
//              currentGoal.Name.Contains("Collect") ||
//              currentGoal.Name.Contains("Pickup");
//     }
//
//     private AgentGoal FindGoal(IGoapAgent agent) {
//       if (_cachedGoal != null && _cachedGoal.Name == _collectGoalName) {
//         return _cachedGoal;
//       }
//
//       var template = agent.agentBrain.goalTemplates?.FirstOrDefault(gt => gt.name == _collectGoalName);
//       if (template == null) return null;
//
//       _cachedGoal = template.Get(agent);
//       return _cachedGoal;
//     }
//   }
}
