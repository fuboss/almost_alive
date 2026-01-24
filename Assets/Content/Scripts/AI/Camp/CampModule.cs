// using System;
// using System.Collections.Generic;
// using Content.Scripts.AI.GOAP.Agent;
// using UnityEngine;
// using UnityEngine.AddressableAssets;
// using VContainer;
// using VContainer.Unity;
// using Random = UnityEngine.Random;
//
// namespace Content.Scripts.AI.Camp {
//   /// <summary>
//   /// Central module for camp management. Handles camp setup instantiation
//   /// and per-agent camp data caching.
//   /// </summary>
//   public class CampModule : IInitializable, IDisposable {
//     [Inject] private IObjectResolver _resolver;
//
//     private readonly List<CampSetup> _setupPrefabs = new();
//     private readonly Dictionary<ICampAgent, AgentCampData> _agentCampData = new();
//     private bool _loaded;
//
//     void IInitializable.Initialize() {
//       Addressables.LoadAssetsAsync<GameObject>("CampSetups").Completed += handle => {
//         foreach (var go in handle.Result) {
//           if (go.TryGetComponent<CampSetup>(out var setup))
//             _setupPrefabs.Add(setup);
//         }
//         _loaded = true;
//         Debug.Log($"[CampModule] Loaded {_setupPrefabs.Count} camp setups");
//       };
//     }
//
//     public bool isReady => _loaded && _setupPrefabs.Count > 0;
//
//     /// <summary>Get or create camp data for agent.</summary>
//     public AgentCampData GetAgentCampData(ICampAgent agent) {
//       if (agent == null) return null;
//       
//       if (!_agentCampData.TryGetValue(agent, out var data)) {
//         data = new AgentCampData();
//         _agentCampData[agent] = data;
//       }
//       return data;
//     }
//
//     /// <summary>Register camp for agent. Updates agent's camp data.</summary>
//     public void RegisterAgentCamp(ICampAgent agent, CampLocation camp) {
//       var data = GetAgentCampData(agent);
//       data?.SetCamp(camp);
//     }
//
//     /// <summary>Unregister agent's camp.</summary>
//     public void UnregisterAgentCamp(ICampAgent agent) {
//       var data = GetAgentCampData(agent);
//       data?.ClearCamp();
//     }
//
//     /// <summary>Remove agent from tracking entirely.</summary>
//     public void RemoveAgent(ICampAgent agent) {
//       _agentCampData.Remove(agent);
//     }
//
//     /// <summary>Instantiates random CampSetup at given location.</summary>
//     public CampSetup InstantiateRandomSetup(CampLocation location) {
//       if (!isReady) {
//         Debug.LogError("[CampModule] Not ready or no setups loaded");
//         return null;
//       }
//       
//       var prefab = _setupPrefabs[Random.Range(0, _setupPrefabs.Count)];
//       var instance = _resolver.Instantiate(prefab, location.transform.position, Quaternion.identity);
//       location.AssignSetup(instance);
//       return instance;
//     }
//
//     /// <summary>Instantiates specific CampSetup by index.</summary>
//     public CampSetup InstantiateSetup(CampLocation location, int index) {
//       if (!isReady || index < 0 || index >= _setupPrefabs.Count) return null;
//       
//       var prefab = _setupPrefabs[index];
//       var instance = _resolver.Instantiate(prefab, location.transform.position, Quaternion.identity);
//       location.AssignSetup(instance);
//       return instance;
//     }
//
//     void IDisposable.Dispose() {
//       _setupPrefabs.Clear();
//       _agentCampData.Clear();
//     }
//   }
// }
