using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.Globalization;
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
    public ParticleLifeSettings settings;
    public ParticleTypes types;
    public ParticleLifeGui gui;

    [Header("Particle Visuals")]
    public Material particleMaterial;
    public Mesh particleMesh;
    private List<Material> m_adjustedMaterials = new List<Material>();
    // todo add cube and sphere and option in settings or maybe per particletype?

    #region private members for entitities
    public NativeList<Entity> m_entities;
    private static EntityManager m_manager;
    public EntityArchetype m_archetype;
    public ParticleLifeSystem m_particleLifeSystem;
    public EntityQuery m_query;
    #endregion

    #region code of particle types

    public void generateMaterials()
    {
        m_adjustedMaterials.Clear();
        for (int i = 0; i < types.m_numTypes; i++)
        {
            Color particle_color = types.m_Colors[i];
            m_adjustedMaterials.Add(new Material(particleMaterial) { color = particle_color });
        }
    }


    public void setSystemTypeArrays()
    {
        m_particleLifeSystem.Attract = types.m_Attract;
        m_particleLifeSystem.RangeMin = types.m_RangeMin;
        m_particleLifeSystem.RangeMax = types.m_RangeMax;
        m_particleLifeSystem.numTypes = types.m_numTypes;
    }

    public void updateParticleTypesAndMaterials()
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
    #endregion

    #region startup and cleanup
    void Start()
    {
        if (settings == null)
            settings = GetComponent<ParticleLifeSettings>();
        if (types == null)
            types = GetComponent<ParticleTypes>();
        if (gui == null)
            gui = GetComponent<ParticleLifeGui>();

        m_manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        m_archetype = m_manager.CreateArchetype(
            typeof(Translation),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(Particle),
            typeof(Scale)
        );
        var queryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Translation), typeof(Particle), typeof(Scale) }
        };
        m_query = m_manager.CreateEntityQuery(queryDesc);
        m_particleLifeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ParticleLifeSystem>();
        m_particleLifeSystem.particleLife = this;
        types.initParticleTypeArrays(settings.numParticleTypes);
        generateMaterials();
        types.setRandomTypes(ref settings);
        setSystemTypeArrays();
        addParticles(settings.spawnCount);
    }

    private void OnEnable()
    {
        m_entities = new NativeList<Entity>(10000, Allocator.Persistent);
    }

    private void OnDisable()
    {
        m_entities.Dispose();
    }
    #endregion

    public float fps { get { return m_fps; } }
    public float simfps { get { return m_simfps; } }
    public float simTimePerSecond { get { return m_simTimePerSecond; } }

    #region update and user input
    private float m_fps = 0f;
    private float m_fpsGain = 0.05f;
    private float m_simfps = 0f;
    private float m_simfpsGain = 0.05f;
    private float m_simTimePerSecond = 0f;
    void Update()
    {
        if (math.abs(UnityEngine.Time.deltaTime) > 1e-9)
        {
            var new_fps = 1.0f / UnityEngine.Time.deltaTime;
            m_fps = new_fps * m_fpsGain + (1f - m_fpsGain) * m_fps;
        }
        if (math.abs(m_particleLifeSystem.lastSimulationDeltaTime) > 1e-9)
        {
            var new_simfps = 1.0f / m_particleLifeSystem.lastSimulationDeltaTime;
            m_simfps = new_simfps * m_simfpsGain + (1f - m_simfpsGain) * m_simfps;
        }
        m_simTimePerSecond = m_fps / m_simfps;
        if (settings.numParticleTypes != types.m_numTypes)
        {
            updateParticleTypeNumber();
        }
        if (Input.GetKeyDown("space"))
        {
            spawnParticles();
        }
        if (Input.GetKeyDown("c"))
        {
            clearParticles();
        }
        if (Input.GetKeyDown("r"))
        {
            respawnParticles();
        }
        if (Input.GetKeyDown("t"))
        {
            randomizeParticleTypes();
        }
        if (Input.GetMouseButtonDown(0))
        {
            gui.m_guiShowMain = true;
        }
        
    }
    #endregion




    #region user commands
    public void updateParticleTypeNumber()
    {
        types.initParticleTypeArrays(settings.numParticleTypes);
        generateMaterials();
        types.setRandomTypes(ref settings);
        setSystemTypeArrays();
        updateParticleTypesAndMaterials();
    }

    public void spawnParticles()
    {
        addParticles(settings.spawnCount);
    }

    public void respawnParticles()
    {
        var num = m_entities.Length;
        clearParticles();
        addParticles(num);
    }

    public void randomizeParticleTypes()
    {
        types.setRandomTypes(ref settings);
        setSystemTypeArrays();
    }
    public void setRandomRadii()
    {
        for (int i = 0; i < m_entities.Length; i++)
        {
            float r = settings.radius + Random.Range(-settings.radiusVariation, settings.radiusVariation);
            m_manager.SetComponentData(m_entities[i], new Scale { Value = r * 2f });
        }
    }
    public void clearParticles()
    {
        for (int i = 0; i < m_entities.Length; i++)
        {
            m_manager.DestroyEntity(m_entities[i]);
        }
        m_entities.Clear();
        //fps.numParticles = particles.Count;
    }
    public void addParticles(int num)
    {
        NativeArray<Entity> particles = new NativeArray<Entity>(num, Allocator.Temp);

        m_manager.CreateEntity(m_archetype, particles);

        float3 dim = settings.upperBound - settings.lowerBound;
        float3 boxCenter = settings.creationBoxCenter * dim + settings.lowerBound;
        float3 posLowerBound = boxCenter - 0.5f * settings.creationBoxSize * dim;
        float3 posUpperBound = boxCenter + 0.5f * settings.creationBoxSize * dim;

        for (int i = 0; i < num; i++)
        {
            float x = Random.Range(posLowerBound.x, posUpperBound.x);
            float y = Random.Range(posLowerBound.y, posUpperBound.y);
            float z = Random.Range(posLowerBound.z, posUpperBound.z);
            float vx = Random.Range(-2.0f, 2.0f);
            float vy = Random.Range(-2.0f, 2.0f);
            float vz = Random.Range(-2.0f, 2.0f);
            int type = Random.Range(0, types.m_numTypes);
            float r = settings.radius + Random.Range(-settings.radiusVariation, settings.radiusVariation);
            var mat = (type < m_adjustedMaterials.Count) ? m_adjustedMaterials[type] : particleMaterial;
            m_manager.SetComponentData(particles[i], new Translation { Value = new float3(x, y, z) });
            m_manager.SetComponentData(particles[i], new Particle { type = type, vel = new float3(vx, vy, vz) });
            m_manager.SetComponentData(particles[i], new Scale { Value = r * 2f });
            m_manager.SetSharedComponentData(particles[i], new RenderMesh { material = mat, mesh = particleMesh });
            m_entities.Add(particles[i]);
        }

        particles.Dispose();
    }
    #endregion
}
