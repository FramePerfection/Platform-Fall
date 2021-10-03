using Collections;
using ChaosInput;
using ChaosGame;
using ChaosMath;

namespace Unstable
{
    public class PlayerMissile : Missile
    {
        float HOMING_SPEED = 50;
        Player player;
        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            player = ((CParams<Player>)cparams).v1;
            particleIndex = SimpleParticles.PARTICLE_INDEX_EXPLOSION_GREEN;
        }

        public override void SetUpdateCalls()
        {
            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(() =>
            {
                Missile target = null;
                var nearest = float.PositiveInfinity;
                float tmpLen;
                foreach (var m in scene.GetSceneData<LinkedList<Missile>>(typeof(Missile)))
                    if (m is HomingMissile)
                        if ((tmpLen = (m.physics.state.position - physics.state.position).LengthSq()) < nearest)
                        {
                            nearest = tmpLen;
                            target = m;
                        }
                if (target != null)
                {
                    physics.state.velocity += Vector3.Normalize(target.physics.state.position - physics.state.position) * ftime * HOMING_SPEED;
                    physics.state.velocity *= (1 - Math.EaseIn(ftime * 1.2f));
                }
            });
            base.SetUpdateCalls();
            scene.game.input.AddHandler(Game.InputLayer.All, e =>
            {
                if (e.type == InputEvent.EventType.Push && player.inputs[(int)Player.InputAction.DetonateRocket].Contains(e.axis))
                {
                    Explode();
                    return true;
                }
                return false;
            });
        }

        public override void Explode()
        {
            float radius = 2.2f;
            float playerDestroyRadius = 1.5f;
            foreach (var m in scene.GetSceneData<LinkedList<Missile>>(typeof(Missile)))
            {
                if (m == this || !m.alive)
                    continue;
                if ((m.physics.state.position - physics.state.position).LengthSq() < (radius + m.radius) * (radius + m.radius))
                    m.Explode();
            }
            var rdsum = player.collisionRadius + playerDestroyRadius;
            if ((scene.player.center - physics.state.position).LengthSq() < rdsum * rdsum)
                scene.player.Die();
            base.Explode();
        }
    }
}
