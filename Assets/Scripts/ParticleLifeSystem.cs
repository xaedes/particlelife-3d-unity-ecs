using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class ParticleLifeSystem : JobComponentSystem
{
    public ParticleLife particleLife;

    [BurstCompile]
    struct ParticleLifeSystemJob : IJobForEach<Translation, Particle>
    {
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Entity> entities;

        public float deltaTime;
        public float friction;

        public float3 lowerBound;
        public float3 upperBound;
        public bool wrapX;
        public bool wrapY;
        public bool wrapZ;
        public float bounce;

        //[ReadOnly] public ComponentDataFromEntity<Translation> translations;
        //[ReadOnly] public ComponentDataFromEntity<Particle> particles;

        public void Execute(ref Translation translation, ref Particle particle)
        {
            
            #region movement
            translation.Value = translation.Value + particle.vel * deltaTime;
            #endregion
            #region friction
            particle.vel = particle.vel * friction;
            #endregion

            #region check universe bounds
            float3 dim = upperBound - lowerBound;
            #region x dimension
            if (wrapX && dim.x > 0) 
            {
                while (translation.Value.x < lowerBound.x) translation.Value.x += dim.x;
                while (translation.Value.x > upperBound.x) translation.Value.x -= dim.x;
            }
            else
            {
                if (translation.Value.x < lowerBound.x)
                {
                    translation.Value.x = lowerBound.x;
                    particle.vel.x *= -bounce;
                }
                else if (translation.Value.x > upperBound.x)
                {
                    translation.Value.x = upperBound.x;
                    particle.vel.x *= -bounce;
                }
            }
            #endregion
            #region y dimension
            if (wrapY && dim.y > 0)
            {
                while (translation.Value.y < lowerBound.y) translation.Value.y += dim.y;
                while (translation.Value.y > upperBound.y) translation.Value.y -= dim.y;
            }
            else
            {
                if (translation.Value.y < lowerBound.y)
                {
                    translation.Value.y = lowerBound.y;
                    particle.vel.y *= -bounce;
                }
                else if (translation.Value.y > upperBound.y)
                {
                    translation.Value.y = upperBound.y;
                    particle.vel.y *= -bounce;
                }
            }
            #endregion
            #region z dimension
            if (wrapZ && dim.z > 0)
            {
                while (translation.Value.z < lowerBound.z) translation.Value.z += dim.z;
                while (translation.Value.z > upperBound.z) translation.Value.z -= dim.z;
            }
            else
            {
                if (translation.Value.z < lowerBound.z)
                {
                    translation.Value.z = lowerBound.z;
                    particle.vel.z *= -bounce;
                }
                else if (translation.Value.z > upperBound.z)
                {
                    translation.Value.z = upperBound.z;
                    particle.vel.z *= -bounce;
                }
            }
            #endregion
            #endregion
        }
    }
    
    [BurstCompile]
    struct ParticleLifeSystemJobFor : IJobParallelFor
    {
        //[DeallocateOnJobCompletion, ReadOnly]
        //public NativeArray<Entity> entities;

        public float deltaTime;
        public float friction;

        public float3 lowerBound;
        public float3 upperBound;
        public bool wrapX;
        public bool wrapY;
        public bool wrapZ;
        public float bounce;

        //public ComponentDataFromEntity<Translation> translations;
        //public ComponentDataFromEntity<Particle> particles;
        public NativeArray<Translation> translations;
        public NativeArray<Particle> particles;

        //[DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;
        //public ArchetypeChunkComponentType<Translation> TranslationType;
        //public ArchetypeChunkComponentType<Particle> ParticleType;

        public void Execute(int index)
        {
            //var chunk = Chunks[chunkIndex];
            //var chunkTranslation = chunk.GetNativeArray(TranslationType);
            //var chunkParticle = chunk.GetNativeArray(ParticleType);
            //var instanceCount = chunk.Count;

            Translation translation = translations[index];
            Particle particle = particles[index];



            #region movement
            translation.Value = translation.Value + particle.vel * deltaTime;
            #endregion
            #region friction
            particle.vel = particle.vel * friction;
            #endregion

            #region check universe bounds
            float3 dim = upperBound - lowerBound;
            #region x dimension
            if (wrapX && dim.x > 0) 
            {
                while (translation.Value.x < lowerBound.x) translation.Value.x += dim.x;
                while (translation.Value.x > upperBound.x) translation.Value.x -= dim.x;
            }
            else
            {
                if (translation.Value.x < lowerBound.x)
                {
                    translation.Value.x = lowerBound.x;
                    particle.vel.x *= -bounce;
                }
                else if (translation.Value.x > upperBound.x)
                {
                    translation.Value.x = upperBound.x;
                    particle.vel.x *= -bounce;
                }
            }
            #endregion
            #region y dimension
            if (wrapY && dim.y > 0)
            {
                while (translation.Value.y < lowerBound.y) translation.Value.y += dim.y;
                while (translation.Value.y > upperBound.y) translation.Value.y -= dim.y;
            }
            else
            {
                if (translation.Value.y < lowerBound.y)
                {
                    translation.Value.y = lowerBound.y;
                    particle.vel.y *= -bounce;
                }
                else if (translation.Value.y > upperBound.y)
                {
                    translation.Value.y = upperBound.y;
                    particle.vel.y *= -bounce;
                }
            }
            #endregion
            #region z dimension
            if (wrapZ && dim.z > 0)
            {
                while (translation.Value.z < lowerBound.z) translation.Value.z += dim.z;
                while (translation.Value.z > upperBound.z) translation.Value.z -= dim.z;
            }
            else
            {
                if (translation.Value.z < lowerBound.z)
                {
                    translation.Value.z = lowerBound.z;
                    particle.vel.z *= -bounce;
                }
                else if (translation.Value.z > upperBound.z)
                {
                    translation.Value.z = upperBound.z;
                    particle.vel.z *= -bounce;
                }
            }
            #endregion
            #endregion
            translations[index] = translation;
            particles[index] = particle;
        }
    }

    EntityQuery m_query;

    protected override void OnCreate()
    {
        var queryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Translation), ComponentType.ReadOnly<Particle>() }
        };
        m_query = GetEntityQuery(queryDesc);
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new ParticleLifeSystemJobFor();

        //var TranslationType = GetArchetypeChunkComponentType<Translation>();
        //var ParticleType = GetArchetypeChunkComponentType<Particle>(true);
        //var chunks = m_query.CreateArchetypeChunkArray(Allocator.TempJob);
        //m_query.

        //var entities = new NativeArray<Entity>(particleLife.entities.Length, Allocator.TempJob);
        //entities.CopyFrom(particleLife.entities);
        //job.entities = entities;
        job.deltaTime = UnityEngine.Time.deltaTime;
        job.friction = particleLife.friction;
        job.lowerBound = particleLife.lowerBound;
        job.upperBound = particleLife.upperBound;
        job.wrapX = particleLife.wrapX;
        job.wrapY = particleLife.wrapY;
        job.wrapZ = particleLife.wrapZ;
        job.bounce = particleLife.bounce;
        job.translations = m_query.ToComponentDataArray<Translation>(Allocator.TempJob);
        job.particles = m_query.ToComponentDataArray<Particle>(Allocator.TempJob);
        //job.translations = GetComponentDataFromEntity<Translation>(false);
        //job.particles = GetComponentDataFromEntity<Particle>(false);

        // Now that the job is set up, schedule it to be run. 
        var jobHandle = job.Schedule(particleLife.entities.Length, 16, inputDependencies);
        jobHandle.Complete();
        m_query.CopyFromComponentDataArray<Translation>(job.translations);
        m_query.CopyFromComponentDataArray<Particle>(job.particles);
        job.translations.Dispose();
        job.particles.Dispose();
        return inputDependencies;
    }
}