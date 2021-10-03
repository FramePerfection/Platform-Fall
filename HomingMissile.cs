using ChaosGame;
using ChaosMath;
namespace Unstable
{
    public abstract class HostileMissile : Missile
    {
        static float HOMING_SPEED = 25;

        public override void SetUpdateCalls()
        {
            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(() => Move(ftime));
            base.SetUpdateCalls();
        }
        protected abstract void Move(float ftime);
    }

    public class HomingMissile : HostileMissile
    {
        static float HOMING_SPEED = 25;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            particleIndex = SimpleParticles.PARTICLE_INDEX_EXPLOSION_PINK;
            lifetime = 4f;
        }

        protected override void Move(float ftime)
        {
            if (scene.player.dead)
                return;
            physics.state.velocity += Vector3.Normalize(scene.player.center - physics.state.position) * ftime * HOMING_SPEED;
            physics.state.velocity *= (1 - Math.EaseIn(ftime * 1.2f));
            physics.state.velocity.z *= (1 - Math.EaseIn(ftime * 4));
        }
    }

    public class StraightMissile : HostileMissile
    {
        static float HOMING_SPEED = 25;

        protected override void Move(float ftime)
        {
            physics.state.velocity.z += Vector3.Normalize(scene.player.center - physics.state.position).z * ftime * HOMING_SPEED;
            physics.state.velocity.z *= (1 - Math.EaseIn(ftime * 4));
        }
    }
}
