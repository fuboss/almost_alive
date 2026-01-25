using Content.Scripts.Building.Data;
using Content.Scripts.Building.Runtime;
using UnityEngine;

namespace Content.Scripts.Building.Data.Expansion {
  /// <summary>
  /// Runtime connection between two structures (base + expansion).
  /// </summary>
  public class StructureConnection {
    public Structure sourceStructure;
    public Structure targetStructure;
    
    public WallSide sourceWall;
    public int sourceSegmentIndex;
    
    public WallSide targetWall;
    public int targetSegmentIndex;
    
    // Runtime state
    public bool isPassageCreated;

    public StructureConnection(
      Structure source, 
      Structure target,
      WallSide sourceWall,
      int sourceSegmentIndex,
      WallSide targetWall,
      int targetSegmentIndex
    ) {
      this.sourceStructure = source;
      this.targetStructure = target;
      this.sourceWall = sourceWall;
      this.sourceSegmentIndex = sourceSegmentIndex;
      this.targetWall = targetWall;
      this.targetSegmentIndex = targetSegmentIndex;
      this.isPassageCreated = false;
    }

    public bool IsValid() {
      return sourceStructure != null && targetStructure != null;
    }

    public bool Contains(Structure structure) {
      return sourceStructure == structure || targetStructure == structure;
    }

    public Structure GetOther(Structure structure) {
      if (structure == sourceStructure) return targetStructure;
      if (structure == targetStructure) return sourceStructure;
      return null;
    }
  }
}
