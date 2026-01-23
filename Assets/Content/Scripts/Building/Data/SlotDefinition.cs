using System;
using System.Collections.Generic;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Data {
  [Serializable]
  public class SlotDefinition {
    [Tooltip("Unique slot identifier within structure")]
    public string slotId;

    [Tooltip("What category of modules this slot accepts")]
    public SlotType type;

    [Tooltip("Position relative to structure origin")]
    public Vector3 localPosition;

    [Tooltip("Rotation relative to structure")]
    public Quaternion localRotation = Quaternion.identity;

    [Tooltip("Specific module tags this slot accepts (empty = all for this SlotType)")]
    [ValueDropdown("Tags")]
    public string[] acceptedModuleTags;

    [Tooltip("Is this slot under roof/walls (weather protected)")]
    public bool isInterior = true;

    [Tooltip("Requires structure upgrade to unlock")]
    public bool startsLocked;

#if UNITY_EDITOR
    private IEnumerable<string> Tags() => Tag.ALL_TAGS;
#endif
  }
}
