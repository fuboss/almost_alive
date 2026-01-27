using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Harversting {
  public interface IHarvestable {
    void AddProgress(int amount);
    bool IsFertile();
    void ResetProgress();
  }

  public class FertilityProgress : MonoBehaviour, IHarvestable {
    public int currentProgress;
    public int maxProgress;

    public FertilityProgress(int maxProgress) {
      this.maxProgress = maxProgress;
      currentProgress = 0;
    }

    public void AddProgress(int amount) {
      currentProgress += amount;
      if (currentProgress > maxProgress) {
        currentProgress = maxProgress;
      }
    }

    public bool IsFertile() {
      return currentProgress >= maxProgress;
    }

    public void ResetProgress() {
      currentProgress = 0;
    }
  }
}