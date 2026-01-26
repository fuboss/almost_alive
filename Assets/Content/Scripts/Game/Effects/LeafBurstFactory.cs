using UnityEngine;

namespace Content.Scripts.Game.Effects {
  public static class LeafBurstFactory {
    public static GameObject CreateLeafBurstPrefab() {
      var go = new GameObject("LeafBurst");
      
      var ps = go.AddComponent<ParticleSystem>();
      
      // Stop immediately to allow setting properties
      ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
      
      var main = ps.main;
      main.playOnAwake = false;
      main.duration = 1f;
      main.loop = false;
      main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
      main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
      main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
      main.startColor = new ParticleSystem.MinMaxGradient(
        new Color(0.2f, 0.6f, 0.1f),
        new Color(0.4f, 0.8f, 0.2f)
      );
      main.gravityModifier = 0.3f;
      main.simulationSpace = ParticleSystemSimulationSpace.World;
      main.maxParticles = 50;

      var emission = ps.emission;
      emission.enabled = true;
      emission.rateOverTime = 0;
      emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30, 50) });

      var shape = ps.shape;
      shape.enabled = true;
      shape.shapeType = ParticleSystemShapeType.Sphere;
      shape.radius = 1f;


      var sizeOverLifetime = ps.sizeOverLifetime;
      sizeOverLifetime.enabled = true;
      var sizeCurve = new AnimationCurve();
      sizeCurve.AddKey(0f, 1f);
      sizeCurve.AddKey(1f, 0f);
      sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

      var colorOverLifetime = ps.colorOverLifetime;
      colorOverLifetime.enabled = true;
      var gradient = new Gradient();
      gradient.SetKeys(
        new[] {
          new GradientColorKey(new Color(0.3f, 0.7f, 0.2f), 0f),
          new GradientColorKey(new Color(0.6f, 0.5f, 0.1f), 1f)
        },
        new[] {
          new GradientAlphaKey(1f, 0f),
          new GradientAlphaKey(0f, 1f)
        }
      );
      colorOverLifetime.color = gradient;

      var rotation = ps.rotationOverLifetime;
      rotation.enabled = true;
      rotation.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

      var renderer = go.GetComponent<ParticleSystemRenderer>();
      renderer.renderMode = ParticleSystemRenderMode.Billboard;
      
      return go;
    }
  }
}
