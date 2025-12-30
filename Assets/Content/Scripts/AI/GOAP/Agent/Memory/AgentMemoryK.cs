using System.Collections.Generic;

namespace Content.Scripts.AI.GOAP.Agent {
  public struct MemoryKey : IEqualityComparer<MemoryKey> {
    public string key;
    public object meta;

    public MemoryKey(string key, object meta = null) {
      this.key = key;
      this.meta = meta;
    }

    public bool Equals(MemoryKey x, MemoryKey y) {
      return x.key == y.key;
    }

    public int GetHashCode(MemoryKey obj) {
      return obj.key.GetHashCode();
    }
  }

  public class AgentMemoryK {
    public readonly Dictionary<MemoryKey, object> memory = new();

    public void Remember(string key, object value) {
      memory[new MemoryKey(key)] = value;
    }

    public T Recall<T>(string key) {
      if (memory.TryGetValue(new MemoryKey(key), out var value) && value is T typedValue) {
        return typedValue;
      }

      return default;
    }

    public void Forget(string key) {
      var keyToRemove = new MemoryKey(key);
      memory.Remove(keyToRemove);
    }
  }
}