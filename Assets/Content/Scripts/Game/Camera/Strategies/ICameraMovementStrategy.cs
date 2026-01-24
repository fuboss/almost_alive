using UnityEngine;

namespace Content.Scripts.Game.Camera.Strategies {
  /// <summary>
  /// Strategy interface for different camera movement modes
  /// </summary>
  public interface ICameraMovementStrategy {
    /// <summary>
    /// Priority of this strategy (higher = takes precedence)
    /// </summary>
    int priority { get; }
    
    /// <summary>
    /// Whether this strategy is currently active and should control the camera
    /// </summary>
    bool isActive { get; }
    
    /// <summary>
    /// Called every frame to update the target focus point
    /// </summary>
    /// <param name="currentFocusPoint">Current focus point position</param>
    /// <param name="deltaTime">Time since last frame</param>
    /// <returns>New focus point position</returns>
    Vector3 UpdateFocusPoint(Vector3 currentFocusPoint, float deltaTime);
    
    /// <summary>
    /// Called when this strategy becomes active
    /// </summary>
    void OnActivate();
    
    /// <summary>
    /// Called when this strategy becomes inactive
    /// </summary>
    void OnDeactivate();
  }
}

