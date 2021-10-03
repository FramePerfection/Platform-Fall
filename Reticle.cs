using ChaosGame;
using ChaosMath;
using ChaosInput;
using ChaosGraphics;

namespace Unstable
{
    public class Reticle : Component<WorldScene>
    {

        public Vector3 cursor { get; private set; }
        Vector2 viewCursor;
        MaterialContainer.Entry material;

        protected override void Create(CreateParameters cparams)
        {
            material = scene.game.materials.Load("reticle/material.mat");
        }

        float mouseSensitivity = 1 / 1200.0f;

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();

            scene.updateLayers[(int)WorldScene.UpdateLayers.UpdateReticle].Add(() =>
            {
                int numMice = 0;
                foreach (var m in scene.game.input.EnumerateDevices<Mouse>())
                {
                    numMice++;

                    viewCursor.x += Mouse.GetValue(scene.game.input, Mouse.MouseParameters.xPositive) * mouseSensitivity;
                    viewCursor.x -= Mouse.GetValue(scene.game.input, Mouse.MouseParameters.xNegative) * mouseSensitivity;
                    viewCursor.y -= Mouse.GetValue(scene.game.input, Mouse.MouseParameters.yPositive) * mouseSensitivity;
                    viewCursor.y += Mouse.GetValue(scene.game.input, Mouse.MouseParameters.yNegative) * mouseSensitivity;

                    float tan = scene.view.tan;
                    viewCursor.x = Math.Clamp(-scene.view.screenRatio * tan, scene.view.screenRatio * tan, viewCursor.x);
                    viewCursor.y = Math.Clamp(-tan, tan, viewCursor.y);

                    var lolz = scene.view.GetLocalX() * viewCursor.x + scene.view.GetLocalY() * viewCursor.y + scene.view.Direction;
                    var f = (-scene.view.Position.z) / lolz.z;
                    cursor = new Vector3((scene.view.Position + lolz * f).xy, 0);
                }
            });
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)WorldScene.DrawLayers.World].Add(() => Draw("World"));
            scene.drawLayers[(int)WorldScene.DrawLayers.Material].Add(() => Draw("Material"));
        }

        void Draw(string pass)
        {
            var shader = scene.game.graphics.defaultEffects.NormalMap;
            scene.view.SetValues(shader, Matrix.Scaling(0.5f) * Matrix.Translation(new Vector3(cursor.xy, -0.1f)));
            material.content.SetValues(shader);
            shader.SetValue("alphaThreshold", 0.5f);
            Sprite.DrawSpriteMesh(scene.game.graphics, shader, $"{pass}_AlphaTested");
        }
    }
}
