using ChaosMath;
using ChaosGraphics;
using ChaosGame;

namespace Unstable
{
    public class SimpleParticles : ParticleSystem
    {
        public static readonly intVector2 PARTICLE_INDEX_LASER = new intVector2(0, 1);
        public static readonly intVector2 PARTICLE_INDEX_EXPLOSION = new intVector2(1, 1);
        public static readonly intVector2 PARTICLE_INDEX_EXPLOSION_PINK = new intVector2(2, 1);
        public static readonly intVector2 PARTICLE_INDEX_EXPLOSION_GREEN = new intVector2(3, 1);
        public static readonly intVector2 PARTICLE_INDEX_SMOKE = new intVector2(0, 0);
        static readonly intVector2 NUM_PARTICLES_IN_TEXTURE = new intVector2(4, 2);

        //ParticleOwner particler;

        protected override void Create(CreateParameters cparams)
        {
            var particler = (WorldScene)scene;

            Initialize(((Game)scene.game).graphics, particler.view, 10000, (int)WorldScene.UpdateLayers.Particles, new[] { "PARTICLE_TEXOFFSET", "PARTICLE_COLOR" });
            effect = ((Game)scene.game).shaders.Load("shaders/simple_particles.fx");
            maskTexture = ((Game)scene.game).textures.Load("particles.png", this);
            numParticlesInTexture = NUM_PARTICLES_IN_TEXTURE;
        }

        public override void DrawMask(TransparencyRenderer renderer)
        {
            if (instancer.numInstances == 0)
                return;
            
            view.SetValues(effect, Matrix.Identity);
            effect.SetValue("tex", maskTexture);
            effect.SetValue("texAtlasSize", 1.0f / numParticlesInTexture);
            effect.SetValue("maskTexBias", 0.3f);
            renderer.SetMaskRenderingValues(effect);
            Sprite.DrawPositionInstanced(graphics, effect, instancer, "Mask");
        }

        public override void DrawTransparent(TransparencyRenderer renderer)
        {
            effect.SetValue("tex", maskTexture);
            DrawTransparent(renderer, "All");
        }
    }
}