using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class DrawWorldBounds : MonoBehaviour
{
    public bool Enabled = false;
    public ParticleLife particleLife;
    public Material material;

    private float3[] cube_vertices =
    {
        new float3(-0.5f,-0.5f,-0.5f),
        new float3(+0.5f,-0.5f,-0.5f),
        new float3(+0.5f,-0.5f,+0.5f),
        new float3(-0.5f,-0.5f,+0.5f),
        new float3(-0.5f,+0.5f,-0.5f),
        new float3(+0.5f,+0.5f,-0.5f),
        new float3(+0.5f,+0.5f,+0.5f),
        new float3(-0.5f,+0.5f,+0.5f),
    };
    private int2[] cube_lines =
    {
        new int2(0,1),
        new int2(1,2),
        new int2(2,3),
        new int2(3,0),
        new int2(4,5),
        new int2(5,6),
        new int2(6,7),
        new int2(7,4),
        new int2(0,4),
        new int2(1,5),
        new int2(2,6),
        new int2(3,7),
    };
    void drawLines()
    {
        if (particleLife == null || particleLife.settings == null) return;
        float3 dim = particleLife.settings.upperBound - particleLife.settings.lowerBound;
        float3 center = (particleLife.settings.upperBound + particleLife.settings.lowerBound) * 0.5f;
        if (cube_vertices.Length > 0 && cube_lines.Length > 0)
        {
            material.SetPass(0);
            GL.Begin(GL.LINES);
            for (int i = 0; i < cube_lines.Length; i++)
            {
                float3 a = cube_vertices[cube_lines[i].x] * dim + center;
                float3 b = cube_vertices[cube_lines[i].y] * dim + center;
                GL.Vertex3(a.x, a.y, a.z);
                GL.Vertex3(b.x, b.y, b.z);
            }
            GL.End();
        }
    }

    private void Update()
    {
        Enabled = particleLife.settings.drawWorldBounds;
    }

    void OnPostRender()
    {
        if (Enabled) drawLines();
    }
    // To show the lines in the editor
    void OnDrawGizmos()
    {
        if (Enabled) drawLines();
    }
}
