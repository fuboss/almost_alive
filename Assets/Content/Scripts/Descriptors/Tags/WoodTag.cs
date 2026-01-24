using System;
using UnityEngine;

namespace Content.Scripts.Game {
  public class WoodTag : TagDefinition {
    public override string Tag => AI.Tag.WOOD;

    private void OnDestroy() {
    }
  }
}