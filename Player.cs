
using ChaosGame;
using ChaosGraphics;
using ChaosMath;
using Collections;
using ChaosInput;

namespace Unstable
{
    public class Player : Component<WorldScene>
    {
        public enum InputAction
        {
            Left,
            Right,
            Jump,
            Reset,
            LaunchRocket,
            DetonateRocket,
#if CHEAT
            CHEAT_reset,
#endif
        }

        public enum WallhugDirection
        {
            None, Left, Right
        }

        public static int MAX_JUMPS = 2;
        static float[] JUMP_VELOCITY = new float[] { 8.2f, 4.2f, 4 };
        static float WALL_STICK = 0.1f;
        static float WALLJUMP_SPEED = 9;

        static float MISSILE_SPEED = 12;
        static float MISSILE_COOLDOWN = 1;

        const float NEW_LEVEL_TIME = 2;
        const float SLOW_CAMERA_INTERPOLATE = 3;
        const float GENEROUS_THING = 0.7f;

        public LinkedList<InputAxis>[] inputs = new LinkedList<InputAxis>[ChaosEnum.GetValues<InputAction>().Length];

        public ChaosPhysics.Physical physics;
        public Vector3 center => physics.state.position + new Vector3(0, 0.8f, 0);
        public float collisionRadius = 0.7f;

        public float onPlatform = 0;
        public int remainingJumps = 0;
        int deaths = 0;
        float timer;
        float missileCooldown = 0;

        MeshContainer.Entry mesh;
        MaterialContainer.Entry material;
        AnimationContainer.Entry animation;
        Rig rig;

        Text retryText, deathCounter, timerText;

        public bool dead { get; private set; } = false;
        float newLevelTimer = 0;


        float runProgress = 0;
        float jumpInterpolate, wallhugInterpolate;
        public float wallStick = WALL_STICK, wallhugTimer = 0;
        public WallhugDirection wallhugDirection = WallhugDirection.None;
        WallhugDirection lastWallhugDirection = WallhugDirection.None;
        float runRotation;

        public float width = 0.48f;
        public float height = 1.8f;

        ChaosGraphics.Lights.PointLight l;

        float GetInput(InputAction action)
        {
            if (newLevelTimer > NEW_LEVEL_TIME * GENEROUS_THING)
                return 0;
            float result = 0;
            foreach (var axis in inputs[(int)action])
                result = Math.Max(result, axis.value);
            return result;
        }

        protected override void Create(CreateParameters cparams)
        {
            for (int i = 0; i < inputs.Length; i++)
                inputs[i] = new LinkedList<InputAxis>();
            foreach (var keyboard in scene.game.input.EnumerateDevices<Keyboard>())
            {
                inputs[(int)InputAction.Left].Add(keyboard[Keyboard.Keys.A]);
                inputs[(int)InputAction.Right].Add(keyboard[Keyboard.Keys.D]);
                inputs[(int)InputAction.Jump].Add(keyboard[Keyboard.Keys.W]);
                inputs[(int)InputAction.Reset].Add(keyboard[Keyboard.Keys.R]);
#if CHEAT
                inputs[(int)InputAction.CHEAT_reset].Add(keyboard[Keyboard.Keys.O]);
#endif
            }
            foreach (var m in scene.game.input.EnumerateDevices<Mouse>())
            {
                inputs[(int)InputAction.LaunchRocket].Add(m[Mouse.MouseParameters.leftButton]);
                inputs[(int)InputAction.DetonateRocket].Add(m[Mouse.MouseParameters.rightButton]);
            }

            scene.shader.lights.Add(l = new ChaosGraphics.Lights.PointLight(scene.shader, Vector3.Empty, 12, 1));

            mesh = scene.game.meshes.Load("player/player.agmdl", MeshLoadFlags.Animated);
            material = new ChaosGame.Containers.DataContainer<Material>.Entry(null, scene.game.graphics.defaultMaterial, false);
            animation = scene.game.animations.Load("player/player.anim");
            rig = Rig.FromArchive(scene.game.archive, "player/player.rig");

            physics = new ChaosPhysics.Physical(this);
            physics.isStatic = true;
            physics.AddShape(new ChaosPhysics.Shapes.CapsuleShape(new Vector3(0, 0.4f, 0), new Vector3(0, 1.4f, 0), 0.4f));
            scene.physics.Add(physics);

            retryText = new Text(scene.game.fontManager, 64);
            retryText.color = new Vector4(4, 0, 0, 4);
            retryText.UpdateText(scene.game.defaultFont, "Press R to retry", Align.Center, false, false, false);

            deathCounter = new Text(scene.game.fontManager, 64);
            deathCounter.color = new Vector4(2);

            timerText = new Text(scene.game.fontManager, 64);
            timerText.color = new Vector4(2);
        }


        public void Respawn()
        {
            newLevelTimer = NEW_LEVEL_TIME;
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            HandleInput();
            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(Move);
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision_DONTDOSHIT].Add(UpdateAnimations);

            scene.updateLayers[(int)WorldScene.UpdateLayers.UpdateCamera].Add(() =>
            {
                if (!scene.gameFinished)
                    timer += ftime;

                newLevelTimer = Math.Max(0, newLevelTimer - ftime);

                l.position = physics.state.position + new Vector3(0, 0.5f, -1f);
                Vector3 targetViewPos = scene.targetViewPosition;
                targetViewPos.x = Math.Clamp(-WorldScene.CAMERA_X_BOUNDS, WorldScene.CAMERA_X_BOUNDS, physics.state.position.x);
                targetViewPos.y = Math.Max(-0.2f * targetViewPos.z, physics.state.position.y + scene.targetViewPosition.y);
                var viewPos = scene.view.Position + (targetViewPos - scene.view.Position)
                                                    * Math.EaseIn(ftime * (newLevelTimer > 0 ? SLOW_CAMERA_INTERPOLATE : 10));
                var viewTargetPos = physics.state.position;
                viewTargetPos.y = Math.Max(-0.1f * targetViewPos.z, viewTargetPos.y);
                scene.viewTargetPosition += (viewTargetPos - scene.viewTargetPosition)
                                            * Math.EaseIn(ftime * (newLevelTimer > 0 ? SLOW_CAMERA_INTERPOLATE : 20));
                scene.view.Update(viewPos, scene.viewTargetPosition - viewPos, new Vector3(0, 1, 0));
            });
        }

        void HandleInput()
        {
            if (newLevelTimer <= NEW_LEVEL_TIME * GENEROUS_THING)
                scene.game.input.AddHandler(Game.InputLayer.All, e =>
                {
                    if (!dead)
                    {
                        if (e.type == InputEvent.EventType.Push && inputs[(int)InputAction.LaunchRocket].Contains(e.axis))
                        {
                            if (missileCooldown == 0)
                            {
                                scene.game.soundPool.PlaySound("rocketlaunch.wav", 1);
                                missileCooldown = MISSILE_COOLDOWN;
                                var rocket = scene.AddComponent<PlayerMissile>(CreateParameters.Create(this));
                                var dir = Vector3.Normalize(scene.reticle.cursor - center);
                                rocket.physics.state.velocity = dir * MISSILE_SPEED;
                                rocket.physics.state.position = center + dir * 0.666f;
                                rocket.lifetime = 1.2f;
                            }
                            return true;
                        }

                        if (e.type == InputEvent.EventType.Push && inputs[(int)InputAction.Jump].Contains(e.axis))
                        {
                            if (wallhugDirection == WallhugDirection.None)
                            {
                                if (onPlatform > 0)
                                    onPlatform = 0;
                                else
                                    remainingJumps--;
                                if (remainingJumps > 0)
                                {
                                    var idx = MAX_JUMPS - remainingJumps;
                                    physics.state.velocity.y = JUMP_VELOCITY[idx];
                                    scene.game.soundPool.PlaySound($"player/jump{idx}.wav", 1);
                                    return true;
                                }
                            }
                            else
                            {
                                physics.state.velocity.x = wallhugDirection == WallhugDirection.Left ? -WALLJUMP_SPEED : WALLJUMP_SPEED;
                                physics.state.velocity.y = JUMP_VELOCITY[0];
                                lastWallhugDirection = wallhugDirection;
                                wallhugDirection = WallhugDirection.None;
                                remainingJumps = 2;
                                wallhugTimer = 0;
                                scene.game.soundPool.PlaySound($"player/jump0.wav", 1);
                            }
                        }
                    }
                    if (e.type == InputEvent.EventType.Push && inputs[(int)InputAction.Reset].Contains(e.axis))
                    {
                        if (scene.gameFinished)
                        {
                            scene.ResetGame();
                            return true;
                        }
                        if (dead)
                        {
                            physics.state.position = physics.state.velocity = Vector3.Empty;
                            scene.LoadLevel(scene.currentLevel);
                            dead = false;
                            return true;
                        }
                    }
#if CHEAT
                    if (e.type == InputEvent.EventType.Push && inputs[(int)InputAction.CHEAT_reset].Contains(e.axis))
                    {
                        physics.state.position = physics.state.velocity = Vector3.Empty;
                        scene.LoadLevel("testlevel");
                        return true;
                    }
#endif
                    return false;
                });
        }

        void Move()
        {
            if (dead)
                return;
            physics.state.position += physics.state.velocity * ftime;
            physics.state.velocity.y -= ftime * 9.81f;

            if (wallhugDirection == WallhugDirection.None)
            {
                if (onPlatform > 0)
                {
                    physics.state.velocity *= (1 - Math.EaseIn(ftime * 30));
                    physics.state.velocity.x += (GetInput(InputAction.Right) - GetInput(InputAction.Left)) * ftime * 180;
                }
                else
                {
                    physics.state.velocity *= (1 - Math.EaseIn(ftime * 0.1f));
                    physics.state.velocity.x *= (1 - Math.EaseIn(ftime * 3f));
                    physics.state.velocity.x += (GetInput(InputAction.Right) - GetInput(InputAction.Left)) * ftime * 35;

                    float jumpFrictionFactor = 1 - GetInput(InputAction.Jump);
                    if (physics.state.velocity.y > 0)
                        physics.state.velocity.y *= (1 - Math.EaseIn(ftime * 6 * jumpFrictionFactor * Math.EaseIn(physics.state.velocity.y)));
                }
            }
            else
            {
                var wallUnstick = wallhugDirection == WallhugDirection.Left ? GetInput(InputAction.Left) : GetInput(InputAction.Right);
                lastWallhugDirection = wallhugDirection;
                wallStick = Math.Max(0, wallStick - wallUnstick * ftime);
                wallhugTimer = Math.Max(0, wallhugTimer - ftime);
                if (wallStick == 0 || wallhugTimer == 0)
                {
                    physics.state.velocity.x = wallhugDirection == WallhugDirection.Left ? -1.5f : 1.5f;
                    wallhugDirection = WallhugDirection.None;
                    wallStick = WALL_STICK;
                }
                else
                {
                    physics.state.velocity.y *= (1 - Math.EaseIn(ftime * 3));
                    physics.state.velocity.x = wallhugDirection == WallhugDirection.Left ? 0.05f : -0.05f;
                }
            }
            onPlatform = Math.Max(0, onPlatform - ftime);
            runRotation += (Math.ATan(-physics.state.velocity.x * 0.15f) - runRotation) * Math.EaseIn(ftime * 10);
            missileCooldown = Math.Max(0, missileCooldown - ftime);

            if (Math.Abs(physics.state.position.x) > WorldScene.LEVEL_BOUNDS
                || (physics.state.position.y < 0 && !scene.levelComplete))
                Die();

            var platforms = scene.GetSceneData<LinkedList<BasePlatform>>(typeof(BasePlatform));
            foreach (var p in platforms)
                p.Interact(this);
        }

        public void Die()
        {
            if (!dead)
            {
                if (!scene.gameFinished)
                    deaths++;
                scene.game.soundPool.PlaySound("explosion.wav", 1);
                scene.game.soundPool.PlaySound("player/die.wav", 1);
                dead = true;
                int num = Random.RndInt(30, 35);
                for (int i = 0; i < num; i++)
                {
                    scene.simpleParticles.particles.Add(new ExplosionParticle(
                        ftime,
                        physics.state.position,
                        Random.RndVector3(Random.Rnd(5)),
                        Random.Rnd(0.5f, 1.0f)
                        ));
                }
                for (int i = 0; i < 20; i++)
                {
                    scene.simpleParticles.particles.Add(new SmokeParticle(
                        ftime,
                        physics.state.position,
                        Random.RndVector3(Random.Rnd(3)),
                        Random.Rnd(1.5f, 2),
                        1.2f,
                        2
                        ));
                }
            }
        }

        void UpdateAnimations()
        {
            var oldRunProgress = runProgress % 0.5f;
            runProgress += Math.Abs(physics.state.velocity.x) * ftime * 0.5f;
            if (!dead && onPlatform > 0 && oldRunProgress > runProgress % 0.5f)
                scene.game.soundPool.PlaySound("player/land.wav", 0.3f);

            rig.ClearAnimations();

            if (wallhugDirection == WallhugDirection.None)
                wallhugInterpolate = Math.Max(wallhugInterpolate - ftime * 4, 0);
            else
                wallhugInterpolate = Math.Min(wallhugInterpolate + ftime * 4, 1);

            float normalActionWeight = 1 - wallhugInterpolate;
            float groundActionWeight = (1 - jumpInterpolate) * normalActionWeight;

            if (onPlatform > 0)
                jumpInterpolate = Math.Max(jumpInterpolate - ftime * 4, 0);
            else
                jumpInterpolate = Math.Min(jumpInterpolate + ftime * 4, 1);

            if (wallhugInterpolate < 1)
            {
                if (jumpInterpolate < 1)
                {
                    var runAnim = animation.content["Run"];
                    runAnim.SetAnimationData(rig, runProgress % 1, Math.EaseIn(Math.Abs(physics.state.velocity.x) * 4) * groundActionWeight);
                    animation.content["Idle"].SetAnimationData(rig, ftime.totalTime, groundActionWeight);
                }
                if (jumpInterpolate > 0)
                    animation.content["Jump"].SetAnimationData(rig, 0, jumpInterpolate * normalActionWeight);
            }
            if (wallhugInterpolate > 0)
                animation.content[lastWallhugDirection == WallhugDirection.Left ? "HugWallLeft" : "HugWallRight"]
                .SetAnimationData(rig, 0, wallhugInterpolate);
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)WorldScene.DrawLayers.World].Add(() => Draw("World"));
            scene.drawLayers[(int)WorldScene.DrawLayers.Material].Add(() => Draw("Material"));
            var ja = Matrix.Scaling(0.075f)
                * scene.view.billBoard;
            deathCounter.transform = ja * Matrix.Translation(scene.view.Position + scene.view.Direction - scene.view.GetLocalY() * 0.85f - scene.view.GetLocalX());
            deathCounter.UpdateText(scene.game.defaultFont, $"Deaths: {deaths}", Align.Left, false, false, false);
            scene.hudTexts.Add(deathCounter);

            timerText.transform = ja * Matrix.Translation(scene.view.Position + scene.view.Direction - scene.view.GetLocalY() * 0.85f);
            timerText.UpdateText(scene.game.defaultFont, $"Time: {System.TimeSpan.FromSeconds(timer).ToString("mm':'ss")}", Align.Left, false, false, false);
            scene.hudTexts.Add(timerText);

            if (dead)
            {
                retryText.transform = Matrix.Scaling(0.2f) * scene.view.billBoard * Matrix.Translation(scene.view.Position + scene.view.Direction);
                scene.hudTexts.Add(retryText);
            }
        }

        void Draw(string pass)
        {
            if (dead)
                return;
            var shader = scene.game.graphics.defaultEffects.SkinnedNormalMap;
            scene.view.SetValues(shader, Matrix.RotationY(runRotation) * Matrix.Translation(physics.state.position));
            material.content.SetValues(shader);
            rig.SetData(shader, (AnimatedMesh)mesh);
            mesh.content.Draw(shader, pass);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            scene.physics.Remove(physics);
        }
    }
}
