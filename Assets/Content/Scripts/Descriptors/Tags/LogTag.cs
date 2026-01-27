namespace Content.Scripts.Game {
  public class LogTag : TagDefinition {
    public string plankActorID = "plank_0";
    public string woodActorID = "wood_0";
    public int plankYield = 2;
    public int woodSource = 2;
    public float workRequired = 8f;
    public override string Tag => AI.Tag.LOG;
  }
}
