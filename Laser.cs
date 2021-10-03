using System;
using ChaosGame;
using ChaosGraphics;
using ChaosMath;

namespace Unstable
{
    public class LaserBeam : Particle
    {
        public Vector3 end = new Vector3(4, 0, 0), start = new Vector3(0, 0, 0);
        public float thickness = 0.5f;
        float pulsePhase = ChaosGame.Random.Rnd(ChaosMath.Math.PI_2);
        Func<float> brightnessFunc;
        Func<bool> aliveFunc;
        public Vector4 color = new Vector4(1, 0.2f, 0, 1);

        public LaserBeam(Time ftime, Func<bool> aliveFunc, Func<float> brightnessFunc) : base(ftime)
        {
            particleIndex = SimpleParticles.PARTICLE_INDEX_LASER;
            this.aliveFunc = aliveFunc;
            this.brightnessFunc = brightnessFunc;
        }

        public override bool Update() => aliveFunc();

        protected override Matrix GetTransform(Camera view)
        {
            var direction = end - start;
            var space = Matrix.LocalSpaceNormalized(direction, view.Position - start);
            return Matrix.Translation(0.5f, 0, 0)
                * Matrix.Scaling(thickness * (0.8f + 0.1f * ChaosMath.Math.Sin(pulsePhase + ftime.totalTime)), direction.Length(), 1)
                * space
                * Matrix.Translation(start);
        }
        protected override Vector4 GetColor() => new Vector4(color.rgb, color.a * (ChaosMath.Math.Sin(ftime.totalTime + pulsePhase) + 3) * brightnessFunc());

        public override void SetInstanceData(Instancer instancer, Camera view) =>
            instancer.AddInstance(GetTransform(view), new Vector4(particleIndex.x, particleIndex.y, 1.0f, 0), GetColor());
    }

    public class LaserBarrier : Component<WorldScene>
    {
        float fadeoutTimer = float.NaN;
        float brightness = 1;
        public bool disabled => !float.IsNaN(fadeoutTimer);

        protected override void Create(CreateParameters cparams)
        {
            Func<bool> alive = () => this.alive;
            Func<float> brightness = () => this.brightness;
            int numLaserz = 50;
            
            for (int i = 0; i < numLaserz; i++)
            {
                var newBeam = new LaserBeam(ftime, alive, brightness);
                newBeam.thickness = 0.2f;
                newBeam.start = new Vector3((i - numLaserz / 2.0f) * WorldScene.LEVEL_BOUNDS * 2 / numLaserz, 0, scene.wallDepth);
                newBeam.end = new Vector3(newBeam.start.xy, -50);
                scene.simpleParticles.particles.Add(newBeam);
            }
            for (int i = 0; i < 20; i++)
            {
                var newBeam = new LaserBeam(ftime, alive, brightness);
                newBeam.thickness = 0.2f;
                newBeam.start = new Vector3(-100, 0, scene.wallDepth - i);
                newBeam.end = new Vector3(100, 0, scene.wallDepth - i);
                scene.simpleParticles.particles.Add(newBeam);
            }
        }

        public void Disable() => fadeoutTimer = 0;

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision_DONTDOSHIT].Add(() =>
            {
                if (disabled)
                {
                    fadeoutTimer = ChaosMath.Math.Min(1, fadeoutTimer + 4 * ftime);
                    brightness = 1 - fadeoutTimer;
                    if (fadeoutTimer >= 1)
                        Dispose();
                }
            });
        }
    }
}
