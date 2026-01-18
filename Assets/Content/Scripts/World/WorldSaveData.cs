using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.World {
  [Serializable]
  public class WorldSaveData {
    public int version = 1;
    public string timestamp;
    public float simTime;
    public List<ActorSaveData> actors = new();
    public List<AgentSaveData> agents = new();
  }

  [Serializable]
  public class ActorSaveData {
    public string actorKey;
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
    public SerializableVector3 scale;
    public int stackCount;

    // Optional custom data (JSON string for extensibility)
    public string customData;
  }

  [Serializable]
  public class AgentSaveData {
    public int agentId;
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
    public AgentStatsSaveData stats;
  }

  [Serializable]
  public class AgentStatsSaveData {
    public float health;
    public float hunger;
    public float fatigue;
    public float bladder;
    public int level;
    public int experience;
  }

  // Unity types aren't serializable to JSON, so we wrap them
  [Serializable]
  public struct SerializableVector3 {
    public float x, y, z;

    public SerializableVector3(Vector3 v) {
      x = v.x;
      y = v.y;
      z = v.z;
    }

    public Vector3 ToVector3() => new(x, y, z);

    public static implicit operator SerializableVector3(Vector3 v) => new(v);
    public static implicit operator Vector3(SerializableVector3 v) => v.ToVector3();
  }

  [Serializable]
  public struct SerializableQuaternion {
    public float x, y, z, w;

    public SerializableQuaternion(Quaternion q) {
      x = q.x;
      y = q.y;
      z = q.z;
      w = q.w;
    }

    public Quaternion ToQuaternion() => new(x, y, z, w);

    public static implicit operator SerializableQuaternion(Quaternion q) => new(q);
    public static implicit operator Quaternion(SerializableQuaternion q) => q.ToQuaternion();
  }
}
