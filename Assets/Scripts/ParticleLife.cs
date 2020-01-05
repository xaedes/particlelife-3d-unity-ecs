using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = UnityEngine.Random;
using Unity.Burst;


public class ParticleLife : MonoBehaviour
{
    [Header("Particle Types")]
    //public int maxInteractions = 1000000;
    public int numParticleTypes = 10;
    public float attractMean = 0;
    public float attractStd = 0.2f;
    public float rangeMinLower = 0.0f;
    public float rangeMinUpper = 20.0f;
    public float rangeMaxLower = 10.0f;
    public float rangeMaxUpper = 50.0f;

    [Header("Simulation Properties")]
    public float minSimulationStepRate = 60.0f;
    public float maxSpeed = 10.0f;
    public float friction = 0.9f;
    public float interactionStrength = 10.0f;
    public float radius = 5.0f;
    public float r_smooth = 2.0f;
    public bool flatForce = false;

    [Header("World Properties")]
    public float3 lowerBound = -100;
    public float3 upperBound = +100;
    public bool wrapX = true;
    public bool wrapY = true;
    public bool wrapZ = true;
    public float bounce = 1.0f;

    [Header("Particle Instantiation")]
    public int spawnCount = 100;
    public float3 creationBoxSize = 0.5f;
    public float3 creationBoxCenter = 0.5f;

    [Header("Particle Visuals")]
    public Material particleMaterial;
    public Mesh particleMesh;



    private int m_numTypes = 0;
    private NativeArray<Color> m_Colors;
    private NativeArray<float> m_Attract;
    private NativeArray<float> m_RangeMin;
    private NativeArray<float> m_RangeMax;
    private List<Material> m_adjustedMaterials = new List<Material>();


    private static EntityManager m_manager;
    private EntityArchetype m_archetype;
    private ParticleLifeSystem m_particleLifeSystem;
    private NativeList<Entity> m_entities;

    void initParticleTypeArrays(int num)
    {
        if(m_Colors.IsCreated && m_Colors.Length>0) m_Colors.Dispose();
        if (m_Attract.IsCreated && m_Attract.Length > 0) m_Attract.Dispose();
        if (m_RangeMin.IsCreated && m_RangeMin.Length > 0) m_RangeMin.Dispose();
        if (m_RangeMax.IsCreated && m_RangeMax.Length > 0) m_RangeMax.Dispose();
        m_numTypes = num;
        m_Colors = new NativeArray<Color>(m_numTypes, Allocator.Persistent);
        m_Attract = new NativeArray<float>(m_numTypes * m_numTypes, Allocator.Persistent);
        m_RangeMin = new NativeArray<float>(m_numTypes * m_numTypes, Allocator.Persistent);
        m_RangeMax = new NativeArray<float>(m_numTypes * m_numTypes, Allocator.Persistent);
        for (int i = 0; i < m_numTypes; i++)
        {
            m_Colors[i] = fromHSV((float)i / m_numTypes, 1.0f, i % 2 * 0.5f + 0.5f);
        }
        generateMaterials();
    }
    Color fromHSV(float h, float s, float v)
    {
        int i = (int)(h * 6);
        float f = h * 6 - i;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);

        float r = 0;
        float g = 0;
        float b = 0;
        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Color(r, g, b);
    }

    void generateMaterials()
    {
        m_adjustedMaterials.Clear();
        for (int i = 0; i < m_numTypes; i++)
        {
            Color particle_color = m_Colors[i];
            m_adjustedMaterials.Add(new Material(particleMaterial) { color = particle_color });
        }
    }
    public static float nextGaussianFloat()
    {
        float u, v, S;

        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
            S = u * u + v * v;
        }
        //while ((S >= 1.0f));
        while ((S >= 1.0f) || (math.abs(S) < 1e-6));

        float fac = math.sqrt(-2.0f * math.log(S) / S);
        return u * fac;
    }
    void setRandomTypes()
    {
        if (!m_Attract.IsCreated || !m_RangeMin.IsCreated || !m_RangeMax.IsCreated)
        {
            Debug.LogError("setRandomParticleTypes called but Attract, RangeMin or RangeMax are nor created.");
            return;
        }
        int num2 = m_numTypes * m_numTypes;
        if ((m_Attract.Length != num2) || (m_RangeMin.Length != num2) || (m_RangeMax.Length != num2))
        {
            Debug.LogError("setRandomParticleTypes called but Attract, RangeMin or RangeMax have wrong size.");
            return;
        }
        for (int i = 0; i < m_numTypes; i++)
        {
            for (int k = 0; k < m_numTypes; k++)
            {
                int coord = i * m_numTypes + k;
                int icoord = k * m_numTypes + i;
                if (i == k)
                {
                    // coord == icoord
                    m_Attract[coord] = -math.abs(nextGaussianFloat() * attractStd + attractMean);
                    //m_Attract[coord] = (nextGaussianFloat(r) * attractStd + attractMean);
                    m_RangeMin[coord] = radius * 2;
                }
                else
                {
                    m_Attract[coord] = nextGaussianFloat() * attractStd + attractMean;
                    m_Attract[icoord] = nextGaussianFloat() * attractStd + attractMean;
                    m_RangeMin[coord] = math.max(radius * 2, UnityEngine.Random.Range(rangeMinLower, rangeMinUpper));
                }
                m_RangeMax[coord] = math.max(m_RangeMin[coord], UnityEngine.Random.Range(rangeMaxLower, rangeMaxUpper));
                m_RangeMin[icoord] = m_RangeMin[coord];
                m_RangeMax[icoord] = m_RangeMax[coord];
            }
        }
    }

    void Start()
    {
        m_manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        m_archetype = m_manager.CreateArchetype(
            typeof(Translation),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(Particle),
            typeof(Scale)
        );
        m_particleLifeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ParticleLifeSystem>();
        m_particleLifeSystem.particleLife = this;
        initParticleTypeArrays(numParticleTypes);
        setRandomTypes();
        setSystemTypeArrays();
        addParticles(spawnCount);
    }

    private void OnEnable()
    {
        m_entities = new NativeList<Entity>(10000, Allocator.Persistent);
    }

    private void OnDisable()
    {
        m_entities.Dispose();
        m_Colors.Dispose();
        m_Attract.Dispose();
        m_RangeMin.Dispose();
        m_RangeMax.Dispose();
    }

    void setSystemTypeArrays()
    {
        m_particleLifeSystem.Attract = m_Attract;
        m_particleLifeSystem.RangeMin = m_RangeMin;
        m_particleLifeSystem.RangeMax = m_RangeMax;
        m_particleLifeSystem.numTypes = m_numTypes;
    }
    
    void updateParticleTypesAndMaterials()
    {
        if (m_adjustedMaterials.Count == 0) return;
        for (int i = 0; i < m_entities.Length; i++)
        {
            var particle = m_manager.GetComponentData<Particle>(m_entities[i]);
            particle.type = particle.type % m_adjustedMaterials.Count;
            
            var mat = (particle.type < m_adjustedMaterials.Count) 
                ? m_adjustedMaterials[particle.type] 
                : particleMaterial;
            m_manager.SetComponentData<Particle>(m_entities[i], particle);
            m_manager.SetSharedComponentData(m_entities[i], 
                new RenderMesh { material = mat, mesh = particleMesh });
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (numParticleTypes != m_numTypes)
        {
            initParticleTypeArrays(numParticleTypes);
            setRandomTypes();
            setSystemTypeArrays();
            updateParticleTypesAndMaterials();
        }
        if (Input.GetKeyDown("space"))
        {
            addParticles(spawnCount);
        }
        if (Input.GetKeyDown("c"))
        {
            clearParticles();
        }
        if (Input.GetKeyDown("r"))
        {
            var num = m_entities.Length;
            clearParticles();
            addParticles(num);
        }
        if (Input.GetKeyDown("t"))
        {
            setRandomTypes();
            setSystemTypeArrays();
        }
    }
    void clearParticles()
    {
        for (int i = 0; i < m_entities.Length; i++)
        {
            m_manager.DestroyEntity(m_entities[i]);
        }
        m_entities.Clear();
        //fps.numParticles = particles.Count;
    }


    void addParticles(int num)
    {
        NativeArray<Entity> particles = new NativeArray<Entity>(num, Allocator.Temp);

        m_manager.CreateEntity(m_archetype, particles);

        float3 dim = upperBound - lowerBound;
        float3 boxCenter = creationBoxCenter * dim + lowerBound;
        float3 posLowerBound = boxCenter - 0.5f * creationBoxSize * dim;
        float3 posUpperBound = boxCenter + 0.5f * creationBoxSize * dim;

        for (int i = 0; i < num; i++)
        {
            float x = Random.Range(posLowerBound.x, posUpperBound.x);
            float y = Random.Range(posLowerBound.y, posUpperBound.y);
            float z = Random.Range(posLowerBound.z, posUpperBound.z);
            float vx = Random.Range(-2.0f, 2.0f);
            float vy = Random.Range(-2.0f, 2.0f);
            float vz = Random.Range(-2.0f, 2.0f);
            int type = Random.Range(0, m_numTypes);
            var mat = (type < m_adjustedMaterials.Count) ? m_adjustedMaterials[type] : particleMaterial;
            m_manager.SetComponentData(particles[i], new Translation { Value = new float3(x, y, z) });
            m_manager.SetComponentData(particles[i], new Particle { type = type, vel = new float3(vx, vy, vz), cell_number=-1 });
            m_manager.SetComponentData(particles[i], new Scale { Value = radius*2f });
            m_manager.SetSharedComponentData(particles[i], new RenderMesh { material = mat, mesh = particleMesh });
            m_entities.Add(particles[i]);
        }

        particles.Dispose();
    }
}
