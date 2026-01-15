using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.Game {
  public class ItemTag : TagDefinition {
    public float weight;
    [SerializeReference] public StackData stackData;
    public override string Tag => AI.Tag.ITEM;
  }
}