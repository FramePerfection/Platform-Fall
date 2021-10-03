using ChaosGraphics;
using ChaosGame;
using ChaosMath;
using ChaosPhysics;
using Collections;

namespace Unstable
{
    [DefaultInstancer(0, "platform/2/frag0.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/2/frag1.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/2/frag2.gmdl", "platform/material.mat")]

    [DefaultInstancer(0, "platform/4/frag0.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/4/frag1.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/4/frag2.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/4/frag3.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/4/frag4.gmdl", "platform/material.mat")]

    [DefaultInstancer(0, "platform/8/frag0.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/8/frag1.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/8/frag2.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/8/frag3.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/8/frag4.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/8/frag5.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/8/frag6.gmdl", "platform/material.mat")]

    class PlatformFragment : Component<WorldScene>, Instancable
    {
        float time;
        public int segment;
        public Physical physics;
        public Vector3 baseOffset { get; private set; }
        protected override void Create(CreateParameters cparams)
        {
            var intParam = (CParams<int>)cparams;
            segment = intParam.v1;

            physics = new Physical(this);
            scene.physics.Add(physics);
            var newState = new RealPhysicsState();
            newState.velocity = Random.RndVector3(Random.Rnd(5));
            newState.angularVelocity = Random.RndVector3(Random.Rnd(5));
            newState.stiffness = 1000;
            newState.velocity.z *= 0.4f;
            physics.state = newState;
            physics.getRelevantShapes = _ =>
            {
                var otherAsShit = _.creator as PlatformFragment;
                if (otherAsShit != null && otherAsShit.time < 0.7f)
                    return null;
                return physics.shapes;
            };

            InstancingManager<WorldScene> myInstancer;
            if (scene.instancers.TryGetValue(GetType(), out myInstancer))
            {
                myInstancer.instancedThings.Add(this);
                var positions = ((DefaultInstancer)myInstancer.instancers[segment]).mesh.content.data.pos;
                var shape = new ChaosPhysics.Shapes.MeshShape(positions);
                shape.bounce = 0;
                baseOffset = (shape.boundingBoxLow + shape.boundingBoxHigh) * 0.5f;
                physics.state.baseTransform = Matrix.Translation(-baseOffset);
                physics.shapes.Add(shape);
            }
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(() =>
            {
                physics.state.Move(ftime);
                if ((time += ftime) > 5)
                    Dispose();
            });
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            scene.physics.Remove(physics);
            InstancingManager<WorldScene> myInstancer;
            if (scene.instancers.TryGetValue(GetType(), out myInstancer))
                myInstancer.instancedThings.Remove(this);
        }

        void Instancable.GiveMeInstances(InstancingAttribute[] instancers)
        {
            instancers[segment].informer.AddInstance(physics.state.GetTransform());
        }

        bool Instancable.NeedsInstancedDraw() => true;
    }
}
