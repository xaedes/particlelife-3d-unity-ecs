using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class DrawWorldBounds : MonoBehaviour
{
    public bool Enabled = false;
    public ParticleLife particleLife;

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
        float3 dim = particleLife.upperBound - particleLife.lowerBound;
        float3 center = (particleLife.upperBound + particleLife.lowerBound) * 0.5f;
        if (cube_vertices.Length > 0 && cube_lines.Length > 0)
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(0f, 0f, 0f));
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
