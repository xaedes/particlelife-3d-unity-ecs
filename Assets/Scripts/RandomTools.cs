using UnityEngine;
using System.Collections;
using Unity.Mathematics;

public class RandomTools
{
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
}
