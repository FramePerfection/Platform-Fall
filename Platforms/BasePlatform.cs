using ChaosGraphics;
using ChaosGame;
using ChaosMath;
using ChaosPhysics;
using Collections;

namespace Unstable
{

    public abstract class BasePlatform : Component<WorldScene>, Instancable, IPerLevelData
    {
        public Physical physics;
        public Vector3 position { get { return physics.state.position; } set { physics.state.position = value; } }
        float interactionTimer = 0;
        public int stability = 1;
        public bool permanent = false;
        public const float INTERACTION_INTERVAL = 0.666f;

        protected Text txt;
        protected override void Create(CreateParameters cparams)
        {
            txt = new Text(scene.game.fontManager, 1);
            physics = new Physical(this);
            scene.physics.Add(physics);
            InstancingManager<WorldScene> myInstancer;
            if (scene.instancers.TryGetValue(GetType(), out myInstancer))
            {
                myInstancer.instancedThings.Add(this);
                var shape = new ChaosPhysics.Shapes.MeshShape(((DefaultInstancer)myInstancer.instancers[0]).mesh.content.data.pos);
                shape.bounce = 0;
                physics.shapes.Add(shape);
            }

            physics.isStatic = true;
            scene.GetOrAssignSceneData(typeof(BasePlatform), () => new LinkedList<BasePlatform>()).Add(this);
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision_Manipulate].Add(() =>
            {
                if (interactionTimer <= 0.7f * INTERACTION_INTERVAL && stability <= 0)
                {
                    scene.game.soundPool.PlaySound("platform/break.wav", 1.0f);
                    BreakInternal();
                    Dispose();
                }
                interactionTimer = Math.Max(0, interactionTimer - ftime);
            });
        }

        protected abstract void BreakInternal();

        protected void CreateFragment(int index)
        {
            var t = physics.state.GetTransform();
            var seg = scene.AddComponent<PlatformFragment>(CreateParameters.Create(index));
            seg.physics.state.baseTransform *= physics.state.baseTransform;
            seg.physics.state.position = Vector3.TransformCoordinate(seg.baseOffset, t);
            seg.physics.UpdateShapes(true);
            for (int k = 0; k < Random.RndInt(10, 12); k++)
                scene.simpleParticles.particles.Add(new SmokeParticle(
                    ftime,
                    seg.physics.shapes[0].GetRandomPositionInShape(),
                    Random.RndVector3(Random.Rnd(5)),
                    Random.Rnd(1.5f, 2),
                    0.5f,
                    1
                    ));
        }

        public void Interact(Player player)
        {
            if (InteractInternal(player))
            {
                if (interactionTimer == 0 && !permanent)
                    stability--;
                interactionTimer = INTERACTION_INTERVAL;
            }
        }

        protected abstract bool InteractInternal(Player player);

        public abstract void GiveMeInstances(InstancingAttribute[] instancers);

        bool Instancable.NeedsInstancedDraw() => true;


        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            txt.UpdateText(scene.game.defaultFont, permanent ? "Stable" : stability.ToString(), Align.Center);
            txt.channelMultipliers = new Vector3(1, 0, 0);
            if (!permanent)
                txt.color = stability > 0 ? (interactionTimer == 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 1, 0, 1)) : new Vector4(1, 0, 0, 1);
            else
                txt.color = new Vector4(0, 0.5f, 1.0f, 1);
            scene.materialTexts.Add(txt);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            txt.Dispose();
            scene.physics.Remove(physics);
            InstancingManager<WorldScene> myInstancer;
            if (scene.instancers.TryGetValue(GetType(), out myInstancer))
                myInstancer.instancedThings.Remove(this);
            scene.GetSceneData<LinkedList<BasePlatform>>(typeof(BasePlatform)).Remove(this);
        }
    }
}
