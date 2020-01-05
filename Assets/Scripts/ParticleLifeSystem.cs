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
    
    //[BurstCompile]
    struct ParticleLifeSystemJobFor : IJobParallelFor
    {
        //[DeallocateOnJobCompletion, ReadOnly]
        //public NativeArray<Entity> entities;

        public float deltaTime;
        public float friction;
        public float maxSpeed;

        public float3 lowerBound;
        public float3 upperBound;
        public float3 dim;
        public float3 halfDim;
        public bool wrapX;
        public bool wrapY;
        public bool wrapZ;
        public float bounce;
        public float eps;
        public float strength;
        public float radius;
        public float r_smooth;
        public bool flatForce;
        public int numTypes;
        [ReadOnly] public NativeArray<float> Attract;
        [ReadOnly] public NativeArray<float> RangeMin;
        [ReadOnly] public NativeArray<float> RangeMax;

        //public ComponentDataFromEntity<Translation> translations;
        //public ComponentDataFromEntity<Particle> particles;
        [ReadOnly] public NativeArray<Translation> oldTranslations;
        [ReadOnly] public NativeArray<Particle> oldParticles;
        public NativeArray<Translation> translations;
        public NativeArray<Particle> particles;

        //[DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;
        //public ArchetypeChunkComponentType<Translation> TranslationType;
        //public ArchetypeChunkComponentType<Particle> ParticleType;
        private int C(int x, int y) => y * numTypes + x;

        public void Execute(int index)
        {
            //var chunk = Chunks[chunkIndex];
            //var chunkTranslation = chunk.GetNativeArray(TranslationType);
            //var chunkParticle = chunk.GetNativeArray(ParticleType);
            //var instanceCount = chunk.Count;

            Translation translation = oldTranslations[index];
            Particle particle = oldParticles[index];

            if (numTypes > 0)
            { 
                int num = particles.Length;
                for (int k = 0; k < num; k++)
                {
                    Translation otherTranslation = oldTranslations[k];
                    Particle otherParticle = oldParticles[k];
                    float3 diff = otherTranslation.Value - translation.Value;
                    if (wrapX && dim.x > 0)
                    {
                        while (diff.x < -halfDim.x) diff.x += dim.x;
                        while (diff.x > +halfDim.x) diff.x -= dim.x;
                    }
                    if (wrapY && dim.y > 0)
                    {
                        while (diff.y < -halfDim.y) diff.y += dim.y;
                        while (diff.y > +halfDim.y) diff.y -= dim.y;
                    }
                    if (wrapZ && dim.z > 0)
                    {
                        while (diff.z < -halfDim.z) diff.z += dim.z;
                        while (diff.z > +halfDim.z) diff.z -= dim.z;
                    }
                    float r2 = diff.x * diff.x + diff.y * diff.y + diff.z * diff.z;
                    var typeA = particle.type % numTypes;
                    var typeB = otherParticle.type % numTypes;
                    var coord = C(typeA, typeB);
                    float r_max = RangeMax[coord];
                    if (r2 > r_max * r_max) continue;
                    float r = math.sqrt(r2);
                    if (r < eps) continue;
                    float3 direction = diff / r;
                    var r_min = RangeMin[coord];
                    float f;
                    if (r > r_min)
                    {
                        var attract = Attract[coord];
                        if (flatForce)
                        {
                            f = attract;
                        }
                        else
                        {
                            var rangeMid = (r_min + r_max) * 0.5f;
                            var numer = 2.0f * math.abs(r - rangeMid);
                            var denom = r_max - r_min;
                            if (denom > eps)
                            {
                                f = attract * (1.0f - numer / denom);
                            }
                            else
                            {
                                f = attract;
                            }
                        }
                    }
                    else
                    {
                        if ((r + r_smooth > eps) && (r_min + r_smooth > eps))
                        {
                            f = r_smooth * r_min * (1.0f / (r_min + r_smooth) - 1.0f / (r + r_smooth));
                        }
                        else
                        {
                            f = 0f;
                        }
                    }

                    particle.vel = particle.vel + direction * f * strength * deltaTime;
                    //otherParticle.vel = otherParticle.vel - direction * deltaTime * f;
                    //particles[k] = otherParticle;
                }
            }

            #region movement
            translation.Value = translation.Value + particle.vel * deltaTime;
            #endregion
            #region friction
            particle.vel = particle.vel * friction;
            #endregion

            #region check universe bounds
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

            #region max speed
            float speed = math.sqrt(particle.vel.x * particle.vel.x + particle.vel.y * particle.vel.y + particle.vel.z * particle.vel.z);
            if (speed > eps)
                if (speed > maxSpeed)
                {
                    particle.vel.x *= maxSpeed / speed;
                    particle.vel.y *= maxSpeed / speed;
                    particle.vel.z *= maxSpeed / speed;
                }
            #endregion
            translations[index] = translation;
            particles[index] = particle;
        }
    }

    EntityQuery m_query;

    public int numTypes
    {
        get { return m_newNumTypes; }
        set
        {
            m_newNumTypes = value;
            m_NumTypesChanged = true;
        }
    }
    public NativeArray<float> Attract
    {
        get { return m_newAttract; }
        set
        {
            m_newAttract = value;
            m_AttractChanged = true;
        }
    }
    public NativeArray<float> RangeMin
    {
        get { return m_newRangeMin; }
        set
        {
            m_newRangeMin = value;
            m_RangeMinChanged = true;
        }
    }
    public NativeArray<float> RangeMax
    {
        get { return m_newRangeMax; }
        set
        {
            m_newRangeMax = value;
            m_RangeMaxChanged = true;
        }
    }

    bool m_NumTypesChanged = false;
    bool m_AttractChanged = false;
    bool m_RangeMinChanged = false;
    bool m_RangeMaxChanged = false;
    int m_newNumTypes;
    NativeArray<float> m_newAttract;
    NativeArray<float> m_newRangeMin;
    NativeArray<float> m_newRangeMax;
    int m_copyOfNumTypes = 0;
    NativeArray<float> m_copyOfAttract = new NativeArray<float>(0, Allocator.Persistent);
    NativeArray<float> m_copyOfRangeMin = new NativeArray<float>(0, Allocator.Persistent);
    NativeArray<float> m_copyOfRangeMax = new NativeArray<float>(0, Allocator.Persistent);

    protected override void OnCreate()
    {
        var queryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Translation), typeof(Particle) }
        };
        m_query = GetEntityQuery(queryDesc);
    }
    protected override void OnDestroy()
    {
        m_copyOfAttract.Dispose();
        m_copyOfRangeMin.Dispose();
        m_copyOfRangeMax.Dispose();
    }
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        // assign new particle type info in sync with the jobs
        if (m_NumTypesChanged)
        {
            m_copyOfNumTypes = numTypes;
            m_NumTypesChanged = false;
        }
        if (m_AttractChanged && Attract.IsCreated)
        {
            if (m_copyOfAttract.IsCreated || m_copyOfAttract.Length > 0) m_copyOfAttract.Dispose();
            m_copyOfAttract = new NativeArray<float>(Attract, Allocator.Persistent);
            m_AttractChanged = false;
        }
        if (m_RangeMinChanged && RangeMin.IsCreated)
        {
            if (m_copyOfRangeMin.IsCreated || m_copyOfRangeMin.Length > 0) m_copyOfRangeMin.Dispose();
            m_copyOfRangeMin = new NativeArray<float>(RangeMin, Allocator.Persistent);
            m_RangeMinChanged = false;
        }
        if (m_RangeMaxChanged && RangeMax.IsCreated)
        {
            if (m_copyOfRangeMax.IsCreated || m_copyOfRangeMax.Length > 0) m_copyOfRangeMax.Dispose();
            m_copyOfRangeMax = new NativeArray<float>(RangeMax, Allocator.Persistent);
            m_RangeMaxChanged = false;
        }

        //// only do jobs when particle type info is existing and consistent
        //if ((!m_copyOfAttract.IsCreated || !m_copyOfRangeMin.IsCreated || !m_copyOfRangeMax.IsCreated)
        //    || (m_copyOfAttract.Length != m_copyOfNumTypes * m_copyOfNumTypes)
        //    || (m_copyOfRangeMin.Length != m_copyOfNumTypes * m_copyOfNumTypes)
        //    || (m_copyOfRangeMax.Length != m_copyOfNumTypes * m_copyOfNumTypes))
        //    return inputDependencies;

        var job = new ParticleLifeSystemJobFor();


        //var TranslationType = GetArchetypeChunkComponentType<Translation>();
        //var ParticleType = GetArchetypeChunkComponentType<Particle>(true);
        //var chunks = m_query.CreateArchetypeChunkArray(Allocator.TempJob);
        //m_query.

        //var entities = new NativeArray<Entity>(particleLife.entities.Length, Allocator.TempJob);
        //entities.CopyFrom(particleLife.entities);
        //job.entities = entities;
        job.eps = 1e-6f;
        if (abs(particleLife.minSimulationStepRate) > job.eps)
        {
            job.deltaTime = min(1.0f / abs(particleLife.minSimulationStepRate), UnityEngine.Time.deltaTime);
        }
        else
        {
            job.deltaTime = UnityEngine.Time.deltaTime;
        }
        job.radius = particleLife.radius;
        job.r_smooth = particleLife.r_smooth;
        job.flatForce = particleLife.flatForce;
        job.friction = particleLife.friction;
        job.strength = particleLife.interactionStrength;
        job.maxSpeed = particleLife.maxSpeed;

        job.numTypes = m_copyOfNumTypes;
        job.Attract = m_copyOfAttract;
        job.RangeMin = m_copyOfRangeMin;
        job.RangeMax = m_copyOfRangeMax;

        job.lowerBound = particleLife.lowerBound;
        job.upperBound = particleLife.upperBound;
        job.dim = job.upperBound - job.lowerBound;
        job.halfDim = job.dim * 0.5f;
        job.wrapX = particleLife.wrapX;
        job.wrapY = particleLife.wrapY;
        job.wrapZ = particleLife.wrapZ;
        job.bounce = particleLife.bounce;

        job.oldTranslations = m_query.ToComponentDataArray<Translation>(Allocator.TempJob);
        job.oldParticles = m_query.ToComponentDataArray<Particle>(Allocator.TempJob);
        job.translations = m_query.ToComponentDataArray<Translation>(Allocator.TempJob);
        job.particles = m_query.ToComponentDataArray<Particle>(Allocator.TempJob);
        //job.translations = GetComponentDataFromEntity<Translation>(false);
        //job.particles = GetComponentDataFromEntity<Particle>(false);

        // Now that the job is set up, schedule it to be run. 
        var jobHandle = job.Schedule(job.particles.Length, 16, inputDependencies);
        jobHandle.Complete();
        m_query.CopyFromComponentDataArray<Translation>(job.translations);
        m_query.CopyFromComponentDataArray<Particle>(job.particles);
        job.oldTranslations.Dispose();
        job.oldParticles.Dispose();
        job.translations.Dispose();
        job.particles.Dispose();
        return inputDependencies;
    }
}