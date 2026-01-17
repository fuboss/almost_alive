namespace Content.Scripts.Game {
  public class TreeTag : TagDefinition {
    public float woodYield = 4;
    public override string Tag => AI.Tag.TREE;
  }
}