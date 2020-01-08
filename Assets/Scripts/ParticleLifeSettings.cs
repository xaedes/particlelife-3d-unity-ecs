using UnityEngine;
using Unity.Mathematics;
using System.Collections;


public class ParticleLifeSettings : MonoBehaviour
{
    #region Properties
    [Header("Particle Types")]
    public int numParticleTypes = 10;
    public float attractMean = 0;
    public float attractStd = 4.0f;
    public float rangeMinLower = 0.0f;
    public float rangeMinUpper = 20.0f;
    public float rangeMaxLower = 10.0f;
    public float rangeMaxUpper = 50.0f;

    [Header("Simulation Properties")]
    public float minSimulationStepRate = 60.0f;
    public float simulationSpeedMultiplicator = 1.0f;
    public float simulationSpeedMultiplicator2 = 2.0f;
    public float maxSpeed = 100.0f;
    public float friction = 0.995f;
    public float frictionTime = 0.1f;
    public float interactionStrength = 100.0f;
    public float radius = 4.0f;
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
    public float3 lowerBound = -1000;
    public float3 upperBound = +1000;
    public bool wrapX = true;
    public bool wrapY = true;
    public bool wrapZ = true;
    public float bounce = 1.0f;
    public bool drawWorldBounds = true;

    [Header("Particle Instantiation")]
    public int spawnCount = 500;
    public float3 creationBoxSize = 0.25f;
    public float3 creationBoxCenter = 0.5f;

    #endregion

}
