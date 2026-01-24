using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public interface ILayerBuilder {
    string LayerName { get; }
    int LayerIndex { get; }
    void Build(AnimatorController controller, LayerBuildContext context);
  }

  public class LayerBuildContext {
    public AnimationClipProvider ClipProvider { get; }
    public BlendTreeFactory BlendTreeFactory { get; }
    public AnimatorGeneratorConfig Config { get; }
    public TransitionFactory TransitionFactory { get; }

    public LayerBuildContext(
      AnimationClipProvider clipProvider,
      BlendTreeFactory blendTreeFactory,
      AnimatorGeneratorConfig config,
      TransitionFactory transitionFactory) {
      ClipProvider = clipProvider;
      BlendTreeFactory = blendTreeFactory;
      Config = config;
      TransitionFactory = transitionFactory;
    }
  }
}

