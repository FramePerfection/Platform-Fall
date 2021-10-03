using ChaosGraphics;
using ChaosGame;
using ChaosMath;

namespace Unstable
{
    public class ExplosionParticle : Particle
    {
        public Vector3 velocity;
        public float lifeTime;
        float initialLifeTime;
        float rotation = Random.Rnd(Math.PI_2);
        float rotationSpeed = Random.Rnd(-Math.PI_2, Math.PI_2);
        public new intVector2 particleIndex { get { return base.particleIndex; } set { base.particleIndex = value; } }

        public ExplosionParticle(Time ftime, Vector3 position, Vector3 speed, float lifeTime) : base(ftime)
        {
            this.position = position;
            this.velocity = speed;
            this.initialLifeTime = this.lifeTime = lifeTime;
            particleIndex = SimpleParticles.PARTICLE_INDEX_EXPLOSION;
        }

        public override bool Update()
        {
            position += velocity * ftime;
            velocity *= (1 - Math.EaseIn(ftime));
            rotation += rotationSpeed * ftime;
            rotationSpeed *= (1 - Math.EaseIn(ftime));
            return (lifeTime -= ftime) > 0;
        }
        protected override Vector4 GetColor() => new Vector4(1, 1, 1, lifeTime / initialLifeTime);
        protected override Matrix GetTransform(Camera view) => Matrix.RotationZ(rotation) * base.GetTransform(view);
        public override void SetInstanceData(Instancer instancer, Camera view) =>
            instancer.AddInstance(GetTransform(view), new Vector4(particleIndex.x, particleIndex.y, lifeTime / initialLifeTime, 0), GetColor());
    }

    public class SmokeParticle : Particle
    {
        public Vector3 velocity;
        public float lifeTime;
        float initialLifeTime;
        float rotation = Random.Rnd(Math.PI_2);
        float rotationSpeed = Random.Rnd(-Math.PI_2, Math.PI_2);
        float sizeIncrement;

        public SmokeParticle(Time ftime, Vector3 position, Vector3 speed, float lifeTime, float startSize, float sizeIncrement) : base(ftime)
        {
            this.position = position;
            this.velocity = speed;
            this.initialLifeTime = this.lifeTime = lifeTime;
            this.size = startSize;
            this.sizeIncrement = sizeIncrement;
            particleIndex = SimpleParticles.PARTICLE_INDEX_SMOKE;
        }

        public override bool Update()
        {
            position += velocity * ftime;
            velocity *= (1 - Math.EaseIn(ftime));
            rotation += rotationSpeed * ftime;
            rotationSpeed *= (1 - Math.EaseIn(ftime));
            size += ftime * sizeIncrement;
            return (lifeTime -= ftime) > 0;
        }
        protected override Vector4 GetColor() => new Vector4(new Vector3(0.5f + 0.5f * (lifeTime / initialLifeTime)), lifeTime / initialLifeTime);
        protected override Matrix GetTransform(Camera view) => Matrix.RotationZ(rotation) * base.GetTransform(view);
        public override void SetInstanceData(Instancer instancer, Camera view) =>
            instancer.AddInstance(GetTransform(view), new Vector4(particleIndex.x, particleIndex.y, (lifeTime / initialLifeTime) * 0.1f, 0), GetColor());
    }
}
