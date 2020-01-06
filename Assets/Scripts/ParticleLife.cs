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
    #region Properties
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
    public float simulationSpeedMultiplicator = 1.0f;
    public float maxSpeed = 10.0f;
    public float friction = 0.9f;
    public float frictionTime = 0.1f;
    public float interactionStrength = 10.0f;
    public float radius = 5.0f;
    public float radiusVariation = 0.0f;
    public float r_smooth = 2.0f;
    public bool flatForce = false;

    [Header("Gravity")]
    public float3 gravityTarget = 0.5f;
    public float gravityStrength = 1e8f;
    public float maxGravity = 7f;

    [Header("World Properties")]
    public float3 lowerBound = -100;
    public float3 upperBound = +100;
    public bool wrapX = true;
    public bool wrapY = true;
    public bool wrapZ = true;
    public float bounce = 1.0f;
    public bool drawWorldBounds = true;

    [Header("Particle Instantiation")]
    public int spawnCount = 100;
    public float3 creationBoxSize = 0.5f;
    public float3 creationBoxCenter = 0.5f;

    [Header("Particle Visuals")]
    public Material particleMaterial;
    public Mesh particleMesh;

    #endregion


    #region private members for particle types
    private int m_numTypes = 0;
    private NativeArray<Color> m_Colors;
    private NativeArray<float> m_Attract;
    private NativeArray<float> m_RangeMin;
    private NativeArray<float> m_RangeMax;
    private List<Material> m_adjustedMaterials = new List<Material>();
    #endregion

    #region private members for entitities
    private NativeList<Entity> m_entities;
    private static EntityManager m_manager;
    private EntityArchetype m_archetype;
    private ParticleLifeSystem m_particleLifeSystem;
    #endregion

    #region code of particle types
    void initParticleTypeArrays(int num)
    {
        if (m_Colors.IsCreated && m_Colors.Length > 0) m_Colors.Dispose();
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
    #endregion

    #region startup and cleanup
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
    #endregion

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
            var fps = 1.0f / UnityEngine.Time.deltaTime;
            m_fps = fps * m_fpsGain + (1f - m_fpsGain) * m_fps;
        }
        if (math.abs(m_particleLifeSystem.lastSimulationDeltaTime) > 1e-9)
        {
            var simfps = 1.0f / m_particleLifeSystem.lastSimulationDeltaTime;
            m_simfps = simfps * m_simfpsGain + (1f - m_simfpsGain) * m_simfps;
        }
        m_simTimePerSecond = m_fps / m_simfps;
        if (numParticleTypes != m_numTypes)
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
            m_guiShowMain = true;
        }
    }
    #endregion

    #region code of GUI

    private enum GuiWindowID : int
    {
        Main = 0,
        ConfirmClear = 1,
        ParticleTypes = 2,
        Particles = 3,
        Simulation = 4,
        WorldBounds = 5,
    }
    private Rect m_guiDragArea = new Rect(0, 0, 10000, 20);
    private const float c_guiDefaultWinWidth = 200;
    private const float c_guiLeft = 20;
    private Rect m_guiWindowRectMain = new Rect(c_guiLeft + 0 * c_guiDefaultWinWidth, 20, c_guiDefaultWinWidth, 280);
    private Rect m_guiWindowRectConfirmClear = new Rect(600, 300, c_guiDefaultWinWidth, 100);
    private Rect m_guiWindowRectParticleTypes = new Rect(c_guiLeft + 0 * c_guiDefaultWinWidth, 300, c_guiDefaultWinWidth, 200);
    private Rect m_guiWindowRectParticles = new Rect(c_guiLeft + 1 * c_guiDefaultWinWidth, 300, c_guiDefaultWinWidth, 200);
    private Rect m_guiWindowRectSimulation = new Rect(c_guiLeft + 2 * c_guiDefaultWinWidth, 300, c_guiDefaultWinWidth, 200);
    private Rect m_guiWindowRectWorldBounds = new Rect(c_guiLeft + 3 * c_guiDefaultWinWidth, 300, c_guiDefaultWinWidth, 200);

    private bool m_guiShowMain = true;
    private bool m_guiShowConfirmClear = false;
    private bool m_guiShowParticleTypes = false;
    private bool m_guiShowParticles = false;
    private bool m_guiShowSimulation = false;
    private bool m_guiShowWorldBounds = false;

    private bool m_guiWindowMainIsResizing = false;
    private bool m_guiWindowParticleTypesIsResizing = false;
    private bool m_guiWindowParticlesIsResizing = false;
    private bool m_guiWindowSimulationIsResizing = false;
    private bool m_guiWindowWorldBoundsIsResizing = false;

    private Vector2 m_guiWindowMainScrollPosition;


    private void OnGUI()
    {
        if (m_guiShowMain)
        {
            m_guiWindowRectMain = GUILayout.Window((int)GuiWindowID.Main, m_guiWindowRectMain,
                OnGuiWindowMain, "Particle Life 3D",
                GUILayout.MinWidth(200), GUILayout.MinHeight(250));
        }
        if (m_guiShowConfirmClear)
        {
            m_guiWindowRectConfirmClear = GUI.ModalWindow((int)GuiWindowID.ConfirmClear, m_guiWindowRectConfirmClear, OnGuiConfirmClearWindow, "Confirm Clear");
            //m_guiWindowRectConfirmClear = GUILayout.Window((int)GuiWindowID.ConfirmClear, m_guiWindowRectConfirmClear, OnGuiConfirmClearWindow, "Confirm Clear");
        }
        if (m_guiShowParticleTypes)
        {
            m_guiWindowRectParticleTypes = GUILayout.Window((int)GuiWindowID.ParticleTypes, m_guiWindowRectParticleTypes,
                OnGUIWindowParticleTypes, "Particle Types",
                GUILayout.MinWidth(200), GUILayout.MinHeight(200));
        }
        if (m_guiShowParticles)
        {
            m_guiWindowRectParticles = GUILayout.Window((int)GuiWindowID.Particles, m_guiWindowRectParticles,
                OnGUIWindowParticles, "Particles",
                GUILayout.MinWidth(200), GUILayout.MinHeight(200));
        }
        if (m_guiShowSimulation)
        {
            m_guiWindowRectSimulation = GUILayout.Window((int)GuiWindowID.Simulation, m_guiWindowRectSimulation,
                OnGUIWindowSimulation, "Simulation",
                GUILayout.MinWidth(200), GUILayout.MinHeight(200));
        }
        if (m_guiShowWorldBounds)
        {
            m_guiWindowRectWorldBounds = GUILayout.Window((int)GuiWindowID.WorldBounds, m_guiWindowRectWorldBounds,
                OnGUIWindowWorldBounds, "WorldBounds",
                GUILayout.MinWidth(200), GUILayout.MinHeight(200));
        }
    }

    void OnGuiWindowMain(int winID)
    {
        m_guiWindowMainScrollPosition = GUILayout.BeginScrollView(m_guiWindowMainScrollPosition);
        GUILayout.Label("num particles: " + m_entities.Length);
        GUILayout.Label("frames per sec: " + (((int)(m_fps * 10 + 0.5f)) / 10.0f));
        GUILayout.Label("sim step per sec: " + (((int)(m_simfps * 10 + 0.5f)) / 10.0f));
        GUILayout.Label("sim time per sec: " + (((int)(m_simTimePerSecond * 10 + 0.5f)) / 10.0f));
        //GUILayout.Label("fps gain: " + m_fpsGain);
        //m_fpsGain = GUILayout.HorizontalSlider(m_fpsGain, 0f, 1f);
        GUILayout.Space(15f);

        if (GUILayout.Button("Particles Types"))
        {
            m_guiShowParticleTypes = !m_guiShowParticleTypes;
        }
        if (GUILayout.Button("Particles"))
        {
            m_guiShowParticles = !m_guiShowParticles;
        }
        if (GUILayout.Button("Simulation"))
        {
            m_guiShowSimulation = !m_guiShowSimulation;
        }
        if (GUILayout.Button("World Bounds"))
        {
            m_guiShowWorldBounds = !m_guiShowWorldBounds;
        }

        GUILayout.EndScrollView();

        GUI_WindowControls(ref m_guiWindowRectMain, ref m_guiShowMain, ref m_guiWindowMainIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }

    void OnGUIWindowParticleTypes(int winID)
    {
        GUILayout.Label("frames per sec: " + (((int)(m_fps * 10 + 0.5f)) / 10.0f));
        GUILayout.Label("num particle types: " + numParticleTypes);
        numParticleTypes = (int)GUILayout.HorizontalSlider(numParticleTypes, 1, 200);
        GUILayout.Label("attract mean: " + attractMean);
        attractMean = GUILayout.HorizontalSlider(attractMean, -10.0f, +10.0f);
        GUILayout.Label("attract std: " + attractStd);
        attractStd = GUILayout.HorizontalSlider(attractStd, 0.0f, +10.0f);
        GUILayout.Label("lower range min: " + rangeMinLower);
        rangeMinLower = GUILayout.HorizontalSlider(rangeMinLower, 0.0f, +50.0f);
        GUILayout.Label("upper range min: " + rangeMinUpper);
        rangeMinUpper = GUILayout.HorizontalSlider(rangeMinUpper, rangeMinLower, +50.0f);
        GUILayout.Label("lower range max: " + rangeMaxLower);
        rangeMaxLower = GUILayout.HorizontalSlider(rangeMaxLower, 0.0f, +50.0f);
        GUILayout.Label("upper range max: " + rangeMaxUpper);
        rangeMaxUpper = GUILayout.HorizontalSlider(rangeMaxUpper, rangeMaxLower, +50.0f);
        GUILayout.Space(25f);

        if (GUILayout.Button("Randomize Particle Types"))
        {
            randomizeParticleTypes();
        }

        GUILayout.Space(25f);

        GUI_WindowControls(ref m_guiWindowRectParticleTypes, ref m_guiShowParticleTypes, ref m_guiWindowParticleTypesIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }



    private bool m_guiCubeCreationBox;
    void OnGUIWindowParticles(int winID)
    {
        GUILayout.Label("num particles: " + m_entities.Length);
        GUILayout.Label("frames per sec: " + (((int)(m_fps * 10 + 0.5f)) / 10.0f));
        //GUILayout.Label("fps: " + (((int)(m_fps * 10)) / 10.0f));
        GUILayout.Space(25f);

        GUILayout.Label("spawn count: " + spawnCount);
        spawnCount = (int)GUILayout.HorizontalSlider(spawnCount, 1, 1000);

        GUILayout.Label("radius: " + radius + "\nonly for display and particle type initialization");
        radius = GUILayout.HorizontalSlider(radius, 0.0f, +50.0f);

        GUILayout.Label("radius variation: " + radiusVariation + "\nonly for display");
        radiusVariation = GUILayout.HorizontalSlider(radiusVariation, 0.0f, radius);
        if (GUILayout.Button("Random Particle Radii"))
        {
            setRandomRadii();
        }

        GUILayout.Label("creation box center");
        GUILayout.Label("x: " + creationBoxCenter.x);
        creationBoxCenter.x = GUILayout.HorizontalSlider(creationBoxCenter.x, 0.0f, +1.0f);
        GUILayout.Label("y: " + creationBoxCenter.y);
        creationBoxCenter.y = GUILayout.HorizontalSlider(creationBoxCenter.y, 0.0f, +1.0f);
        GUILayout.Label("z: " + creationBoxCenter.z);
        creationBoxCenter.z = GUILayout.HorizontalSlider(creationBoxCenter.z, 0.0f, +1.0f);
        GUILayout.Label("creation box size");
        var newCubeCreationBox = GUILayout.Toggle(m_guiCubeCreationBox, "toggle cube");
        if (newCubeCreationBox && !m_guiCubeCreationBox)
        {
            float mid = (creationBoxSize.x + creationBoxSize.y + creationBoxSize.z) / 3.0f;
            creationBoxSize.x = mid;
            creationBoxSize.y = mid;
            creationBoxSize.z = mid;
        }
        m_guiCubeCreationBox = newCubeCreationBox;

        GUILayout.Label("x: " + creationBoxSize.x);
        creationBoxSize.x = GUILayout.HorizontalSlider(creationBoxSize.x, 0.0f, +1.0f);
        if (m_guiCubeCreationBox && GUI.changed) creationBoxSize = creationBoxSize.x;

        GUILayout.Label("y: " + creationBoxSize.y);
        creationBoxSize.y = GUILayout.HorizontalSlider(creationBoxSize.y, 0.0f, +1.0f);
        if (m_guiCubeCreationBox && GUI.changed) creationBoxSize = creationBoxSize.y;

        GUILayout.Label("z: " + creationBoxSize.z);
        creationBoxSize.z = GUILayout.HorizontalSlider(creationBoxSize.z, 0.0f, +1.0f);
        if (m_guiCubeCreationBox && GUI.changed) creationBoxSize = creationBoxSize.z;


        if (GUILayout.Button("Spawn Particles"))
        {
            spawnParticles();
        }
        if (GUILayout.Button("Clear Particles") && m_entities.Length > 0)
        {
            m_guiShowConfirmClear = true;

            m_guiWindowRectConfirmClear.center = Camera.main.pixelRect.center;
        }

        if (GUILayout.Button("Respawn Particles"))
        {
            respawnParticles();
        }

        GUI_WindowControls(ref m_guiWindowRectParticles, ref m_guiShowParticles, ref m_guiWindowParticlesIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }
    void OnGuiConfirmClearWindow(int winID)
    {
        if (GUILayout.Button("Clear Particles"))
        {
            clearParticles();
            m_guiShowConfirmClear = false;
        }
        GUILayout.Space(25f);
        if (GUILayout.Button("Cancel"))
        {
            m_guiShowConfirmClear = false;
        }
        GUI.DragWindow(m_guiDragArea);
    }

    private string strGravityStrength = null;
    private string strInteractionStrength = null;
    void OnGUIWindowSimulation(int winID)
    {
        GUILayout.Label("sim step per sec: " + (((int)(m_simfps * 10 + 0.5f)) / 10.0f));
        GUILayout.Label("sim time per sec: " + (((int)(m_simTimePerSecond * 10 + 0.5f)) / 10.0f));
        //GUILayout.Label("sim fps: " + (((int)(m_simfps * 10)) / 10.0f));
        GUILayout.Label("min steps per simulated sec: ");
        GUILayout.Label("" + minSimulationStepRate);
        minSimulationStepRate = (int)GUILayout.HorizontalSlider(minSimulationStepRate, 0, 200);
        GUILayout.Label("simulation speed factor: ");
        GUILayout.Label("" + simulationSpeedMultiplicator);
        simulationSpeedMultiplicator = GUILayout.HorizontalSlider(simulationSpeedMultiplicator, 0.0f, +100.0f);
        GUILayout.Label("max speed: ");
        GUILayout.Label("" + maxSpeed);
        maxSpeed = GUILayout.HorizontalSlider(maxSpeed, 0.0f, +100.0f);
        GUILayout.Label("friction per "+ (((int)(frictionTime * 10 + 0.5f)) / 10.0f) + "s :");
        GUILayout.Label("velocity will be friction*velocity after frictionTime seconds");
        GUILayout.Label("" + friction);
        friction = GUILayout.HorizontalSlider(friction, 0.0f, 1.0f);
        GUILayout.Label("frictionTime " + frictionTime);
        frictionTime = GUILayout.HorizontalSlider(frictionTime, 0.0f, 2.0f);
        GUILayout.Label("interaction strength: ");
        if (strInteractionStrength == null)
            strInteractionStrength  = "" + interactionStrength;
        strInteractionStrength  = GUILayout.TextField(strInteractionStrength );
        float interactionStrengthFromStr;

        if (GUI.changed)
            if (float.TryParse(strInteractionStrength , out interactionStrengthFromStr))
            {
                interactionStrength = interactionStrengthFromStr;
                strInteractionStrength  = "" + interactionStrength;
            }
        interactionStrength = GUILayout.HorizontalSlider(interactionStrength, -2000.0f, 2000.0f);
        if (GUI.changed)
        {
            strInteractionStrength  = "" + interactionStrength;
        }

        //GUILayout.Label("" + interactionStrength);
        //interactionStrength = GUILayout.HorizontalSlider(interactionStrength, -2000.0f, 2000.0f);
        flatForce = GUILayout.Toggle(flatForce, "flat force");

        GUILayout.Space(25f);


        GUILayout.Label("r_smooth: " + r_smooth);
        r_smooth = GUILayout.HorizontalSlider(r_smooth, 0.0f, +10.0f);

        GUILayout.Space(25f);

        GUILayout.Label("gravity strength: ");
        if (strGravityStrength == null)
            strGravityStrength = "" + gravityStrength;
        strGravityStrength = GUILayout.TextField(strGravityStrength);
        float gravityStrengthFromStr;

        if (GUI.changed)
            if (float.TryParse(strGravityStrength, out gravityStrengthFromStr))
            {
                gravityStrength = gravityStrengthFromStr;
                strGravityStrength = "" + gravityStrength;
            }
        gravityStrength = GUILayout.HorizontalSlider(gravityStrength, -1e9f, +1e9f);
        if (GUI.changed)
        {
            strGravityStrength = "" + gravityStrength;
        }

        GUILayout.Label("max gravity: ");
        GUILayout.Label("" + maxGravity);
        maxGravity = GUILayout.HorizontalSlider(maxGravity, 0.0f, +100.0f);

        GUILayout.Label("gravity target");
        GUILayout.Label("x: " + gravityTarget.x);
        gravityTarget.x = GUILayout.HorizontalSlider(gravityTarget.x, 0.0f, +1.0f);
        GUILayout.Label("y: " + gravityTarget.y);
        gravityTarget.y = GUILayout.HorizontalSlider(gravityTarget.y, 0.0f, +1.0f);
        GUILayout.Label("z: " + gravityTarget.z);
        gravityTarget.z = GUILayout.HorizontalSlider(gravityTarget.z, 0.0f, +1.0f);

        //if (GUILayout.Button("Randomize Particle Types"))
        //{
        //    //randomizeSimulation();
        //}

        //GUILayout.Space(25f);

        GUI_WindowControls(ref m_guiWindowRectSimulation, ref m_guiShowSimulation, ref m_guiWindowSimulationIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }
    private float3 m_guiWorldBoxCenter;
    private float3 m_guiWorldBoxSize;
    private float m_guiWorldBoxSizeMaxValue;
    private bool m_guiCubeWorldBox;
    void OnGUIWindowWorldBounds(int winID)
    {
        drawWorldBounds = GUILayout.Toggle(drawWorldBounds, "draw world bounds");
        m_guiWorldBoxSize = upperBound - lowerBound;
        m_guiWorldBoxCenter = lowerBound + m_guiWorldBoxSize * 0.5f;
        GUILayout.Label("world box center");
        GUILayout.Label("x: " + m_guiWorldBoxCenter.x);
        m_guiWorldBoxCenter.x = GUILayout.HorizontalSlider(m_guiWorldBoxCenter.x, -m_guiWorldBoxSize.x, +m_guiWorldBoxSize.x);
        GUILayout.Label("y: " + m_guiWorldBoxCenter.y);
        m_guiWorldBoxCenter.y = GUILayout.HorizontalSlider(m_guiWorldBoxCenter.y, -m_guiWorldBoxSize.y, +m_guiWorldBoxSize.y);
        GUILayout.Label("z: " + m_guiWorldBoxCenter.z);
        m_guiWorldBoxCenter.z = GUILayout.HorizontalSlider(m_guiWorldBoxCenter.z, -m_guiWorldBoxSize.z, +m_guiWorldBoxSize.z);
        GUILayout.Label("world box size");
        m_guiCubeWorldBox = GUILayout.Toggle(m_guiCubeWorldBox, "cube");
        if (GUI.changed && m_guiCubeWorldBox)
        {
            float mid = (m_guiWorldBoxSize.x + m_guiWorldBoxSize.y + m_guiWorldBoxSize.z) / 3.0f;
            m_guiWorldBoxSize = mid;
        }
        m_guiWorldBoxSizeMaxValue = math.max(m_guiWorldBoxSize.x, m_guiWorldBoxSizeMaxValue);
        m_guiWorldBoxSizeMaxValue = math.max(m_guiWorldBoxSize.y, m_guiWorldBoxSizeMaxValue);
        m_guiWorldBoxSizeMaxValue = math.max(m_guiWorldBoxSize.z, m_guiWorldBoxSizeMaxValue);
        GUILayout.Label("max value: " + m_guiWorldBoxSizeMaxValue);
        m_guiWorldBoxSizeMaxValue = GUILayout.HorizontalSlider(m_guiWorldBoxSizeMaxValue, 1.0f, +20000.0f);
        GUILayout.Label("x: " + m_guiWorldBoxSize.x);
        m_guiWorldBoxSize.x = GUILayout.HorizontalSlider(m_guiWorldBoxSize.x, 0.0f, m_guiWorldBoxSizeMaxValue);
        if (m_guiCubeWorldBox && GUI.changed) m_guiWorldBoxSize = m_guiWorldBoxSize.x;

        GUILayout.Label("y: " + m_guiWorldBoxSize.y);
        m_guiWorldBoxSize.y = GUILayout.HorizontalSlider(m_guiWorldBoxSize.y, 0.0f, m_guiWorldBoxSizeMaxValue);
        if (m_guiCubeWorldBox && GUI.changed) m_guiWorldBoxSize = m_guiWorldBoxSize.y;

        GUILayout.Label("z: " + m_guiWorldBoxSize.z);
        m_guiWorldBoxSize.z = GUILayout.HorizontalSlider(m_guiWorldBoxSize.z, 0.0f, m_guiWorldBoxSizeMaxValue);
        if (m_guiCubeWorldBox && GUI.changed) m_guiWorldBoxSize = m_guiWorldBoxSize.z;

        lowerBound = m_guiWorldBoxCenter - m_guiWorldBoxSize * 0.5f;
        upperBound = m_guiWorldBoxCenter + m_guiWorldBoxSize * 0.5f;

        GUILayout.Space(5f);
        wrapX = GUILayout.Toggle(wrapX, "wrap x");
        wrapY = GUILayout.Toggle(wrapY, "wrap y");
        wrapZ = GUILayout.Toggle(wrapZ, "wrap z");

        GUILayout.Label("bounce: " + bounce);
        bounce = GUILayout.HorizontalSlider(bounce, -1f, +1f);

        GUILayout.Space(5f);

        if (GUILayout.Button("Spawn Particles"))
        {
            spawnParticles();
        }
        if (GUILayout.Button("Clear Particles") && m_entities.Length > 0)
        {
            m_guiShowConfirmClear = true;

            m_guiWindowRectConfirmClear.center = Camera.main.pixelRect.center;
        }
        if (GUILayout.Button("Respawn Particles"))
        {
            respawnParticles();
        }

        GUI_WindowControls(ref m_guiWindowRectWorldBounds, ref m_guiShowWorldBounds, ref m_guiWindowWorldBoundsIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }


    void GUI_WindowControls(ref Rect windowRect, ref bool show, ref bool isResizing)
    {
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Close"))
        {
            show = false;
        }
        GUILayout.FlexibleSpace();
        GUILayout.Box("resize", GUILayout.MinWidth(20), GUILayout.MaxHeight(20));
        if (Event.current.type == EventType.MouseUp)
        {
            isResizing = false;
        }
        else if (Event.current.type == EventType.MouseDown &&
                 GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        {
            isResizing = true;
        }
        if (isResizing)
        {
            windowRect = new Rect(windowRect.x, windowRect.y,
                windowRect.width + Event.current.delta.x, windowRect.height + Event.current.delta.y);
        }
        GUILayout.EndHorizontal();
    }


    #endregion




    #region user commands
    void updateParticleTypeNumber()
    {
        initParticleTypeArrays(numParticleTypes);
        setRandomTypes();
        setSystemTypeArrays();
        updateParticleTypesAndMaterials();
    }

    void spawnParticles()
    {
        addParticles(spawnCount);
    }

    void respawnParticles()
    {
        var num = m_entities.Length;
        clearParticles();
        addParticles(num);
    }

    void randomizeParticleTypes()
    {
        setRandomTypes();
        setSystemTypeArrays();
    }
    void setRandomRadii()
    {
        for (int i = 0; i < m_entities.Length; i++)
        {
            float r = radius + Random.Range(-radiusVariation, radiusVariation);
            m_manager.SetComponentData(m_entities[i], new Scale { Value = r * 2f });
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
            float r = radius + Random.Range(-radiusVariation, radiusVariation);
            var mat = (type < m_adjustedMaterials.Count) ? m_adjustedMaterials[type] : particleMaterial;
            m_manager.SetComponentData(particles[i], new Translation { Value = new float3(x, y, z) });
            m_manager.SetComponentData(particles[i], new Particle { type = type, vel = new float3(vx, vy, vz), cell_number = -1 });
            m_manager.SetComponentData(particles[i], new Scale { Value = r * 2f });
            m_manager.SetSharedComponentData(particles[i], new RenderMesh { material = mat, mesh = particleMesh });
            m_entities.Add(particles[i]);
        }

        particles.Dispose();
    }
    #endregion






}
