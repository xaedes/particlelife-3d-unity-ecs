using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct Particle : IComponentData
{
    public int type;
    public float3 vel;
}
