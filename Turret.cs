using ChaosGraphics;
using ChaosGraphics.Lights;
using ChaosGame;
using ChaosMath;

namespace Unstable
{
    [DefaultInstancer(0, "turret/mesh.gmdl", "turret/material.mat")]
    public class Turret : Component<WorldScene>, Instancable, IPerLevelData
    {
        public Vector3 position;
        public float initialVelocity = 0;
        public float initialDepthVelocity = 4;
        PointLight l;
        public Vector4 lightColor;

        public Vector3 spawn => position + new Vector3(0, 0, -0.2f);

        public float interval = 1;
        public float timer;

        Text txt;
        public System.Func<Missile> createMissile;

        public static Vector4 GetLightColor(intVector2 particleIndex)
        {
            if (particleIndex == SimpleParticles.PARTICLE_INDEX_EXPLOSION)
                return new Vector4(1, 0.666f, 0.2f, 1.5f);
            else if (particleIndex == SimpleParticles.PARTICLE_INDEX_EXPLOSION_GREEN)
                return new Vector4(0.1f, 0.777f, 0.2f, 1.5f);
            else if (particleIndex == SimpleParticles.PARTICLE_INDEX_EXPLOSION_PINK)
                return new Vector4(0.88f, 0.2f, 0.9f, 1.5f);
            return 1;
        }

        protected override void Create(CreateParameters cparams)
        {
            InstancingManager<WorldScene> myInstancer;
            if (scene.instancers.TryGetValue(GetType(), out myInstancer))
                myInstancer.instancedThings.Add(this);
            txt = new Text(scene.game.fontManager, 1);
            l = new PointLight(scene.shader, position, 4, 0);
            scene.shader.lights.Add(l);
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.Ballern].Add(() =>
            {
                timer -= ftime;
                if (timer < 0)
                {
                    timer += interval;
                    var missile = createMissile();
                    missile.physics.state.velocity.z = -initialDepthVelocity;
                    scene.game.soundPool.PlaySound("rocketlaunch.wav", 1);
                }
            });
            l.position = new Vector3(position.xy, 0);
            var cool = (1 - Math.Min(1, timer / interval));
            l.color = lightColor * (0.5f + 4 * ((float)System.Math.Pow(cool, 4)));
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            int intTimer = (int)(timer + 1.0f);
            txt.UpdateText(scene.game.defaultFont, intTimer.ToString(), Align.Center, true);
            txt.color = intTimer > 3 ? new Vector4(1) : (intTimer > 1 ? new Vector4(1, 1, 0.5f, 1) : new Vector4(1, 0.5f, 0.5f, 1));
            txt.transform = Matrix.Scaling(0.4f) * Matrix.Translation(position + new Vector3(-0.25f, 0.25f, -0.5f));
            scene.hudTexts.Add(txt);
        }

        void Instancable.GiveMeInstances(InstancingAttribute[] instancers)
        {
            instancers[0].informer.AddInstance(Matrix.Scaling(2, 2, 1.2f) * Matrix.Translation(position));
        }

        bool Instancable.NeedsInstancedDraw() => true;

        protected override void DoDispose()
        {
            base.DoDispose();
            InstancingManager<WorldScene> myInstancer;
            if (scene.instancers.TryGetValue(GetType(), out myInstancer))
                myInstancer.instancedThings.Remove(this);
            scene.shader.lights.Remove(l);
        }
    }
}
