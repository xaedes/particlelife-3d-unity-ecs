using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = UnityEngine.Random;

public struct Particle : IComponentData
{
    public int type;
    public float3 vel;
    public int cell_number;
}

public class ParticleLife : MonoBehaviour
{
    public int spawnCount = 100;
    public float3 lowerBound = -100;
    public float3 upperBound = +100;

    public Material particleMaterial;
    public Mesh particleMesh;

    private static EntityManager m_manager;
    private EntityArchetype m_archetype;
    
    // Start is called before the first frame update
    void Start()
    {
        m_manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        m_archetype = m_manager.CreateArchetype(
            typeof(Translation),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(Particle)
        );
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            AddParticles(spawnCount);
        }
    }

    void AddParticles(int num)
    {
        NativeArray<Entity> particles = new NativeArray<Entity>(num, Allocator.Temp);

        m_manager.CreateEntity(m_archetype, particles);

        for (int i = 0; i < num; i++)
        {
            float x = Random.Range(lowerBound.x, upperBound.x);
            float y = Random.Range(lowerBound.y, upperBound.y);
            float z = Random.Range(lowerBound.z, upperBound.z);
            float vx = Random.Range(-2.0f, 2.0f);
            float vy = Random.Range(-2.0f, 2.0f);
            float vz = Random.Range(-2.0f, 2.0f);
            m_manager.SetComponentData(particles[i], new Translation { Value = new float3(x, y, z) });
            m_manager.SetComponentData(particles[i], new Particle { type = 0, vel = new float3(vx, vy, vz), cell_number=-1 });
            m_manager.SetSharedComponentData(particles[i], new RenderMesh { material = particleMaterial, mesh = particleMesh });
        }

        particles.Dispose();
    }
}
