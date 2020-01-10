using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Globalization; // for culture neutral number serialization
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;


public class SerializeBundle
{
    public int VERSION { get { return 1; } }
    public int version;

    public SerializeSettings particleLife = null;
    public SerializeParticles particles = null;
    public SerializeTypes types = null;
    public SerializeCamera camera = null;
    public SerializeBundle(SerializeSettings particleLife, SerializeParticles particles, SerializeTypes types, SerializeCamera camera)
    {
        this.version = this.VERSION;
        this.particleLife = particleLife;
        this.particles = particles;
        this.types = types;
        this.camera = camera;
    }
    public static SerializeBundle FromJson([ReadOnly] ref string json)
    {
        var ser = JsonUtility.FromJson<SerializeBundle>(json);
        bool isValidBundle = ser != null && 0 < ser.version && ser.version <= ser.VERSION;
        bool hasParticleLife = ser != null && 0 < ser.particleLife.version && ser.particleLife.version <= ser.particleLife.VERSION;
        bool hasParticles = ser != null && 0 < ser.particles.version && ser.particles.version <= ser.particles.VERSION;
        bool hasTypes = ser != null && 0 < ser.types.version && ser.types.version <= ser.types.VERSION;
        bool hasCamera = ser != null && 0 < ser.camera.version && ser.camera.version <= ser.camera.VERSION;
        if (ser == null || !isValidBundle)
        {
            ser = new SerializeBundle(null, null, null, null);
        }
        // JsonUtility will set values even when it has no values to deserialize
        // set to null for this cases
        if (!hasParticleLife)
        {
            ser.particleLife = null;
        }
        if (!hasParticles)
        {
            ser.particles = null;
        }
        if (!hasTypes)
        {
            ser.types = null;
        }
        if (!hasCamera)
        {
            ser.camera = null;
        }
        return ser;
    }

    public string ToJson()
    {
        string s_particleLife = this.particleLife == null ? "" : this.particleLife.ToJson();
        string s_particles = this.particles == null ? "" : this.particles.ToJson();
        string s_types = this.types == null ? "" : this.types.ToJson();
        string s_camera = this.camera == null ? "" : this.camera.ToJson();
        if (s_particleLife.Length == 0
            && s_particles.Length == 0
            && s_types.Length == 0
            && s_camera.Length == 0)
            return "";
        StringBuilder sb = new StringBuilder(60 
            + s_particleLife.Length 
            + s_particles.Length 
            + s_types.Length 
            + s_camera.Length);
        sb.Append("{");
        sb.Append("\"version\":");
        sb.Append(this.version.ToString(CultureInfo.InvariantCulture));
        if (s_particleLife.Length > 0)
        {
            sb.Append(",");
            sb.Append("\"particleLife\":");
            sb.Append(s_particleLife);
        }
        if (s_particles.Length > 0)
        {
            sb.Append(",");
            sb.Append("\"particles\":");
            sb.Append(s_particles);
        }
        if (s_types.Length > 0)
        {
            sb.Append(",");
            sb.Append("\"types\":");
            sb.Append(s_types);
        }
        if (s_camera.Length > 0)
        {
            sb.Append(",");
            sb.Append("\"camera\":");
            sb.Append(s_camera);
        }
        sb.Append("}");
        return sb.ToString();
    }
}

[Serializable]
public class SerializeCamera
{
    public int VERSION { get { return 1; } }
    public int version;
    
    public float renderDistance;
    public float fieldOfView;
    public float moveSpeed;
    public float3 pos;
    public float4 rotation;

    public SerializeCamera([ReadOnly] ref CameraSettings cam_settings)
    {
        version = this.VERSION;
        this.renderDistance = cam_settings.camera.farClipPlane;
        this.fieldOfView = cam_settings.camera.fieldOfView;
        this.moveSpeed = cam_settings.control.dragSpeed;
        this.pos = cam_settings.transform.position;
        this.rotation.x = cam_settings.transform.rotation.x;
        this.rotation.y = cam_settings.transform.rotation.y;
        this.rotation.z = cam_settings.transform.rotation.z;
        this.rotation.w = cam_settings.transform.rotation.w;
    }
    public void readOut(ref CameraSettings cam_settings)
    {
        if (!(0 < version && version <= VERSION)) return;
        cam_settings.camera.farClipPlane = renderDistance;
        cam_settings.camera.fieldOfView = fieldOfView;
        cam_settings.control.dragSpeed = moveSpeed;
        cam_settings.transform.position = pos;
        cam_settings.transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        cam_settings.control.UpdateFromCurrentCamera();
    }
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    public static SerializeSettings FromJson([ReadOnly] ref string json)
    {
        var result = JsonUtility.FromJson<SerializeSettings>(json);
        bool isValid = result != null && 0 < result.version && result.version <= result.VERSION;
        if (isValid) return result;
        else return null;
    }
}

[Serializable]
public class SerializeSettings
{
    public int VERSION { get { return 1; } }
    public int version;

    // this seems like unnecessary code duplication
    // but hear me out:
    // we have control over what will be serialized and with which version
    // additionally it behaves the same as the other serialize classes for types and particles

    [Header("Particle Types")]
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
    public float simulationSpeedMultiplicator2 = 1.0f;
    public float maxSpeed = 10.0f;
    public float friction = 0.9f;
    public float frictionTime = 0.1f;
    public float interactionStrength = 10.0f;
    public float radius = 5.0f;
    public float radiusVariation = 0.0f;
    public float r_smooth = 2.0f;
    public bool flatForce = false;
    public float cellSize = 50.0f;

    [Header("Gravity")]
    public float3 gravityTarget = 0.5f;
    public float gravityTargetRange = 0.0f;
    public bool gravityLinear = false;
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

    public SerializeSettings([ReadOnly] ref ParticleLifeSettings settings)
    {
        version = this.VERSION;
        numParticleTypes = settings.numParticleTypes;
        attractMean = settings.attractMean;
        attractStd = settings.attractStd;
        rangeMinLower = settings.rangeMinLower;
        rangeMinUpper = settings.rangeMinUpper;
        rangeMaxLower = settings.rangeMaxLower;
        rangeMaxUpper = settings.rangeMaxUpper;
        minSimulationStepRate = settings.minSimulationStepRate;
        simulationSpeedMultiplicator = settings.simulationSpeedMultiplicator;
        simulationSpeedMultiplicator2 = settings.simulationSpeedMultiplicator2;
        maxSpeed = settings.maxSpeed;
        friction = settings.friction;
        frictionTime = settings.frictionTime;
        interactionStrength = settings.interactionStrength;
        radius = settings.radius;
        radiusVariation = settings.radiusVariation;
        r_smooth = settings.r_smooth;
        flatForce = settings.flatForce;
        cellSize = settings.cellSize;
        gravityTarget = settings.gravityTarget;
        gravityTargetRange = settings.gravityTargetRange;
        gravityLinear = settings.gravityLinear;
        gravityStrength = settings.gravityStrength;
        maxGravity = settings.maxGravity;
        lowerBound = settings.lowerBound;
        upperBound = settings.upperBound;
        wrapX = settings.wrapX;
        wrapY = settings.wrapY;
        wrapZ = settings.wrapZ;
        bounce = settings.bounce;
        drawWorldBounds = settings.drawWorldBounds;
        spawnCount = settings.spawnCount;
        creationBoxSize = settings.creationBoxSize;
        creationBoxCenter = settings.creationBoxCenter;
    }
    public void readOut(ref ParticleLifeSettings settings)
    {
        if (!(0 < version && version <= VERSION)) return;
        settings.numParticleTypes = numParticleTypes;
        settings.attractMean = attractMean;
        settings.attractStd = attractStd;
        settings.rangeMinLower = rangeMinLower;
        settings.rangeMinUpper = rangeMinUpper;
        settings.rangeMaxLower = rangeMaxLower;
        settings.rangeMaxUpper = rangeMaxUpper;
        settings.minSimulationStepRate = minSimulationStepRate;
        settings.simulationSpeedMultiplicator = simulationSpeedMultiplicator;
        settings.simulationSpeedMultiplicator2 = simulationSpeedMultiplicator2;
        settings.maxSpeed = maxSpeed;
        settings.friction = friction;
        settings.frictionTime = frictionTime;
        settings.interactionStrength = interactionStrength;
        settings.radius = radius;
        settings.radiusVariation = radiusVariation;
        settings.r_smooth = r_smooth;
        settings.flatForce = flatForce;
        settings.cellSize = cellSize;
        settings.gravityTarget = gravityTarget;
        settings.gravityTargetRange = gravityTargetRange;
        settings.gravityLinear = gravityLinear;
        settings.gravityStrength = gravityStrength;
        settings.maxGravity = maxGravity;
        settings.lowerBound = lowerBound;
        settings.upperBound = upperBound;
        settings.wrapX = wrapX;
        settings.wrapY = wrapY;
        settings.wrapZ = wrapZ;
        settings.bounce = bounce;
        settings.drawWorldBounds = drawWorldBounds;
        settings.spawnCount = spawnCount;
        settings.creationBoxSize = creationBoxSize;
        settings.creationBoxCenter = creationBoxCenter;
    }
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    public static SerializeSettings FromJson([ReadOnly] ref string json)
    {
        var result = JsonUtility.FromJson<SerializeSettings>(json);
        bool isValid = result != null && 0 < result.version && result.version <= result.VERSION;
        if (isValid) return result;
        else return null;
    }
}

[Serializable]
public class SerializeTypes
{
    public int VERSION { get { return 1; } }
    public int version;
    public int numTypes;
    public SerializeNativeArrayFloat attract;
    public SerializeNativeArrayFloat r_min;
    public SerializeNativeArrayFloat r_max;


    public SerializeTypes([ReadOnly] ref ParticleTypes types, DataSerializer.EncodingType encoding)
    {
        version = this.VERSION;
        this.numTypes = types.m_numTypes;
        this.attract = new SerializeNativeArrayFloat(ref types.m_Attract, encoding);
        this.r_min = new SerializeNativeArrayFloat(ref types.m_RangeMin, encoding);
        this.r_max = new SerializeNativeArrayFloat(ref types.m_RangeMax, encoding);
        //this.attract = new SerializeNativeArrayFloat(ref attract, DataEncoder.EncodingType.Base64CodedZippedBinary);
        //this.r_min = new SerializeNativeArrayFloat(ref r_min, DataEncoder.EncodingType.Base64CodedZippedBinary);
        //this.r_max = new SerializeNativeArrayFloat(ref r_max, DataEncoder.EncodingType.Base64CodedZippedBinary);
    }
    public bool readOut(ref ParticleTypes types, ref ParticleLifeSettings settings, ref ParticleLife particles)
    {
        if (!(0 < version && version <= VERSION)) return false;
        NativeArray<float> _attract = new NativeArray<float>();
        NativeArray<float> _r_min = new NativeArray<float>();
        NativeArray<float> _r_max = new NativeArray<float>();
        if (!attract.readOut(ref _attract)) return false;
        if (numTypes * numTypes != _attract.Length) return false;
        if (!r_min.readOut(ref _r_min)) return false;
        if (numTypes * numTypes != _r_min.Length) return false;
        if (!r_max.readOut(ref _r_max)) return false;
        if (numTypes * numTypes != _r_max.Length) return false;
        settings.numParticleTypes = numTypes;
        types.initParticleTypeArrays(numTypes);
        particles.generateMaterials();
        particles.updateParticleTypesAndMaterials();
        types.m_Attract.CopyFrom(_attract);
        types.m_RangeMin.CopyFrom(_r_min);
        types.m_RangeMax.CopyFrom(_r_max);
        _attract.Dispose();
        _r_min.Dispose();
        _r_max.Dispose();
        types.updateMaxRangeMax();
        particles.setSystemTypeArrays();
        return true;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    public static SerializeTypes FromJson([ReadOnly] ref string json)
    {
        var result = JsonUtility.FromJson<SerializeTypes>(json);
        bool isValid = result != null && 0 < result.version && result.version <= result.VERSION;
        if (isValid) return result;
        else return null;
    }
}

[Serializable]
public class SerializeParticles
{
    public int VERSION { get { return 1; } }
    public int version;
    public int numParticles;
    public SerializeNativeArrayInt types;
    public SerializeNativeArrayFloat x;
    public SerializeNativeArrayFloat y;
    public SerializeNativeArrayFloat z;
    public SerializeNativeArrayFloat vx;
    public SerializeNativeArrayFloat vy;
    public SerializeNativeArrayFloat vz;
    public SerializeNativeArrayFloat scale;
    public SerializeParticles([ReadOnly] ref ParticleLife particleLife, DataSerializer.EncodingType encoding)
    {
        version = this.VERSION;

        NativeArray<Translation> translations = particleLife.m_query.ToComponentDataArray<Translation>(Allocator.Persistent);
        NativeArray<Particle> particles = particleLife.m_query.ToComponentDataArray<Particle>(Allocator.Persistent);
        NativeArray<Scale> scales = particleLife.m_query.ToComponentDataArray<Scale>(Allocator.Persistent);

        this.numParticles = translations.Length;
        if (particles.Length < this.numParticles) this.numParticles = particles.Length;
        if (scales.Length < this.numParticles) this.numParticles = scales.Length;

        //NativeArray<SerializeParticle> serializeParticles = new NativeArray<SerializeParticle>(this.numParticles, Allocator.Persistent);
        NativeArray<int> serializeTypes = new NativeArray<int>(this.numParticles, Allocator.Persistent);
        NativeArray<float> serializeX = new NativeArray<float>(this.numParticles, Allocator.Persistent);
        NativeArray<float> serializeY = new NativeArray<float>(this.numParticles, Allocator.Persistent);
        NativeArray<float> serializeZ = new NativeArray<float>(this.numParticles, Allocator.Persistent);
        NativeArray<float> serializeVX = new NativeArray<float>(this.numParticles, Allocator.Persistent);
        NativeArray<float> serializeVY = new NativeArray<float>(this.numParticles, Allocator.Persistent);
        NativeArray<float> serializeVZ = new NativeArray<float>(this.numParticles, Allocator.Persistent);
        NativeArray<float> serializeScale = new NativeArray<float>(this.numParticles, Allocator.Persistent);

        for (int i = 0; i < this.numParticles; i++)
        {
            serializeTypes[i] = particles[i].type;
            serializeX[i] = translations[i].Value.x;
            serializeY[i] = translations[i].Value.y;
            serializeZ[i] = translations[i].Value.z;
            serializeVX[i] = particles[i].vel.x;
            serializeVY[i] = particles[i].vel.y;
            serializeVZ[i] = particles[i].vel.z;
            serializeScale[i] = scales[i].Value;
            //serializeParticles[i] = new SerializeParticle()
            //{
            //    type = particles[i].type,
            //    vel = particles[i].vel,
            //    pos = translations[i].Value,
            //    scale = scales[i].Value,
            //};
        }
        //this.types = new SerializeNativeArrayInt(ref serializeTypes);
        this.types = new SerializeNativeArrayInt(ref serializeTypes, encoding);
        this.x = new SerializeNativeArrayFloat(ref serializeX, encoding);
        this.y = new SerializeNativeArrayFloat(ref serializeY, encoding);
        this.z = new SerializeNativeArrayFloat(ref serializeZ, encoding);
        this.vx = new SerializeNativeArrayFloat(ref serializeVX, encoding);
        this.vy = new SerializeNativeArrayFloat(ref serializeVY, encoding);
        this.vz = new SerializeNativeArrayFloat(ref serializeVZ, encoding);
        this.scale = new SerializeNativeArrayFloat(ref serializeScale, encoding);
        translations.Dispose();
        particles.Dispose();
        scales.Dispose();
        serializeTypes.Dispose();
        serializeX.Dispose();
        serializeY.Dispose();
        serializeZ.Dispose();
        serializeVX.Dispose();
        serializeVY.Dispose();
        serializeVZ.Dispose();
        serializeScale.Dispose();
    }
    public bool readOut(ref ParticleLife particleLife)
    {
        if (!(0 < version && version <= VERSION)) return false;


        if (!(0 < version && version <= VERSION)) return false;
        NativeArray<int> _types = new NativeArray<int>();
        NativeArray<float> _x = new NativeArray<float>();
        NativeArray<float> _y = new NativeArray<float>();
        NativeArray<float> _z = new NativeArray<float>();
        NativeArray<float> _vx = new NativeArray<float>();
        NativeArray<float> _vy = new NativeArray<float>();
        NativeArray<float> _vz = new NativeArray<float>();
        NativeArray<float> _scale = new NativeArray<float>();

        if (!types.readOut(ref _types)) return false;
        if (numParticles != _types.Length) return false;

        if (!x.readOut(ref _x)) return false;
        if (numParticles != _x.Length) return false;

        if (!y.readOut(ref _y)) return false;
        if (numParticles != _y.Length) return false;

        if (!z.readOut(ref _z)) return false;
        if (numParticles != _z.Length) return false;

        if (!vx.readOut(ref _vx)) return false;
        if (numParticles != _vx.Length) return false;

        if (!vy.readOut(ref _vy)) return false;
        if (numParticles != _vy.Length) return false;

        if (!vz.readOut(ref _vz)) return false;
        if (numParticles != _vz.Length) return false;

        if (!scale.readOut(ref _scale)) return false;
        if (numParticles != _scale.Length) return false;

        particleLife.clearParticles();
        particleLife.addParticles(numParticles);

        NativeArray<Translation> translations = new NativeArray<Translation>(numParticles, Allocator.Persistent);
        NativeArray<Particle> particles = new NativeArray<Particle>(numParticles, Allocator.Persistent);
        NativeArray<Scale> scales = new NativeArray<Scale>(numParticles, Allocator.Persistent);

        for (int i=0; i < numParticles; i++)
        {
            translations[i] = new Translation()
            {
                Value = new float3(_x[i], _y[i], _z[i])
            };
            particles[i] = new Particle()
            {
                type = _types[i],
                vel = new float3(_vx[i], _vy[i], _vz[i])
            };
            scales[i] = new Scale()
            {
                Value = _scale[i]
            };
        }

        particleLife.m_query.CopyFromComponentDataArray(translations);
        particleLife.m_query.CopyFromComponentDataArray(particles);
        particleLife.m_query.CopyFromComponentDataArray(scales);

        particleLife.updateParticleTypesAndMaterials();

        _types.Dispose();
        _x.Dispose();
        _y.Dispose();
        _z.Dispose();
        _vx.Dispose();
        _vy.Dispose();
        _vz.Dispose();
        _scale.Dispose();

        translations.Dispose();
        particles.Dispose();
        scales.Dispose();
        return true;
    }
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    public static SerializeParticles FromJson([ReadOnly] ref string json)
    {
        var result = JsonUtility.FromJson<SerializeParticles>(json);
        bool isValid = result != null && 0 < result.version && result.version <= result.VERSION;
        if (isValid) return result;
        else return null;
    }
}

[Serializable]
public struct SerializeParticle
{
    public int type;
    public float3 pos;
    public float3 vel;
    public float scale;
}


