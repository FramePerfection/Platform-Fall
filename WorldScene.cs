using System;
using ChaosGame;
using ChaosGraphics;
using ChaosMath;
using Collections;
using OpenTK.Graphics.OpenGL;
using SysCol = System.Collections.Generic;

namespace Unstable
{
    public interface IPerLevelData { }

    public class WorldScene : Scene<Game>
    {
        public enum DrawLayers
        {
            ResetInstancers,
            FillInstancers,
            World,
            Material,
            BackgroundWorld,
            BackgroundMaterial,
            LiquidWorld,
            LiquidMaterial,
            Transparents,
            Postprocessing,
            HUDTexts,
        }

        public enum UpdateLayers
        {
            Input,
            Ballern,
            Move,
            PrepareCollision,
            Collision,
            PostCollision_Manipulate,
            PostCollision_DONTDOSHIT,
            UpdateCamera,
            UpdateReticle,
            CreateParticles,
            Particles,
            Overlay,
        }

        protected ChaosGraphics.PostProcessors.AntiAliasing antiAliasing = new ChaosGraphics.PostProcessors.AntiAliasing();
        public TransparencyRenderer transparencyRenderer;
        public SimpleParticles simpleParticles;

        public LinkedList<Text> hudTexts, worldTexts, materialTexts;
        LinkedList<Tuple<DrawLayers, string, LinkedList<Text>>> textLists = new LinkedList<Tuple<DrawLayers, string, LinkedList<Text>>>();

        public Camera view { get; private set; }
        public DeferredShader shader;

        public InstancingManagerContainer<WorldScene> instancers;
        public Player player;
        public Vector3 targetViewPosition;
        public Vector3 viewTargetPosition;
        public readonly ChaosPhysics.PhysicsWorld physics;
        Material backgroundMaterial;
        Instancer backgroundInstancer;
        LaserBarrier groundLaser;

        public bool gameFinished => currentLevel == "end";

        public Reticle reticle;

        int levelIndex = 0;
        static int DEBUG_LEVEL_INDEX = 9;

        public float wallDepth = 1;
        public const float LEVEL_BOUNDS = 30;
        public const float CAMERA_X_BOUNDS = LEVEL_BOUNDS - 5;
        public const float BACKGROUND_TILE_SIZE = (CAMERA_X_BOUNDS + 10) * 2;

        public string currentLevel { get; private set; }
        public bool levelComplete { get; private set; } = false;

        public SysCol.Dictionary<object, object> perSceneData = new SysCol.Dictionary<object, object>();

        public T GetSceneData<T>(object key) where T : class
        {
            object result;
            if (!perSceneData.TryGetValue(key, out result))
                return null;
            return result as T;
        }
        public T GetOrAssignSceneData<T>(object key, Func<T> generateValue) where T : class
        {
            object result;
            if (!perSceneData.TryGetValue(key, out result))
                perSceneData[key] = result = generateValue();
            return (T)result;
        }

        public WorldScene(Game game, int width = -1, int height = -1) : base(game, typeof(UpdateLayers), typeof(DrawLayers))
        {
            textLists.Add(new Tuple<DrawLayers, string, LinkedList<Text>>(DrawLayers.World, "World", worldTexts = new LinkedList<Text>()));
            textLists.Add(new Tuple<DrawLayers, string, LinkedList<Text>>(DrawLayers.Material, "Material", materialTexts = new LinkedList<Text>()));
            textLists.Add(new Tuple<DrawLayers, string, LinkedList<Text>>(DrawLayers.HUDTexts, "HUD", hudTexts = new LinkedList<Text>()));
            view = new Camera(game.graphics);
            game.graphics.ratioChanged.Add(view.Update);
            shader = new DeferredShader(game.graphics, view, game.window.Size);
            backgroundMaterial = game.materials.Load("background/material.mat");

            transparencyRenderer = new TransparencyRenderer(
                game.graphics,
                ChaosMath.Math.Max(1, (int)(shader.width * Confix.transparencyRendererScale)),
                ChaosMath.Math.Max(1, (int)(shader.height * Confix.transparencyRendererScale)),
                shader.depthBuffer,
                (int)Confix.numTransparencyLayers);

            transparencyRenderer.transparents.Add(simpleParticles = AddComponent<SimpleParticles>());

            instancers = new InstancingManagerContainer<WorldScene>(this);
            instancers.CreateInstancers(this.game.graphics, AssemblyManager.gameAssembly);

            physics = AddComponent<ChaosPhysics.PhysicsWorld>(Component.CreateParameters.Create((int)UpdateLayers.Collision));
            physics.AddForce(new ChaosPhysics.Forces.LinearForce(0, -9.81f, 0));

            reticle = AddComponent<Reticle>();
            backgroundInstancer = new Instancer(game.graphics, new string[0], 0);

            for (int k = 0; k < 10; k++)
                for (int i = -1; i <= 1; i += 2)
                {
                    var beam = new LaserBeam(ftime, () => true, () => 1);
                    beam.start = new Vector3(i * LEVEL_BOUNDS, -1000, -k);
                    beam.end = new Vector3(i * LEVEL_BOUNDS, 1000, -k);
                    beam.thickness = 0.3f;
                    beam.color.a = 1.5f;
                    simpleParticles.particles.Add(beam);
                }

            ResetGame();
        }

        public void ResetGame()
        {
#if DEBUG
            levelIndex = DEBUG_LEVEL_INDEX;
#else
            levelIndex = 0;
#endif
            player?.Dispose();

            player = AddComponent<Player>();
            currentLevel = Levels.levelNames[levelIndex];
            LoadLevel(currentLevel);
        }

        public void LoadLevel(string name)
        {
            if (groundLaser == null || groundLaser.disabled)
                groundLaser = AddComponent<LaserBarrier>();
            levelComplete = false;
            currentLevel = name;

            foreach (var perLevelData in EnumerateChildren<Component<WorldScene>>(true))
                if (perLevelData is IPerLevelData)
                    perLevelData.Dispose();

            var rig = Rig.FromArchive(game.archive, $"levels/{name}.rig");
            foreach (var b in rig.root.EnumerateBones())
            {
                var bParams = b.name.Split('.');
                switch (bParams[0])
                {
                    case "Platform":
                        {
                            int stability = 1;
                            if (bParams.Length > 1 && bParams[1].StartsWith("s"))
                                int.TryParse(bParams[1].Substring(1), out stability);

                            int width = 2;
                            if (bParams.Length > 2 && bParams[2].StartsWith("w"))
                                int.TryParse(bParams[2].Substring(1), out width);
                            var p = AddComponent<Platform>();
                            p.position = b.GetPosition();
                            p.width = width;

                            if (stability == 0)
                            {
                                p.permanent = true;
                                p.stability = 1;
                            }
                            else
                                p.stability = stability;

                            break;
                        }
                    case "Wall":
                        {
                            int stability = 1;
                            if (bParams.Length > 1 && bParams[1].StartsWith("s"))
                                int.TryParse(bParams[1].Substring(1), out stability);

                            int height = 2;
                            if (bParams.Length > 2 && bParams[2].StartsWith("h"))
                                int.TryParse(bParams[2].Substring(1), out height);
                            var p = AddComponent<Wall>();
                            p.position = b.GetPosition();
                            p.height = height;
                            p.stability = stability;
                            break;
                        }
                    case "Spawn":
                        player.physics.state.position = b.GetPosition();
                        break;
                    case "Camera":
                        targetViewPosition = b.GetPosition();
                        break;
                    case "Text":
                        {
                            int textID = 0;
                            if (bParams.Length > 1 && bParams[1].StartsWith("id"))
                                int.TryParse(bParams[1].Substring(2), out textID);
                            if (textID < Levels.labelTexts.Length)
                                AddComponent<Label>(Component.CreateParameters.Create(Levels.labelTexts[textID], b.GetPosition(), b.length));
                        }
                        break;
                    case "Turret":
                        {
                            string type = "homing";
                            if (bParams.Length > 1)
                                type = bParams[1];

                            int interval = 10;
                            if (bParams.Length > 2 && bParams[2].StartsWith("i"))
                                int.TryParse(bParams[2].Substring(1), out interval);

                            int initial = 10;
                            if (bParams.Length > 3 && bParams[3].StartsWith("o"))
                                int.TryParse(bParams[3].Substring(1), out initial);
                            var turret = AddComponent<Turret>();
                            turret.interval = interval / 10.0f;
                            turret.timer = initial / 10.0f;
                            turret.position = b.GetPosition();

                            switch (type)
                            {
                                case "straight":

                                    var _direction = b.GetDirection() * b.length * 10.0f;
                                    turret.createMissile = () =>
                                    {
                                        var missile = AddComponent<StraightMissile>();
                                        missile.physics.state.position = turret.spawn;
                                        missile.physics.state.velocity.xy = _direction.xy;
                                        return missile;
                                    };
                                    turret.lightColor = Turret.GetLightColor(SimpleParticles.PARTICLE_INDEX_EXPLOSION);
                                    break;
                                case "homing":
                                    turret.createMissile = () =>
                                    {
                                        var missile = AddComponent<HomingMissile>();
                                        missile.physics.state.position = turret.spawn;
                                        return missile;
                                    };
                                    turret.lightColor = Turret.GetLightColor(SimpleParticles.PARTICLE_INDEX_EXPLOSION_PINK);
                                    break;
                                default:
                                    turret.createMissile = () =>
                                    {
                                        var missile = AddComponent<HomingMissile>();
                                        missile.physics.state.position = turret.spawn;
                                        missile.lifetime = 0;
                                        return missile;
                                    };
                                    break;
                            }
                        }
                        break;
                }
            }
            player.Respawn();
        }

        void AdvanceLevel()
        {

            var off = new Vector3(0, BACKGROUND_TILE_SIZE, 0);
            view.Update(view.Position + off, viewTargetPosition += off, view.Up);
            levelIndex++;
            if (levelIndex < Levels.levelNames.Length)
                currentLevel = Levels.levelNames[levelIndex];
            LoadLevel(currentLevel);
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            updateLayers[(int)UpdateLayers.PostCollision_DONTDOSHIT].Add(() =>
            {
                if (levelComplete && !view.CheckSphereIsVisible(player.physics.state.position, 2) && !groundLaser.alive)
                {
                    AdvanceLevel();
                }
                var platforms = GetSceneData<LinkedList<BasePlatform>>(typeof(BasePlatform));
                if (!levelComplete && platforms != null && platforms.Empty)
                {
                    levelComplete = true;
                    groundLaser?.Disable();
                    game.soundPool.PlaySound("levelcomplete.wav", 1);
                }
            });
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            instancers.SetDrawCalls(drawLayers[(int)DrawLayers.ResetInstancers], drawLayers[(int)DrawLayers.FillInstancers]);
            for (int i = 0; i < drawLayers.Length; i++)
                drawLayers[i].Add(() => Graphics.ThrowErrors());
            drawLayers[(int)DrawLayers.ResetInstancers].Add(() =>
            {
                backgroundInstancer.Reset();
                for (int i = -2; i < 20; i++)
                {
                    backgroundInstancer.AddInstance(
                        Matrix.Scaling(BACKGROUND_TILE_SIZE)
                        * Matrix.Translation(0, -BACKGROUND_TILE_SIZE * i, wallDepth)
                        );
                    backgroundInstancer.AddInstance(Matrix.Scaling(BACKGROUND_TILE_SIZE)
                        * Matrix.RotationY(-ChaosMath.Math.PI_Half)
                        * Matrix.Translation(-BACKGROUND_TILE_SIZE * 0.5f, -BACKGROUND_TILE_SIZE * i, 0)
                        );
                    backgroundInstancer.AddInstance(Matrix.Scaling(BACKGROUND_TILE_SIZE)
                        * Matrix.RotationY(ChaosMath.Math.PI_Half)
                        * Matrix.Translation(BACKGROUND_TILE_SIZE * 0.5f, -BACKGROUND_TILE_SIZE * i, 0)
                        );

                }
            });
            drawLayers[(int)DrawLayers.BackgroundWorld].Add(() => DrawBackground("World"));
            drawLayers[(int)DrawLayers.BackgroundMaterial].Add(() => DrawBackground("Material"));
            Graphics.ThrowErrors();
        }

        void DrawBackground(string pass)
        {
            var bgShader = game.graphics.defaultEffects.InstancedNormalMap;
            view.SetValues(bgShader, Matrix.Identity);
            backgroundMaterial.SetValues(bgShader);
            Sprite.DrawSpriteMeshInstanced(game.graphics, bgShader, backgroundInstancer, pass);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            shader.Dispose();
            game.graphics.ratioChanged.Remove(view.Update);
            transparencyRenderer.Dispose();
            instancers.Dispose();
        }

        protected void DrawCallSetup()
        {
            MeasurementLog.StartMeasure("Set Draw Calls");
            foreach (var textList in textLists)
                textList.Item3.Clear();
            foreach (LinkedList<Action> draws in drawLayers)
                draws.Clear();
            SetDrawCalls();

            foreach (Component child in components)
                child.SetDrawCalls();

            foreach (var textList in textLists)
                drawLayers[(int)textList.Item1].Add(() =>
                {
                    view.SetValues(game.graphics.defaultEffects.ManagedText, Matrix.Identity);
                    game.fontManager.DrawTexts(game.graphics.defaultEffects.ManagedText, textList.Item2, textList.Item3);
                });
            MeasurementLog.EndMeasure();
        }

        public override void Draw()
        {
            DrawCallSetup();


            PerformDrawLayer(DrawLayers.ResetInstancers);
            PerformDrawLayer(DrawLayers.FillInstancers);
            var old = game.graphics.managed.GetFramebuffer(FramebufferTarget.Framebuffer);

            //view.Update(view.Position, view.Direction, view.Up, .9f, background.maxZ - view.Position.z + 1);

            shader.BeginWorld();
            PerformDrawLayer(DrawLayers.World);
            PerformDrawLayer(DrawLayers.BackgroundWorld);

            shader.BeginMaterial();
            PerformDrawLayer(DrawLayers.Material);
            PerformDrawLayer(DrawLayers.BackgroundMaterial);

            //liquidShader.BeginWorld(false);
            //PerformDrawLayer(DrawLayers.LiquidWorld);
            //liquidShader.BeginMaterial();
            //PerformDrawLayer(DrawLayers.LiquidMaterial);

            //game.graphics.managed.BindFramebuffer(FramebufferTarget.Framebuffer, liquidShader.resultFBO);
            //GL.ClearColor(0, 0, 0, 0);
            //GL.Clear(ClearBufferMask.ColorBufferBit);
            //liquidShader.RenderTested(0.1f);
            shader.Render(shader.resultFBO);

            //game.graphics.managed.BindFramebuffer(FramebufferTarget.Framebuffer, shader.resultFBO);
            //view.SetValues(splatterEffect, Matrix.Identity);
            //splatterEffect.SetValue("blendSampler", liquidShader.GBuffers[(int)DeferredShader.GBUFFERS.Emissive]);
            //splatterEffect.SetValue("positionSampler", liquidShader.GBuffers[(int)DeferredShader.GBUFFERS.Position]);
            //splatterEffect.SetValue("otherPositionSampler", shader.GBuffers[(int)DeferredShader.GBUFFERS.Position]);
            //splatterEffect.SetValue("diffuseSampler", liquidShader.renderResult);
            //splatterEffect.BeginPass("BlendRenderResult");
            //Sprite.DrawPositionTextured(game.graphics);
            //splatterEffect.EndPass();

            MeasurementLog.StartMeasure("Transparency");
            transparencyRenderer.Render(shader);
            MeasurementLog.EndMeasure();

            game.graphics.managed.BindFramebuffer(FramebufferTarget.Framebuffer, old);
            antiAliasing.Apply(shader);
            PerformDrawLayer(DrawLayers.Postprocessing);
            PerformDrawLayer(DrawLayers.HUDTexts);
        }
    }
}
