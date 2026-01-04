using System.Linq;
using UnityEngine;

namespace Reflex.Core {
  public static class Ext {
    public static ContainerBuilder AddSingletonAutoContracts(this ContainerBuilder b, object instance) {
      var contracts = instance.GetType().GetInterfaces();
      if (contracts.Length > 0) {
        Debug.Log($"AddingAuto {instance.GetType().Name} with: {string.Join(", ", contracts.Select(c => c.Name))}");
        return b.AddSingleton(instance, contracts);
      }

      return b.AddSingleton(instance);
    }
  }
}