using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.Game;
using Content.Scripts.Game.Work;
using UnityEngine;
using UnityEngine.AI;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Full agent interface combining all capabilities.
  /// Used by GOAPAgent (human colonist).
  /// </summary>
  public interface IGoapAgent : IGoapAgentCore, ITransientTargetAgent, IInventoryAgent, IWorkAgent, ICampAgent, ITickable {
  }
}
