using ChaosGame;
using ChaosGraphics;
using ChaosMath;

namespace Unstable
{
    [DefaultInstancer(0, "platform/2/mesh.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/4/mesh.gmdl", "platform/material.mat")]
    [DefaultInstancer(0, "platform/8/mesh.gmdl", "platform/material.mat")]
    public class Platform : BasePlatform
    {
        static readonly int[] widths = new[] { 2, 4, 8 };
        static readonly int[] numFragments = new[] { 3, 5, 7 };


        float _xScale = 1;
        int _width = 2;
        int _instancerIndex = 0;
        public int width
        {
            get { return _width; }
            set
            {
                _width = value;
                for (_instancerIndex = 0; _instancerIndex < widths.Length - 1; _instancerIndex++)
                    if (widths[_instancerIndex + 1] > _width)
                        break;
                physics.state.baseTransform = Matrix.Scaling(_xScale = (float)width / widths[_instancerIndex], 1, 1);
            }
        }

        Text frontTxt;
        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            frontTxt = new Text(scene.game.fontManager, 1);
        }

        protected override bool InteractInternal(Player player)
        {
            if (player.physics.state.velocity.y <= 0)
            {
                if (Math.Abs(player.physics.state.position.x - position.x) < 0.5f * width
                    && player.physics.state.position.y < position.y
                    && player.physics.state.position.y + 0.1f - player.physics.state.velocity.y * 0.1f >= position.y)
                {
                    if (player.onPlatform <= 0)
                        scene.game.soundPool.PlaySound("player/land.wav", 1);

                    player.physics.state.position.y = position.y;
                    player.physics.state.velocity.y = 0;
                    player.onPlatform = 0.12f;
                    player.remainingJumps = Player.MAX_JUMPS;

                    return true;
                }
            }
            return false;
        }

        protected override void BreakInternal()
        {
            int baseIndex = 0;
            for (int i = 0; i < _instancerIndex; i++)
                baseIndex += numFragments[i];
            for (int i = 0; i < numFragments[_instancerIndex]; i++)
                CreateFragment(i + baseIndex);
        }

        public override void GiveMeInstances(InstancingAttribute[] instancers) =>
            instancers[_instancerIndex].informer.AddInstance(physics.state.GetTransform());

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            txt.transform = Matrix.Scaling(0.5f) * Matrix.RotationX(Math.PI_Half) * Matrix.Translation(0, 0.01f, 0) * physics.state.GetTransform();

            frontTxt.transform = Matrix.Scaling(0.2f) * Matrix.Translation(position + new Vector3(0, -0.125f, -0.492f));
            frontTxt.color = txt.color;
            frontTxt.channelMultipliers = txt.channelMultipliers;
            frontTxt.UpdateText(scene.game.defaultFont, txt.geometry.text, txt.geometry.align);
            scene.materialTexts.Add(frontTxt);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            frontTxt.Dispose();
        }
    }
}
