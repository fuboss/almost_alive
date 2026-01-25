using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Building.Data;
using Content.Scripts.World.Grid;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;

namespace Content.Scripts.Building.EditorUtilities {
  /// <summary>
  /// Editor tool for assembling structure foundation prefabs.
  /// Provides gizmos for footprint visualization and slot placement.
  /// </summary>
  public class StructureFoundationBuilder : SerializedMonoBehaviour {
    [Title("Footprint")]
    [Tooltip("Grid size in cells (X, Z)")]
    [MinValue(1)]
    public Vector2Int footprint = new(3, 3);

    [Title("Entry Points")]
    [Tooltip("Which sides allow entry (stairs placement)")]
    [EnumToggleButtons]
    public EntryDirection entryDirections = EntryDirection.All;

    [Title("Slots")]
    [Tooltip("Module slots within this structure")]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
    public List<SlotDefinition> slots = new();

    [Title("Gizmo Settings")]
    [FoldoutGroup("Gizmos")]
    public bool drawFootprint = true;
    
    [FoldoutGroup("Gizmos")]
    public bool drawSlots = true;
    
    [FoldoutGroup("Gizmos")]
    public bool drawSlotLabels = true;
    
    [FoldoutGroup("Gizmos")]
    public bool drawGridCoords = false;
    
    [FoldoutGroup("Gizmos")]
    public bool drawEntryDirections = true;
    
    [FoldoutGroup("Gizmos")]
    public bool drawTerrainCheck = false;
    
    [FoldoutGroup("Gizmos")]
    [ShowIf("drawTerrainCheck")]
    [Tooltip("Max allowed height variance before warning")]
    [MinValue(0.1f)]
    public float maxHeightVariance = 1f;

    [FoldoutGroup("Gizmos/Colors")]
    public Color footprintColor = new(1f, 1f, 0f, 0.3f);
    
    [FoldoutGroup("Gizmos/Colors")]
    public Color footprintOutlineColor = Color.yellow;
    
    [FoldoutGroup("Gizmos/Colors")]
    public Color sleepingSlotColor = new(0.2f, 0.4f, 1f, 0.8f);
    
    [FoldoutGroup("Gizmos/Colors")]
    public Color productionSlotColor = new(1f, 0.5f, 0.1f, 0.8f);
    
    [FoldoutGroup("Gizmos/Colors")]
    public Color storageSlotColor = new(0.2f, 0.8f, 0.2f, 0.8f);
    
    [FoldoutGroup("Gizmos/Colors")]
    public Color utilitySlotColor = new(0.6f, 0.6f, 0.6f, 0.8f);
    
    [FoldoutGroup("Gizmos/Colors")]
    public Color lockedSlotOutline = Color.red;
    
    [FoldoutGroup("Gizmos/Colors")]
    public Color entryArrowColor = Color.magenta;

    // Cached terrain heights for gizmo drawing
    private float[,] _terrainHeights;
    private float _minTerrainHeight;
    private float _maxTerrainHeight;
    private bool _terrainDataValid;

    private float CellSize => WorldGrid.cellSize;

    #region Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmos() {
      if (drawTerrainCheck) {
        UpdateTerrainHeights();
      }

      if (drawFootprint) {
        DrawFootprintGizmo();
      }

      if (drawEntryDirections) {
        DrawEntryDirectionsGizmo();
      }

      if (drawSlots) {
        DrawSlotsGizmo();
      }
    }

    private void OnDrawGizmosSelected() {
      // Additional details when selected
      if (drawTerrainCheck && _terrainDataValid) {
        DrawTerrainCheckGizmo();
      }
    }

    private void DrawFootprintGizmo() {
      var origin = transform.position;
      var size = new Vector3(footprint.x * CellSize, 0.05f, footprint.y * CellSize);
      var center = origin + new Vector3(size.x * 0.5f, 0, size.z * 0.5f);

      // Fill
      Gizmos.color = footprintColor;
      Gizmos.DrawCube(center, size);

      // Outline
      Gizmos.color = footprintOutlineColor;
      Gizmos.DrawWireCube(center, size);

      // Grid lines
      Gizmos.color = new Color(footprintOutlineColor.r, footprintOutlineColor.g, footprintOutlineColor.b, 0.5f);
      
      for (var x = 0; x <= footprint.x; x++) {
        var start = origin + new Vector3(x * CellSize, 0.01f, 0);
        var end = origin + new Vector3(x * CellSize, 0.01f, footprint.y * CellSize);
        Gizmos.DrawLine(start, end);
      }

      for (var z = 0; z <= footprint.y; z++) {
        var start = origin + new Vector3(0, 0.01f, z * CellSize);
        var end = origin + new Vector3(footprint.x * CellSize, 0.01f, z * CellSize);
        Gizmos.DrawLine(start, end);
      }

      // Grid coords
      if (drawGridCoords) {
        DrawGridCoords(origin);
      }

      // Center marker
      Gizmos.color = Color.white;
      var crossSize = 0.2f;
      Gizmos.DrawLine(center - Vector3.right * crossSize, center + Vector3.right * crossSize);
      Gizmos.DrawLine(center - Vector3.forward * crossSize, center + Vector3.forward * crossSize);
    }

    private void DrawEntryDirectionsGizmo() {
      var origin = transform.position;
      var centerX = footprint.x * CellSize * 0.5f;
      var centerZ = footprint.y * CellSize * 0.5f;
      var arrowLength = 1f;
      
      Gizmos.color = entryArrowColor;

      // North (+Z)
      if ((entryDirections & EntryDirection.North) != 0) {
        var start = origin + new Vector3(centerX, 0.2f, footprint.y * CellSize);
        DrawArrow(start, Vector3.forward * arrowLength);
      }

      // South (-Z)
      if ((entryDirections & EntryDirection.South) != 0) {
        var start = origin + new Vector3(centerX, 0.2f, 0);
        DrawArrow(start, Vector3.back * arrowLength);
      }

      // East (+X)
      if ((entryDirections & EntryDirection.East) != 0) {
        var start = origin + new Vector3(footprint.x * CellSize, 0.2f, centerZ);
        DrawArrow(start, Vector3.right * arrowLength);
      }

      // West (-X)
      if ((entryDirections & EntryDirection.West) != 0) {
        var start = origin + new Vector3(0, 0.2f, centerZ);
        DrawArrow(start, Vector3.left * arrowLength);
      }
    }

    private void DrawArrow(Vector3 start, Vector3 direction) {
      var end = start + direction;
      Gizmos.DrawLine(start, end);
      
      // Arrow head
      var headLength = 0.2f;
      var headWidth = 0.1f;
      var right = Vector3.Cross(Vector3.up, direction.normalized) * headWidth;
      Gizmos.DrawLine(end, end - direction.normalized * headLength + right);
      Gizmos.DrawLine(end, end - direction.normalized * headLength - right);
    }

    private void DrawGridCoords(Vector3 origin) {
      var style = new GUIStyle {
        fontSize = 10,
        normal = { textColor = Color.white },
        alignment = TextAnchor.MiddleCenter
      };

      for (var x = 0; x < footprint.x; x++) {
        for (var z = 0; z < footprint.y; z++) {
          var cellCenter = origin + new Vector3((x + 0.5f) * CellSize, 0.1f, (z + 0.5f) * CellSize);
          UnityEditor.Handles.Label(cellCenter, $"{x},{z}", style);
        }
      }
    }

    private void DrawSlotsGizmo() {
      if (slots == null) return;

      foreach (var slot in slots) {
        if (slot == null) continue;
        
        var worldPos = transform.position + slot.localPosition;
        var color = GetSlotColor(slot.type);
        var radius = 0.3f;

        // Sphere
        Gizmos.color = color;
        Gizmos.DrawSphere(worldPos, radius);

        // Locked outline
        if (slot.startsLocked) {
          Gizmos.color = lockedSlotOutline;
          Gizmos.DrawWireSphere(worldPos, radius + 0.05f);
        }

        // Direction arrow
        Gizmos.color = Color.white;
        var forward = slot.localRotation * Vector3.forward * 0.5f;
        Gizmos.DrawRay(worldPos, forward);
        
        // Arrow head
        var right = slot.localRotation * Vector3.right * 0.1f;
        var arrowTip = worldPos + forward;
        Gizmos.DrawLine(arrowTip, arrowTip - forward * 0.2f + right);
        Gizmos.DrawLine(arrowTip, arrowTip - forward * 0.2f - right);

        // Label
        if (drawSlotLabels) {
          DrawSlotLabel(slot, worldPos + Vector3.up * 0.5f);
        }
      }
    }

    private void DrawSlotLabel(SlotDefinition slot, Vector3 position) {
      var label = string.IsNullOrEmpty(slot.slotId) ? $"[{slot.type}]" : slot.slotId;
      // var color = GetSlotColor(slot.type);
      // color.a = 1f;
      var color = Color.antiqueWhite;
      var style = new GUIStyle {
        fontSize = 11,
        fontStyle = FontStyle.Bold,
        normal = { textColor = color },
        alignment = TextAnchor.MiddleCenter
      };
      UnityEditor.Handles.Label(position, label, style);
    }

    private Color GetSlotColor(SlotType type) {
      return type switch {
        SlotType.Sleeping => sleepingSlotColor,
        SlotType.Production => productionSlotColor,
        SlotType.Storage => storageSlotColor,
        SlotType.Utility => utilitySlotColor,
        _ => Color.white
      };
    }

    private void UpdateTerrainHeights() {
      var terrain = Terrain.activeTerrain;
      if (terrain == null) {
        _terrainDataValid = false;
        return;
      }

      _terrainHeights = new float[footprint.x, footprint.y];
      _minTerrainHeight = float.MaxValue;
      _maxTerrainHeight = float.MinValue;

      var origin = transform.position;

      for (var x = 0; x < footprint.x; x++) {
        for (var z = 0; z < footprint.y; z++) {
          var cellCenter = origin + new Vector3((x + 0.5f) * CellSize, 0, (z + 0.5f) * CellSize);
          var height = terrain.SampleHeight(cellCenter) + terrain.transform.position.y;
          
          _terrainHeights[x, z] = height;
          _minTerrainHeight = Mathf.Min(_minTerrainHeight, height);
          _maxTerrainHeight = Mathf.Max(_maxTerrainHeight, height);
        }
      }

      _terrainDataValid = true;
    }

    private void DrawTerrainCheckGizmo() {
      if (!_terrainDataValid) return;

      var origin = transform.position;
      var variance = _maxTerrainHeight - _minTerrainHeight;
      var isWarning = variance > maxHeightVariance;

      for (var x = 0; x < footprint.x; x++) {
        for (var z = 0; z < footprint.y; z++) {
          var height = _terrainHeights[x, z];
          var cellCenter = origin + new Vector3((x + 0.5f) * CellSize, 0, (z + 0.5f) * CellSize);
          
          // Height relative to max (where foundation would sit)
          var gap = _maxTerrainHeight - height;
          
          if (gap > 0.01f) {
            // This cell needs support
            var supportTop = new Vector3(cellCenter.x, _maxTerrainHeight, cellCenter.z);
            var supportBottom = new Vector3(cellCenter.x, height, cellCenter.z);
            
            // Draw support line
            Gizmos.color = isWarning ? Color.red : Color.cyan;
            Gizmos.DrawLine(supportTop, supportBottom);
            
            // Draw support base
            Gizmos.DrawWireSphere(supportBottom, 0.1f);
          }
          else {
            // Ground contact point
            Gizmos.color = Color.green;
            var contactPoint = new Vector3(cellCenter.x, height, cellCenter.z);
            Gizmos.DrawWireSphere(contactPoint, 0.15f);
          }
        }
      }

      // Draw foundation level plane
      var planeCenter = origin + new Vector3(footprint.x * CellSize * 0.5f, _maxTerrainHeight, footprint.y * CellSize * 0.5f);
      var planeSize = new Vector3(footprint.x * CellSize, 0.02f, footprint.y * CellSize);
      Gizmos.color = isWarning ? new Color(1f, 0f, 0f, 0.3f) : new Color(0f, 1f, 1f, 0.3f);
      Gizmos.DrawCube(planeCenter, planeSize);
    }
#endif

    #endregion

    #region Validation

    /// <summary>
    /// Check if a slot position is within footprint bounds.
    /// </summary>
    public bool IsSlotInBounds(SlotDefinition slot) {
      if (slot == null) return false;
      
      var pos = slot.localPosition;
      return pos.x >= 0 && pos.x <= footprint.x * CellSize &&
             pos.z >= 0 && pos.z <= footprint.y * CellSize;
    }

    /// <summary>
    /// Get validation errors for current configuration.
    /// </summary>
    public List<string> Validate() {
      var errors = new List<string>();

      if (footprint.x <= 0 || footprint.y <= 0) {
        errors.Add("Footprint must be at least 1x1");
      }

      if (slots == null || slots.Count == 0) {
        errors.Add("No slots defined");
      }
      else {
        // Check slot IDs
        var ids = new HashSet<string>();
        for (var i = 0; i < slots.Count; i++) {
          var slot = slots[i];
          if (slot == null) {
            errors.Add($"Slot {i} is null");
            continue;
          }

          if (string.IsNullOrEmpty(slot.slotId)) {
            errors.Add($"Slot {i} has empty slotId");
          }
          else if (!ids.Add(slot.slotId)) {
            errors.Add($"Duplicate slotId: {slot.slotId}");
          }

          if (!IsSlotInBounds(slot)) {
            errors.Add($"Slot '{slot.slotId}' is outside footprint bounds");
          }
        }
      }

      // Check for View child with NavMeshSurface
      var navMeshSurface = GetComponentInChildren<NavMeshSurface>(true);
      if (navMeshSurface == null) {
        errors.Add("No child with NavMeshSurface found. Add a 'View' child object with NavMeshSurface component.");
      }

      // Check entry directions
      if (entryDirections == EntryDirection.None) {
        errors.Add("No entry directions set. Structure will be inaccessible.");
      }

      return errors;
    }

    /// <summary>
    /// Get terrain height variance info.
    /// </summary>
    public (float min, float max, float variance) GetTerrainVariance() {
      if (!_terrainDataValid) {
        UpdateTerrainHeights();
      }

      if (!_terrainDataValid) {
        return (0, 0, 0);
      }

      return (_minTerrainHeight, _maxTerrainHeight, _maxTerrainHeight - _minTerrainHeight);
    }

    #endregion

    #region Slot Helpers

    /// <summary>
    /// Add a new slot at specified local position.
    /// </summary>
    public SlotDefinition AddSlot(Vector3 localPosition, SlotType type) {
      var slot = new SlotDefinition {
        slotId = $"slot_{slots.Count}",
        type = type,
        localPosition = localPosition,
        localRotation = Quaternion.identity,
        isInterior = true,
        startsLocked = false
      };
      slots.Add(slot);
      return slot;
    }

    /// <summary>
    /// Add slot at cell center.
    /// </summary>
    public SlotDefinition AddSlotAtCell(int cellX, int cellZ, SlotType type) {
      var localPos = new Vector3((cellX + 0.5f) * CellSize, 0, (cellZ + 0.5f) * CellSize);
      var slot = new SlotDefinition {
        slotId = $"slot_{cellX}_{cellZ}",
        type = type,
        localPosition = localPos,
        localRotation = Quaternion.identity,
        isInterior = true,
        startsLocked = false
      };
      slots.Add(slot);
      return slot;
    }

    /// <summary>
    /// Get slot at cell position (if any).
    /// </summary>
    public SlotDefinition GetSlotAtCell(int cellX, int cellZ) {
      var cellCenter = new Vector3((cellX + 0.5f) * CellSize, 0, (cellZ + 0.5f) * CellSize);
      foreach (var slot in slots) {
        if (slot == null) continue;
        var slotCellX = Mathf.FloorToInt(slot.localPosition.x / CellSize);
        var slotCellZ = Mathf.FloorToInt(slot.localPosition.z / CellSize);
        if (slotCellX == cellX && slotCellZ == cellZ) return slot;
      }
      return null;
    }

    /// <summary>
    /// Remove slot at cell position.
    /// </summary>
    public bool RemoveSlotAtCell(int cellX, int cellZ) {
      var slot = GetSlotAtCell(cellX, cellZ);
      if (slot == null) return false;
      slots.Remove(slot);
      return true;
    }

    /// <summary>
    /// Convert world position to cell coordinates.
    /// </summary>
    public Vector2Int WorldToCell(Vector3 worldPos) {
      var local = worldPos - transform.position;
      return new Vector2Int(
        Mathf.FloorToInt(local.x / CellSize),
        Mathf.FloorToInt(local.z / CellSize)
      );
    }

    /// <summary>
    /// Check if cell is within footprint bounds.
    /// </summary>
    public bool IsCellInBounds(int cellX, int cellZ) {
      return cellX >= 0 && cellX < footprint.x && cellZ >= 0 && cellZ < footprint.y;
    }

    /// <summary>
    /// Create one slot at center of each cell.
    /// </summary>
    public void CreateSlotsForAllCells(SlotType defaultType = SlotType.Utility) {
      slots.Clear();
      
      for (var x = 0; x < footprint.x; x++) {
        for (var z = 0; z < footprint.y; z++) {
          var localPos = new Vector3((x + 0.5f) * CellSize, 0, (z + 0.5f) * CellSize);
          var slot = new SlotDefinition {
            slotId = $"slot_{x}_{z}",
            type = defaultType,
            localPosition = localPos,
            localRotation = Quaternion.identity,
            isInterior = true,
            startsLocked = false
          };
          slots.Add(slot);
        }
      }
    }

    /// <summary>
    /// Mirror all slots along X axis.
    /// </summary>
    public void MirrorSlotsX() {
      var centerX = footprint.x * CellSize * 0.5f;
      var newSlots = new List<SlotDefinition>();

      foreach (var slot in slots) {
        // Mirror position
        var mirroredX = centerX + (centerX - slot.localPosition.x);
        var mirroredPos = new Vector3(mirroredX, slot.localPosition.y, slot.localPosition.z);

        // Check if mirrored position already has a slot
        var exists = slots.Any(s => Vector3.Distance(s.localPosition, mirroredPos) < 0.1f);
        if (exists) continue;

        // Mirror rotation (flip around Y axis)
        var mirroredRot = Quaternion.Euler(
          slot.localRotation.eulerAngles.x,
          -slot.localRotation.eulerAngles.y,
          slot.localRotation.eulerAngles.z
        );

        var newSlot = new SlotDefinition {
          slotId = $"{slot.slotId}_mirror",
          type = slot.type,
          localPosition = mirroredPos,
          localRotation = mirroredRot,
          acceptedModuleTags = slot.acceptedModuleTags?.ToArray(),
          isInterior = slot.isInterior,
          startsLocked = slot.startsLocked
        };
        newSlots.Add(newSlot);
      }

      slots.AddRange(newSlots);
    }

    #endregion
  }
}
