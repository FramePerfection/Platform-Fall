using ChaosGraphics;
using ChaosGame;
using ChaosMath;

namespace Unstable
{
    public class Label : Component<WorldScene>, IPerLevelData
    {
        public Text text;
        float scale;
        Vector3 position;
        protected override void Create(CreateParameters cparams)
        {
            var pxsa = (CParams<string, Vector3, float>)cparams;
            var str = pxsa.v1;
            position = pxsa.v2;
            scale = pxsa.v3;
            text = new Text(scene.game.fontManager, str.Length);
            text.UpdateText(scene.game.defaultFont, str, Align.Center, true, false, false);
            text.channelMultipliers = new Vector3(0.5f);
            text.transform = Matrix.Scaling(scale * 0.5f) * Matrix.Translation(position);
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.worldTexts.Add(text);
            scene.materialTexts.Add(text);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            text.Dispose();
        }
    }
}
