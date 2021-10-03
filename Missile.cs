using ChaosGraphics;
using ChaosPhysics;
using ChaosGame;
using ChaosMath;
using Collections;
using ChaosGraphics.Lights;

namespace Unstable
{
    [DefaultInstancer(0, "missile/missile.gmdl", "missile/material.mat")]
    public class Missile : Component<WorldScene>, Instancable, IPerLevelData
    {
        float age = 0;
        public float lifetime = 30;
        public Physical physics;
        float particleTimer = 0;
        float particleInterval = 0.01f;
        public float radius { get; protected set; } = 0.35f;
        public intVector2 particleIndex = SimpleParticles.PARTICLE_INDEX_EXPLOSION;
        protected PointLight light;

        protected override void Create(CreateParameters cparams)
        {
            InstancingManager<WorldScene> myInstancer;
            if (scene.instancers.TryGetValue(GetType(), out myInstancer))
                myInstancer.instancedThings.Add(this);

            physics = new Physical(this);
            physics.isStatic = true;
            scene.GetOrAssignSceneData(typeof(Missile), () => new LinkedList<Missile>()).Add(this);
            light = new PointLight(scene.shader, physics.state.position, 3.5f, new Vector4(1, 0.5f, 0, 1));
            scene.shader.lights.Add(light);
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();

            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(() =>
            {
                float minAge = 0.1f;
                physics.state.Move(ftime);
                if (age > minAge)
                {
                    foreach (var m in scene.GetSceneData<LinkedList<Missile>>(typeof(Missile)))
                    {
                        if (m == this || !m.alive)
                            continue;
                        if (m.age > minAge && (m.physics.state.position - physics.state.position).LengthSq() < (radius + m.radius) * (radius + m.radius))
                        {
                            Explode();
                            m.Explode();
                        }
                    }
                    if (!scene.player.dead && (scene.player.center - physics.state.position).LengthSq() < (scene.player.collisionRadius + radius) * (scene.player.collisionRadius + radius))
                    {
                        scene.player.Die();
                        Explode();
                    }
                }
            });

            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision_DONTDOSHIT].Add(() =>
            {
                age += ftime;
                if ((lifetime -= ftime) < 0)
                    Explode();
            });
            scene.updateLayers[(int)WorldScene.UpdateLayers.CreateParticles].Add(() =>
            {
                particleTimer -= ftime;
                while (particleTimer < 0)
                {
                    particleTimer += particleInterval;

                    var p = new ExplosionParticle(ftime, physics.state.position, Random.RndVector3(Random.Rnd()), 0.5f);
                    p.size = 0.2f;
                    p.particleIndex = particleIndex;
                    scene.simpleParticles.particles.Add(p);
                }
                light.color = Turret.GetLightColor(particleIndex);
                light.position = physics.state.position;
            });
        }

        public virtual void Explode()
        {
            scene.game.soundPool.PlaySound("explosion.wav", 1);

            int num = Random.RndInt(30, 35);
            for (int i = 0; i < num; i++)
            {
                var p = new ExplosionParticle(
                    ftime,
                    physics.state.position,
                    Random.RndVector3(Random.Rnd(5)),
                    Random.Rnd(0.5f, 1.0f)
                    );
                p.particleIndex = particleIndex;
                scene.simpleParticles.particles.Add(p);
            }
            for (int i = 0; i < 20; i++)
            {
                scene.simpleParticles.particles.Add(new SmokeParticle(
                    ftime,
                    physics.state.position,
                    Random.RndVector3(Random.Rnd(3)),
                    Random.Rnd(1.5f, 2),
                    1.2f,
                    2
                    ));
            }
            Dispose();
        }

        void Instancable.GiveMeInstances(InstancingAttribute[] instancers)
        {
            instancers[0].informer.AddInstance(
                Matrix.RotationY(ftime.totalTime)
                * Matrix.LocalSpaceNormalized(physics.state.velocity, new Vector3(0, 0, 1))
                * Matrix.Translation(physics.state.position)
                );
        }

        bool Instancable.NeedsInstancedDraw() => true;

        protected override void DoDispose()
        {
            base.DoDispose();
            InstancingManager<WorldScene> myInstancer;
            if (scene.instancers.TryGetValue(GetType(), out myInstancer))
                myInstancer.instancedThings.Remove(this);
            scene.GetSceneData<LinkedList<Missile>>(typeof(Missile)).Remove(this);
            scene.shader.lights.Remove(light);
        }
    }
}
