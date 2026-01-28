using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Utility {
  /// <summary>
  /// Base class for ScriptableObjects that wrap configuration data.
  /// TData should be a serializable class containing actual config fields.
  /// This pattern separates data from the SO container.
  /// </summary>
  /// <example>
  /// // 1. Define data class
  /// [Serializable]
  /// public class MyConfig {
  ///   public float speed = 1f;
  ///   public int count = 10;
  /// }
  /// 
  /// // 2. Create SO wrapper
  /// [CreateAssetMenu(menuName = "Config/My Config")]
  /// public class MyConfigSO : ScriptableConfig&lt;MyConfig&gt; { }
  /// 
  /// // 3. Usage
  /// var data = myConfigSO.Data;  // direct access
  /// </example>
  public abstract class ScriptableConfig<TData> : SerializedScriptableObject 
    where TData : class, new() {
    
    [HideLabel]
    [InlineProperty]
    [SerializeField]
    protected TData _data = new();

    /// <summary>
    /// Direct access to config data.
    /// </summary>
    public TData Data => _data;
  }
}
