namespace Content.Scripts.Game {
  public class FoodTag : TagDefinition {
    public float nutrition = 20;
    public override string Tag => AI.Tag.FOOD;
  }
}