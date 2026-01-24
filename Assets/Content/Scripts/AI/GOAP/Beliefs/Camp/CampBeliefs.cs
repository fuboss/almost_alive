// using System;
// using System.Linq;
// using Content.Scripts.AI.GOAP.Agent;
// using Sirenix.OdinInspector;
//
// namespace Content.Scripts.AI.GOAP.Beliefs.Camp {
//   [Serializable, TypeInfoBox("True when agent has claimed camp with setup.")]
//   public class HasCampBelief : AgentBelief {
//     protected override Func<bool> GetCondition(IGoapAgentCore agent) {
//       if (agent is not ICampAgent campAgent) return () => false;
//       
//       return () => {
//         var camp = campAgent.camp;
//         return camp != null && camp.hasSetup;
//       };
//     }
//
//     public override AgentBelief Copy() => new HasCampBelief { name = name };
//   }
//
//   [Serializable, TypeInfoBox("True when agent needs a camp (has no camp or no setup).")]
//   public class NeedsCampBelief : AgentBelief {
//     protected override Func<bool> GetCondition(IGoapAgentCore agent) {
//       if (agent is not ICampAgent campAgent) return () => true; // No camp capability = needs camp? Debatable
//       
//       return () => {
//         var camp = campAgent.camp;
//         return camp == null || !camp.hasSetup;
//       };
//     }
//
//     public override AgentBelief Copy() => new NeedsCampBelief { name = name };
//   }
//
//   [Serializable, TypeInfoBox("True when camp has empty spots and agent can build something (has resources).")]
//   public class CampNeedsBuildingBelief : AgentBelief {
//     protected override Func<bool> GetCondition(IGoapAgentCore agent) {
//       if (agent is not ICampAgent campAgent) return () => false;
//       if (agent is not IWorkAgent workAgent) return () => false;
//       if (agent is not IInventoryAgent invAgent) return () => false;
//       
//       return () => {
//         var recipeModule = workAgent.recipeModule;
//         var camp = campAgent.camp;
//         if (camp?.setup == null) return false;
//         if (camp.setup.allSpotsFilled) return false;
//
//         if (recipeModule == null) return false;
//         var campRecipes = workAgent.recipes.GetUnlockedCampRecipes(recipeModule);
//         return campRecipes.Any(r => recipeModule.CanCraft(r, invAgent.inventory));
//       };
//     }
//
//     public override AgentBelief Copy() => new CampNeedsBuildingBelief { name = name };
//   }
//
//   [Serializable, TypeInfoBox("True when camp is fully built (all spots filled).")]
//   public class CampFullyBuiltBelief : AgentBelief {
//     protected override Func<bool> GetCondition(IGoapAgentCore agent) {
//       if (agent is not ICampAgent campAgent) return () => false;
//       
//       return () => {
//         var camp = campAgent.camp;
//         return camp?.setup != null && camp.setup.allSpotsFilled;
//       };
//     }
//
//     public override AgentBelief Copy() => new CampFullyBuiltBelief { name = name };
//   }
//
//   [Serializable, TypeInfoBox("True when camp has a module with specified tag.")]
//   public class CampHasModuleBelief : AgentBelief {
//     [ValueDropdown("GetTags")] public string moduleTag;
//
//     protected override Func<bool> GetCondition(IGoapAgentCore agent) {
//       if (agent is not ICampAgent campAgent) return () => false;
//       
//       return () => {
//         var camp = campAgent.camp;
//         if (camp == null) return false;
//
//         var hasModule = camp.setup.allSpotsFilled;
//         if (!hasModule && !string.IsNullOrEmpty(moduleTag)) {
//           var occupiedSlots = camp.setup.GetOccupiedSpots();
//           hasModule = occupiedSlots.Any(m => m.builtActor.HasAllTags(new[] { moduleTag }));
//         }
//
//         return hasModule;
//       };
//     }
//
//     public override AgentBelief Copy() => new CampHasModuleBelief { name = name, moduleTag = moduleTag };
//   }
// }
