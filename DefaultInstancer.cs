using ChaosGame;
using ChaosMath;
using ChaosGraphics;

namespace Unstable
{
    class DefaultInstancer : InstancingAttribute
    {
        protected WorldScene scene { get; private set; }
        public ShaderContainer.Entry shader { get; protected set; }
        public MaterialContainer.Entry material { get; protected set; }
        public MeshContainer.Entry mesh { get; protected set; }
        public DefaultInstancer() { }
        public DefaultInstancer(int maxInstances, string meshSource, string materialSource, string overrideEffect = null, params string[] customRegisters)
            : base(
                  maxInstances,
                  meshSource.NormalizeRelativePath(),
                  materialSource.NormalizeRelativePath(),
                  overrideEffect == null ? null : overrideEffect.NormalizeRelativePath(),
                  customRegisters
                  )
        { }

        protected DefaultInstancer(int maxInstances, object[] creationParams) : base(maxInstances, creationParams) { }

        public override void Initialize(Graphics graphics, int maxInstances, object[] parameters)
        {
            scene = context<WorldScene>();

            if (parameters != null)
            {
                informer = new Instancer(scene.game.graphics, (string[])parameters[3], maxInstances);
                mesh = scene.game.meshes.Load((string)parameters[0]);
                material = scene.game.materials.Load((string)parameters[1]);
                shader = parameters[2] == null ? scene.game.graphics.defaultEffects.InstancedNormalMap : scene.game.shaders.Load((string)parameters[2]);
            }
        }

        public virtual void DrawInstances(Camera view, string pass)
        {
            if (informer.numInstances == 0)
                return;
            view.SetValues(shader, Matrix.Identity, Matrix.Identity);
            material.content.SetValues(shader);
            mesh.content.DrawInstanced(shader, pass, informer);
        }

        public override void SetDrawCalls()
        {
            scene.drawLayers[(int)WorldScene.DrawLayers.World].Add(() => DrawInstances(scene.view, "World"));
            scene.drawLayers[(int)WorldScene.DrawLayers.Material].Add(() => DrawInstances(scene.view, "Material"));
        }
    }
}
