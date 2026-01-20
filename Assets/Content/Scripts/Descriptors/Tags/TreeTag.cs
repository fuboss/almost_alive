namespace Content.Scripts.Game {
  public class TreeTag : TagDefinition {
    public string woodActorID = "wood_0";
    public int woodYield = 4;
    public float workRequired = 10f;
    public override string Tag => AI.Tag.TREE;
  }
}