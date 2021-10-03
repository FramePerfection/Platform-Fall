using ChaosGraphics;
using ChaosMath;

namespace Unstable
{
    [DefaultInstancer(0, "platform/2/mesh.gmdl", "wall/material.mat")]
    [DefaultInstancer(0, "platform/4/mesh.gmdl", "wall/material.mat")]
    [DefaultInstancer(0, "platform/8/mesh.gmdl", "wall/material.mat")]
    public class Wall : BasePlatform
    {
        //float _width = 1.0f;
        //public float width { get { return _width; } set { physics.state.baseTransform = Matrix.Scaling(_width = value, 1, 1); } }

        const float width = 0.125f;
        static readonly Matrix defaultBaseTransform = Matrix.Translation(0, width, 0) * Matrix.RotationZ(Math.PI_Half);

        Text leftSideTex, rightSideText;

        int _height = 2;
        int _instancerIndex = 0;
        public int height
        {
            get { return _height; }
            set
            {
                _height = value;
                for (_instancerIndex = 0; _instancerIndex < widths.Length - 1; _instancerIndex++)
                    if (widths[_instancerIndex + 1] > _height)
                        break;
                physics.state.baseTransform = defaultBaseTransform * Matrix.Scaling(1, (float)_height / widths[_instancerIndex], 1);
            }
        }

        static readonly int[] widths = new[] { 2, 4, 8 };
        static readonly int[] numFragments = new[] { 3, 5, 7 };


        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            physics.state.baseTransform = defaultBaseTransform;
            leftSideTex = new Text(scene.game.fontManager, 1);
            rightSideText = new Text(scene.game.fontManager, 1);
        }

        protected override bool InteractInternal(Player player)
        {
            if (Math.Abs((player.physics.state.position.y + player.height / 2) - position.y) < 0.5f * _height + player.height / 2)
            {
                if (player.physics.state.position.x < position.x
                    && player.physics.state.position.x + player.width >= position.x - width && player.physics.state.velocity.x > 0)
                {
                    player.physics.state.position.x = position.x - player.width - width;
                    Stick(player);
                    player.wallhugDirection = Player.WallhugDirection.Left;
                    return true;
                }
                if (player.physics.state.position.x > position.x
                    && player.physics.state.position.x - player.width <= position.x + width && player.physics.state.velocity.x < 0)
                {
                    player.physics.state.position.x = position.x + player.width + width;
                    Stick(player);
                    player.wallhugDirection = Player.WallhugDirection.Right;
                    return true;
                }
            }
            return false;
        }

        void Stick(Player player)
        {
            player.physics.state.velocity.x = 0;
            player.wallhugTimer = 0.12f;
        }
        public override void GiveMeInstances(InstancingAttribute[] instancers) =>
            instancers[_instancerIndex].informer.AddInstance(physics.state.GetTransform());

        protected override void BreakInternal()
        {
            for (int i = 0; i < 3; i++)
                CreateFragment(i);
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            txt.transform = Matrix.Scaling(0.25f) * Matrix.Translation(position + new Vector3(0, 0, -0.492f));
            foreach (var t in new[] { leftSideTex, rightSideText })
            {
                t.channelMultipliers = txt.channelMultipliers;
                t.color = txt.color;
                t.UpdateText(scene.game.defaultFont, txt.geometry.text, txt.geometry.align, true);
                bool left = t == leftSideTex;
                t.transform = Matrix.Scaling(0.5f)
                             * Matrix.RotationY(left ? Math.PI_Half : -Math.PI_Half)
                             * Matrix.Translation(position + new Vector3(left ? -width : width, 0, 0));
                scene.materialTexts.Add(t);
            }
        }
    }
}
