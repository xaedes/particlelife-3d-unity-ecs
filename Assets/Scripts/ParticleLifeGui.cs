using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using System.Runtime.InteropServices;
public class ParticleLifeGui : MonoBehaviour
{
    public ParticleLifeSettings settings;
    public ParticleTypes types;
    public ParticleLife particleLife;
    public CameraSettings cameraSettings;

    public TextAsset[] examples;

    void Start()
    {
        if (settings == null)
            settings = GetComponent<ParticleLifeSettings>();
        if (types == null)
            types = GetComponent<ParticleTypes>();
        if (particleLife == null)
            particleLife = GetComponent<ParticleLife>();
    }
    #region code of GUI

    public enum GuiWindowID : int
    {
        Main = 0,
        ConfirmClear = 1,
        ParticleTypes = 2,
        Particles = 3,
        Simulation = 4,
        WorldBounds = 5,
        Stats = 6,
        Movement = 7,
        Gravity = 8,
        Library = 9,
    }
    public Rect m_guiDragArea = new Rect(0, 0, 10000, 20);
    public const float c_guiDefaultWinWidth = 200;
    public const float c_guiLeft = 20;
    public Rect m_guiWindowRectMain = new Rect(c_guiLeft + 0 * c_guiDefaultWinWidth, 20, c_guiDefaultWinWidth, 300);
    public Rect m_guiWindowRectConfirmClear = new Rect(600, 300, c_guiDefaultWinWidth, 100);
    public Rect m_guiWindowRectParticleTypes = new Rect(c_guiLeft + 0 * c_guiDefaultWinWidth, 320, c_guiDefaultWinWidth, 200);
    public Rect m_guiWindowRectParticles = new Rect(c_guiLeft + 1 * c_guiDefaultWinWidth, 320, c_guiDefaultWinWidth, 200);
    public Rect m_guiWindowRectSimulation = new Rect(c_guiLeft + 2 * c_guiDefaultWinWidth, 320, c_guiDefaultWinWidth, 400);
    public Rect m_guiWindowRectMovement = new Rect(c_guiLeft + 3 * c_guiDefaultWinWidth, 320, c_guiDefaultWinWidth, 400);
    public Rect m_guiWindowRectGravity = new Rect(c_guiLeft + 4 * c_guiDefaultWinWidth, 320, c_guiDefaultWinWidth, 400);
    public Rect m_guiWindowRectWorldBounds = new Rect(20 + c_guiLeft + 5 * c_guiDefaultWinWidth, 320, c_guiDefaultWinWidth, 200);
    public Rect m_guiWindowRectStats = new Rect(c_guiLeft + 1 * c_guiDefaultWinWidth, 20, c_guiDefaultWinWidth, 300);
    public Rect m_guiWindowRectLibrary = new Rect(c_guiLeft + 2 * c_guiDefaultWinWidth, 20, 2*c_guiDefaultWinWidth, 300);

    public bool m_guiShowMain = true;
    public bool m_guiShowConfirmClear = false;
    public bool m_guiShowParticleTypes = false;
    public bool m_guiShowParticles = false;
    public bool m_guiShowSimulation = false;
    public bool m_guiShowWorldBounds = false;
    public bool m_guiShowMovement = false;
    public bool m_guiShowGravity = false;
    public bool m_guiShowStats = false;
    public bool m_guiShowLibrary = false;

    public bool m_guiWindowMainIsResizing = false;
    public bool m_guiWindowParticleTypesIsResizing = false;
    public bool m_guiWindowParticlesIsResizing = false;
    public bool m_guiWindowSimulationIsResizing = false;
    public bool m_guiWindowWorldBoundsIsResizing = false;
    public bool m_guiWindowMovementIsResizing = false;
    public bool m_guiWindowGravityIsResizing = false;
    public bool m_guiWindowStatsIsResizing = false;
    public bool m_guiWindowLibraryIsResizing = false;

    public Vector2 m_guiWindowMainScrollPosition;
    public Vector2 m_guiWindowSimulationScrollPosition;
    public Vector2 m_guiWindowStatsScrollPosition;
    public Vector2 m_guiWindowLibraryScrollPosition;
    public Vector2 m_guiWindowLibraryLeftColScrollPosition;


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
        if (m_guiShowMovement)
        {
            m_guiWindowRectMovement = GUILayout.Window((int)GuiWindowID.Movement, m_guiWindowRectMovement,
                OnGUIWindowMovement, "Movement",
                GUILayout.MinWidth(200), GUILayout.MinHeight(200));
        }
        if (m_guiShowGravity)
        {
            m_guiWindowRectGravity = GUILayout.Window((int)GuiWindowID.Gravity, m_guiWindowRectGravity,
                OnGUIWindowGravity, "Gravity",
                GUILayout.MinWidth(200), GUILayout.MinHeight(200));
        }
        if (m_guiShowWorldBounds)
        {
            m_guiWindowRectWorldBounds = GUILayout.Window((int)GuiWindowID.WorldBounds, m_guiWindowRectWorldBounds,
                OnGUIWindowWorldBounds, "WorldBounds",
                GUILayout.MinWidth(200), GUILayout.MinHeight(200));
        }
        if (m_guiShowStats)
        {
            m_guiWindowRectStats = GUILayout.Window((int)GuiWindowID.Stats, m_guiWindowRectStats,
                OnGUIWindowStats, "Stats",
                GUILayout.MinWidth(200), GUILayout.MinHeight(200));
        }
        if (m_guiShowLibrary)
        {
            m_guiWindowRectLibrary = GUILayout.Window((int)GuiWindowID.Library, m_guiWindowRectLibrary,
                OnGUIWindowLibrary, "Library",
                GUILayout.MinWidth(200), GUILayout.MinHeight(200));
        }
    }

    void OnGuiWindowMain(int winID)
    {
        m_guiWindowMainScrollPosition = GUILayout.BeginScrollView(m_guiWindowMainScrollPosition);
        GUILayout.Label("num particles: " + particleLife.m_entities.Length);
        GUILayout.Label("frames per sec: " + (((int)(particleLife.fps * 10 + 0.5f)) / 10.0f));
        GUILayout.Label("steps per sim sec: " + (((int)(particleLife.simfps * 10 + 0.5f)) / 10.0f));
        //GUILayout.Label("sim time per sec: " + (((int)(m_simTimePerSecond * 10 + 0.5f)) / 10.0f));
        //GUILayout.Label("fps gain: " + particleLife.fpsGain);
        //m_fpsGain = GUILayout.HorizontalSlider(m_fpsGain, 0f, 1f);

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
        if (GUILayout.Button("Stats"))
        {
            m_guiShowStats = !m_guiShowStats;
        }
        if (GUILayout.Button("Library"))
        {
            m_guiShowLibrary = !m_guiShowLibrary;
        }



        GUILayout.EndScrollView();

        GUI_WindowControls(ref m_guiWindowRectMain, ref m_guiShowMain, ref m_guiWindowMainIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }

    void OnGUIWindowParticleTypes(int winID)
    {
        GUILayout.Label("frames per sec: " + (((int)(particleLife.fps * 10 + 0.5f)) / 10.0f));
        GUILayout.Label("num particle types: " + settings.numParticleTypes);
        settings.numParticleTypes = (int)GUILayout.HorizontalSlider(settings.numParticleTypes, 1, 200);
        GUILayout.Label("attract mean: " + settings.attractMean);
        settings.attractMean = GUILayout.HorizontalSlider(settings.attractMean, -10.0f, +10.0f);
        GUILayout.Label("attract std: " + settings.attractStd);
        settings.attractStd = GUILayout.HorizontalSlider(settings.attractStd, 0.0f, +10.0f);
        GUILayout.Label("lower range min: " + settings.rangeMinLower);
        settings.rangeMinLower = GUILayout.HorizontalSlider(settings.rangeMinLower, 0.0f, +50.0f);
        GUILayout.Label("upper range min: " + settings.rangeMinUpper);
        settings.rangeMinUpper = GUILayout.HorizontalSlider(settings.rangeMinUpper, settings.rangeMinLower, +50.0f);
        GUILayout.Label("lower range max: " + settings.rangeMaxLower);
        settings.rangeMaxLower = GUILayout.HorizontalSlider(settings.rangeMaxLower, 0.0f, +50.0f);
        GUILayout.Label("upper range max: " + settings.rangeMaxUpper);
        settings.rangeMaxUpper = GUILayout.HorizontalSlider(settings.rangeMaxUpper, settings.rangeMaxLower, +50.0f);
        GUILayout.Space(25f);

        if (GUILayout.Button("Randomize Particle Types"))
        {
            particleLife.randomizeParticleTypes();
        }

        GUILayout.Space(25f);

        GUI_WindowControls(ref m_guiWindowRectParticleTypes, ref m_guiShowParticleTypes, ref m_guiWindowParticleTypesIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }



    private bool m_guiCubeCreationBox;
    void OnGUIWindowParticles(int winID)
    {
        GUILayout.Label("num particles: " + particleLife.m_entities.Length);
        GUILayout.Label("frames per sec: " + (((int)(particleLife.fps * 10 + 0.5f)) / 10.0f));
        //GUILayout.Label("fps: " + (((int)(particleLife.fps * 10)) / 10.0f));
        GUILayout.Space(15f);

        GUILayout.Label("spawn count: " + settings.spawnCount);
        settings.spawnCount = (int)GUILayout.HorizontalSlider(settings.spawnCount, 1, 1000);

        GUILayout.Label("radius: " + settings.radius + "\nonly for display and particle type initialization");
        settings.radius = GUILayout.HorizontalSlider(settings.radius, 0.0f, +50.0f);

        GUILayout.Label("radius variation: " + settings.radiusVariation + "\nonly for display");
        settings.radiusVariation = GUILayout.HorizontalSlider(settings.radiusVariation, 0.0f, settings.radius);
        if (GUILayout.Button("Random Particle Radii"))
        {
            particleLife.setRandomRadii();
        }

        GUILayout.Label("creation box center");
        GUILayout.Label("x: " + settings.creationBoxCenter.x);
        settings.creationBoxCenter.x = GUILayout.HorizontalSlider(settings.creationBoxCenter.x, 0.0f, +1.0f);
        GUILayout.Label("y: " + settings.creationBoxCenter.y);
        settings.creationBoxCenter.y = GUILayout.HorizontalSlider(settings.creationBoxCenter.y, 0.0f, +1.0f);
        GUILayout.Label("z: " + settings.creationBoxCenter.z);
        settings.creationBoxCenter.z = GUILayout.HorizontalSlider(settings.creationBoxCenter.z, 0.0f, +1.0f);
        GUILayout.Label("creation box size");
        var newCubeCreationBox = GUILayout.Toggle(m_guiCubeCreationBox, "toggle cube");
        if (newCubeCreationBox && !m_guiCubeCreationBox)
        {
            float mid = (settings.creationBoxSize.x + settings.creationBoxSize.y + settings.creationBoxSize.z) / 3.0f;
            settings.creationBoxSize.x = mid;
            settings.creationBoxSize.y = mid;
            settings.creationBoxSize.z = mid;
        }
        m_guiCubeCreationBox = newCubeCreationBox;

        GUILayout.Label("x: " + settings.creationBoxSize.x);
        settings.creationBoxSize.x = GUILayout.HorizontalSlider(settings.creationBoxSize.x, 0.0f, +1.0f);
        if (m_guiCubeCreationBox && GUI.changed) settings.creationBoxSize = settings.creationBoxSize.x;

        GUILayout.Label("y: " + settings.creationBoxSize.y);
        settings.creationBoxSize.y = GUILayout.HorizontalSlider(settings.creationBoxSize.y, 0.0f, +1.0f);
        if (m_guiCubeCreationBox && GUI.changed) settings.creationBoxSize = settings.creationBoxSize.y;

        GUILayout.Label("z: " + settings.creationBoxSize.z);
        settings.creationBoxSize.z = GUILayout.HorizontalSlider(settings.creationBoxSize.z, 0.0f, +1.0f);
        if (m_guiCubeCreationBox && GUI.changed) settings.creationBoxSize = settings.creationBoxSize.z;


        if (GUILayout.Button("Spawn Particles"))
        {
            particleLife.spawnParticles();
        }
        if (GUILayout.Button("Clear Particles") && particleLife.m_entities.Length > 0)
        {
            m_guiShowConfirmClear = true;

            m_guiWindowRectConfirmClear.center = Camera.main.pixelRect.center;
        }

        if (GUILayout.Button("Respawn Particles"))
        {
            particleLife.respawnParticles();
        }

        GUI_WindowControls(ref m_guiWindowRectParticles, ref m_guiShowParticles, ref m_guiWindowParticlesIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }
    void OnGuiConfirmClearWindow(int winID)
    {
        if (GUILayout.Button("Clear Particles"))
        {
            particleLife.clearParticles();
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
        //m_guiWindowSimulationScrollPosition = GUILayout.BeginScrollView(m_guiWindowSimulationScrollPosition);
        GUILayout.BeginVertical();
        GUILayout.Label("steps per sim sec: " + (((int)(particleLife.simfps * 10 + 0.5f)) / 10.0f));
        GUILayout.Label("sim time per sec: " + (((int)(particleLife.simTimePerSecond * 10 + 0.5f)) / 10.0f));

        GUILayout.Label("min steps per simulated sec: ");
        GUILayout.Label("" + settings.minSimulationStepRate);
        settings.minSimulationStepRate = (int)GUILayout.HorizontalSlider(settings.minSimulationStepRate, 0, 500);

        GUILayout.Label("simulation slowdown: ");
        GUILayout.Label("" + settings.simulationSpeedMultiplicator);
        settings.simulationSpeedMultiplicator = GUILayout.HorizontalSlider(settings.simulationSpeedMultiplicator, 0.0f, 1.0f);
        GUILayout.Label("simulation speedup: ");
        GUILayout.Label("" + settings.simulationSpeedMultiplicator2);
        settings.simulationSpeedMultiplicator2 = GUILayout.HorizontalSlider(settings.simulationSpeedMultiplicator2, 1.0f, +100.0f);


        GUILayout.Space(25f);

        GUILayout.Label("interaction strength: ");
        if (strInteractionStrength == null)
            strInteractionStrength = "" + settings.interactionStrength;
        strInteractionStrength = GUILayout.TextField(strInteractionStrength);
        float interactionStrengthFromStr;

        if (GUI.changed)
            if (float.TryParse(strInteractionStrength, out interactionStrengthFromStr))
            {
                settings.interactionStrength = interactionStrengthFromStr;
                strInteractionStrength = "" + settings.interactionStrength;
            }
        settings.interactionStrength = GUILayout.HorizontalSlider(settings.interactionStrength, -200.0f, 1000.0f);
        if (GUI.changed)
        {
            strInteractionStrength = "" + settings.interactionStrength;
        }

        //GUILayout.Label("" + interactionStrength);
        //interactionStrength = GUILayout.HorizontalSlider(interactionStrength, -2000.0f, 2000.0f);
        settings.flatForce = GUILayout.Toggle(settings.flatForce, "flat force");

        GUILayout.Label("r_smooth: " + settings.r_smooth);
        settings.r_smooth = GUILayout.HorizontalSlider(settings.r_smooth, 0.0f, +10.0f);

        GUILayout.Space(25f);


        if (GUILayout.Button("Movement"))
        {
            m_guiShowMovement = !m_guiShowMovement;
        }
        if (GUILayout.Button("Gravity"))
        {
            m_guiShowGravity = !m_guiShowGravity;
        }

        //GUILayout.Space(25f);
        GUILayout.EndVertical();
        //GUILayout.EndScrollView();

        GUI_WindowControls(ref m_guiWindowRectSimulation, ref m_guiShowSimulation, ref m_guiWindowSimulationIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }
    void OnGUIWindowMovement(int winID)
    {
        GUILayout.Label("max speed: " + settings.maxSpeed);
        settings.maxSpeed = GUILayout.HorizontalSlider(settings.maxSpeed, 0.0f, +200.0f);
        GUILayout.Label("friction per " + (((int)(settings.frictionTime * 10 + 0.5f)) / 10.0f) + "s :");
        GUILayout.Label("velocity will be friction*velocity after frictionTime seconds");
        GUILayout.Label("" + settings.friction);
        settings.friction = GUILayout.HorizontalSlider(settings.friction, 0.0f, 1.0f);
        GUILayout.Label("frictionTime " + settings.frictionTime);
        settings.frictionTime = GUILayout.HorizontalSlider(settings.frictionTime, 0.0f, 2.0f);

        GUI_WindowControls(ref m_guiWindowRectMovement, ref m_guiShowMovement, ref m_guiWindowMovementIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }
    void OnGUIWindowGravity(int winID)
    {
        GUILayout.Label("gravity strength: ");
        if (strGravityStrength == null)
            strGravityStrength = "" + settings.gravityStrength;
        strGravityStrength = GUILayout.TextField(strGravityStrength);
        float gravityStrengthFromStr;

        if (GUI.changed)
            if (float.TryParse(strGravityStrength, out gravityStrengthFromStr))
            {
                settings.gravityStrength = gravityStrengthFromStr;
                strGravityStrength = "" + settings.gravityStrength;
            }
        settings.gravityStrength = GUILayout.HorizontalSlider(settings.gravityStrength, -1e9f, +1e9f);
        if (GUI.changed)
        {
            strGravityStrength = "" + settings.gravityStrength;
        }
        settings.gravityLinear = GUILayout.Toggle(settings.gravityLinear, "linear gravity instead of quadratic");

        GUILayout.Label("max gravity: " + settings.maxGravity);
        settings.maxGravity = GUILayout.HorizontalSlider(settings.maxGravity, 0.0f, +1000.0f);

        GUILayout.Label("gravity target range: ");
        GUILayout.Label("" + settings.gravityTargetRange);
        settings.gravityTargetRange = GUILayout.HorizontalSlider(settings.gravityTargetRange, 0.0f, +1000.0f);

        GUILayout.Label("gravity target");
        GUILayout.Label("x: " + settings.gravityTarget.x);
        settings.gravityTarget.x = GUILayout.HorizontalSlider(settings.gravityTarget.x, 0.0f, +1.0f);
        GUILayout.Label("y: " + settings.gravityTarget.y);
        settings.gravityTarget.y = GUILayout.HorizontalSlider(settings.gravityTarget.y, 0.0f, +1.0f);
        GUILayout.Label("z: " + settings.gravityTarget.z);
        settings.gravityTarget.z = GUILayout.HorizontalSlider(settings.gravityTarget.z, 0.0f, +1.0f);

        GUI_WindowControls(ref m_guiWindowRectGravity, ref m_guiShowGravity, ref m_guiWindowGravityIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }
    public float3 m_guiWorldBoxCenter;
    public float3 m_guiWorldBoxSize;
    public float m_guiWorldBoxSizeMaxValue;
    public bool m_guiCubeWorldBox;
    void OnGUIWindowWorldBounds(int winID)
    {
        settings.drawWorldBounds = GUILayout.Toggle(settings.drawWorldBounds, "draw world bounds");
        m_guiWorldBoxSize = settings.upperBound - settings.lowerBound;
        m_guiWorldBoxCenter = settings.lowerBound + m_guiWorldBoxSize * 0.5f;
        GUILayout.Label("world box center");
        GUILayout.Label("x: " + m_guiWorldBoxCenter.x);
        m_guiWorldBoxCenter.x = GUILayout.HorizontalSlider(m_guiWorldBoxCenter.x, -m_guiWorldBoxSize.x, +m_guiWorldBoxSize.x);
        GUILayout.Label("y: " + m_guiWorldBoxCenter.y);
        m_guiWorldBoxCenter.y = GUILayout.HorizontalSlider(m_guiWorldBoxCenter.y, -m_guiWorldBoxSize.y, +m_guiWorldBoxSize.y);
        GUILayout.Label("z: " + m_guiWorldBoxCenter.z);
        m_guiWorldBoxCenter.z = GUILayout.HorizontalSlider(m_guiWorldBoxCenter.z, -m_guiWorldBoxSize.z, +m_guiWorldBoxSize.z);

        GUILayout.Label("cell size: " + settings.cellSize);
        settings.cellSize = GUILayout.HorizontalSlider(settings.cellSize, 1.0f, 200.0f);
        if (settings.cellSize < types.m_maxRangeMax)
        {
            GUILayout.Label("cell size should not be smaller than max interaction range (" + types.m_maxRangeMax + ") otherwise some interactions will be ignored");
        }
        if (m_guiWorldBoxSizeMaxValue >= 9000 && settings.cellSize < 150.0f) // when its over 9000 its gets dangerous 
        {
            GUILayout.Label("max world size is over 9000!\ncell size should be increased when setting large world bounds otherwise you get out-of-memory, unnecessary interactions will be checked though");
            //cellSize = 200.0f;
        }

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

        settings.lowerBound = m_guiWorldBoxCenter - m_guiWorldBoxSize * 0.5f;
        settings.upperBound = m_guiWorldBoxCenter + m_guiWorldBoxSize * 0.5f;

        GUILayout.Space(5f);
        settings.wrapX = GUILayout.Toggle(settings.wrapX, "wrap x");
        settings.wrapY = GUILayout.Toggle(settings.wrapY, "wrap y");
        settings.wrapZ = GUILayout.Toggle(settings.wrapZ, "wrap z");

        GUILayout.Label("bounce: " + settings.bounce);
        settings.bounce = GUILayout.HorizontalSlider(settings.bounce, -1f, +1f);

        GUILayout.Space(5f);

        if (GUILayout.Button("Spawn Particles"))
        {
            particleLife.spawnParticles();
        }
        if (GUILayout.Button("Clear Particles") && particleLife.m_entities.Length > 0)
        {
            m_guiShowConfirmClear = true;

            m_guiWindowRectConfirmClear.center = Camera.main.pixelRect.center;
        }
        if (GUILayout.Button("Respawn Particles"))
        {
            particleLife.respawnParticles();
        }

        GUI_WindowControls(ref m_guiWindowRectWorldBounds, ref m_guiShowWorldBounds, ref m_guiWindowWorldBoundsIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }

    void OnGUIWindowStats(int winID)
    {
        m_guiWindowStatsScrollPosition = GUILayout.BeginScrollView(m_guiWindowStatsScrollPosition);
        GUILayout.Label("num particles: " + particleLife.m_entities.Length);
        GUILayout.Label("frames per sec: " + (((int)(particleLife.fps * 10 + 0.5f)) / 10.0f));
        GUILayout.Label("steps per sim sec: " + (((int)(particleLife.simfps * 10 + 0.5f)) / 10.0f));
        GUILayout.Label("sim time per sec: " + (((int)(particleLife.simTimePerSecond * 10 + 0.5f)) / 10.0f));
        var dim = settings.upperBound - settings.lowerBound;
        GUILayout.Label("world size: ");
        GUILayout.Label("x:" + dim.x);
        GUILayout.Label("y:" + dim.y);
        GUILayout.Label("z:" + dim.z);
        GUILayout.Label("num grid cells: " + particleLife.m_particleLifeSystem.lastNumCells);

        GUILayout.Label("maxCellSize: " + particleLife.m_particleLifeSystem.maxCellSize);
        GUILayout.Label("numOccupiedCells: " + particleLife.m_particleLifeSystem.numOccupiedCells);
        GUILayout.Label("averageCellSize: " + particleLife.m_particleLifeSystem.averageCellSize);
        GUILayout.Label("numCellAccesses: " + particleLife.m_particleLifeSystem.numCellAccesses);
        GUILayout.Label("numInteractionCandidates: " + particleLife.m_particleLifeSystem.numInteractionCandidates);
        GUILayout.Label("numActualInteractions: " + particleLife.m_particleLifeSystem.numActualInteractions);

        GUILayout.EndScrollView();
        GUI_WindowControls(ref m_guiWindowRectStats, ref m_guiShowStats, ref m_guiWindowStatsIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }

    public string m_guiLibraryTextAreaString = "";
    bool m_guiLibraryWriteDownSettings = true;
    bool m_guiLibraryWriteDownParticles = true;
    bool m_guiLibraryWriteDownTypes = true;
    bool m_guiLibraryWriteDownCamera = true;
    bool m_guiLibraryReadOutSettings = true;
    bool m_guiLibraryReadOutParticles = true;
    bool m_guiLibraryReadOutTypes = true;
    bool m_guiLibraryReadOutCamera = true;

    bool m_guiLibraryWriteDownAdvanced = false;
    int m_guiLibraryWriteDownEncoding = 2;
    bool m_guiLibraryShowExamples = true;
    bool m_guiLibraryShowWriteDown = true;
    bool m_guiLibraryShowReadOut = true;

    void loadExamples()
    {
        //string[] exampleAssets = AssetDatabase.FindAssets("t:TextAsset l:Example");

    }

    [DllImport("__Internal")]
    private static extern void WebGLCopyToClipboard(string text);
    [DllImport("__Internal")]
    private static extern void WebGLRequestClipboardPaste(string objectName);

    void OnGUIWindowLibrary(int winID)
    {
        bool doLoad = false;
        GUILayout.BeginHorizontal();
        {
            #region left column: controls
            GUILayout.BeginVertical(GUILayout.Width(200));
            {

                //GUILayout.BeginHorizontal();
                //m_guiLibraryShowExamples = GUILayout.Toggle(m_guiLibraryShowExamples, "examples");
                //m_guiLibraryShowWriteDown = GUILayout.Toggle(m_guiLibraryShowWriteDown, "save");
                //GUILayout.EndHorizontal();

                GUILayout.Label("Examples");

                m_guiWindowLibraryLeftColScrollPosition = GUILayout.BeginScrollView(m_guiWindowLibraryLeftColScrollPosition);
                if (m_guiLibraryShowExamples)
                {
                    for (int i = 0; i < examples.Length; i++)
                    {
                        //Debug.Log(examples[i].name);
                        var name = examples[i].name;
                        name = name.Replace("_", " ");
                        if (GUILayout.Button(name))
                        {
                            m_guiLibraryTextAreaString = examples[i].text;
                            doLoad = true;
                        }
                    }
                }
                GUILayout.EndScrollView();


            }
            GUILayout.EndVertical();
            #endregion

            #region right column: text area
            GUILayout.BeginVertical(GUILayout.Width(200));
            {
                if (m_guiLibraryShowWriteDown)
                {
                    if (GUILayout.Button("Save to JSON text") && !doLoad)
                    {
                        DataSerializer.EncodingType encoding = (DataSerializer.EncodingType)(
                            m_guiLibraryWriteDownEncoding % (int)DataSerializer.EncodingType.COUNT);

                        SerializeBundle serialize = new SerializeBundle(
                            m_guiLibraryWriteDownSettings ? new SerializeSettings(ref settings) : null,
                            m_guiLibraryWriteDownParticles ? new SerializeParticles(ref particleLife, encoding) : null,
                            m_guiLibraryWriteDownTypes ? new SerializeTypes(ref types, encoding) : null,
                            m_guiLibraryWriteDownCamera ? new SerializeCamera(ref cameraSettings) : null

                        );
                        m_guiLibraryTextAreaString = serialize.ToJson();
                    }
                    GUILayout.BeginHorizontal();
                    //GUILayout.Label("write down");
                    m_guiLibraryWriteDownCamera = GUILayout.Toggle(m_guiLibraryWriteDownCamera, "cam");
                    m_guiLibraryWriteDownSettings = GUILayout.Toggle(m_guiLibraryWriteDownSettings, "settings");
                    m_guiLibraryWriteDownTypes = GUILayout.Toggle(m_guiLibraryWriteDownTypes, "types");
                    m_guiLibraryWriteDownParticles = GUILayout.Toggle(m_guiLibraryWriteDownParticles, "particles");
                    GUILayout.EndHorizontal();
                    m_guiLibraryWriteDownAdvanced = GUILayout.Toggle(m_guiLibraryWriteDownAdvanced, "advanced options");
                    if (m_guiLibraryWriteDownAdvanced)
                    {
                        string[] encoding_options = { "decimal", "hexbin", "zipb64" };
                        m_guiLibraryWriteDownEncoding = GUILayout.Toolbar(m_guiLibraryWriteDownEncoding, encoding_options);
                    }
                }


                GUILayout.BeginHorizontal();
                {
 
                    if (GUILayout.Button("Load from JSON text") || doLoad)
                    {
                        SerializeBundle serialized = SerializeBundle.FromJson(ref m_guiLibraryTextAreaString);
                        if (m_guiLibraryReadOutCamera && serialized.camera != null)
                        {
                            serialized.camera.readOut(ref cameraSettings);
                        }
                        if (m_guiLibraryReadOutSettings && serialized.particleLife != null)
                        {
                            serialized.particleLife.readOut(ref settings);
                        }
                        if (m_guiLibraryReadOutParticles && serialized.particles != null)
                        {
                            serialized.particles.readOut(ref particleLife);
                        }
                        if (m_guiLibraryReadOutTypes && serialized.types != null)
                        {
                            serialized.types.readOut(ref types, ref settings, ref particleLife);
                        }
                    }
                    //GUILayout.FlexibleSpace();

                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                m_guiLibraryReadOutCamera = GUILayout.Toggle(m_guiLibraryReadOutCamera, "cam");
                m_guiLibraryReadOutSettings = GUILayout.Toggle(m_guiLibraryReadOutSettings, "settings");
                m_guiLibraryReadOutTypes = GUILayout.Toggle(m_guiLibraryReadOutTypes, "types");
                m_guiLibraryReadOutParticles = GUILayout.Toggle(m_guiLibraryReadOutParticles, "particles");
                GUILayout.EndHorizontal();


                GUILayout.BeginVertical(GUILayout.MinHeight(200), GUILayout.MaxHeight(512));
                m_guiWindowLibraryScrollPosition = GUILayout.BeginScrollView(m_guiWindowLibraryScrollPosition);
                GUI.SetNextControlName("LibraryText");
                m_guiLibraryTextAreaString = GUILayout.TextArea(m_guiLibraryTextAreaString);
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("select all") )
                {
                    GUI.FocusControl("LibraryText");
                    TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    te.SelectAll();
                }
                if (GUILayout.Button("copy") )
                {
                    //Application.ExternalCall("CopyToClipboard", m_guiLibraryTextAreaString);
                    if (Application.platform == RuntimePlatform.WebGLPlayer)
                    {
                        WebGLCopyToClipboard(m_guiLibraryTextAreaString);
                    }
                    else
                    {
                        GUIUtility.systemCopyBuffer = m_guiLibraryTextAreaString;
                    }
                }
                if (GUILayout.Button("paste") )
                {
                    if (Application.platform == RuntimePlatform.WebGLPlayer)
                    {
                        m_guiLibraryTextAreaWantsClipboardPaste = true;
                        WebGLRequestClipboardPaste(gameObject.name);
                    }
                    else
                    {
                        m_guiLibraryTextAreaString = GUIUtility.systemCopyBuffer;
                    }
                }
                if (GUILayout.Button("cut") )
                {
                    if (Application.platform == RuntimePlatform.WebGLPlayer)
                    {

                        WebGLCopyToClipboard(m_guiLibraryTextAreaString);
                    }
                    else
                    {
                        GUIUtility.systemCopyBuffer = m_guiLibraryTextAreaString;
                    }
                    //Application.ExternalCall("CopyToClipboard", m_guiLibraryTextAreaString);
                    m_guiLibraryTextAreaString = "";
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("clear") || doLoad)
                {
                    m_guiLibraryTextAreaString = "";
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            #endregion
        }
        GUILayout.EndHorizontal();

        GUI_WindowControls(ref m_guiWindowRectLibrary, ref m_guiShowLibrary, ref m_guiWindowLibraryIsResizing);
        GUI.DragWindow(m_guiDragArea);
    }
    bool m_guiLibraryTextAreaWantsClipboardPaste = false;

    public void WebGLReceiveClipboardPaste(string text)
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer) return;
        if (m_guiLibraryTextAreaWantsClipboardPaste)
        {
            m_guiLibraryTextAreaString = text;
            m_guiLibraryTextAreaWantsClipboardPaste = false;
        }
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

}
