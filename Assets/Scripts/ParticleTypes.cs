using UnityEngine;
using Unity.Mathematics;
using System.Collections;
using Unity.Collections;


public class ParticleTypes : MonoBehaviour
{
    public int m_numTypes = 0;
    public NativeArray<Color> m_Colors;
    public NativeArray<float> m_Attract;
    public NativeArray<float> m_RangeMin;
    public NativeArray<float> m_RangeMax;
    
    public float m_maxRangeMax;

    public void OnDisable()
    {
        if(m_Colors.IsCreated) m_Colors.Dispose();
        if (m_Attract.IsCreated) m_Attract.Dispose();
        if (m_RangeMin.IsCreated) m_RangeMin.Dispose();
        if (m_RangeMax.IsCreated) m_RangeMax.Dispose();
    }

    public static Color fromHSV(float h, float s, float v)
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

    public void initParticleTypeArrays(int num)
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
    }

    public void updateMaxRangeMax()
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
            for (int k = i; k < m_numTypes; k++)
            {
                int coord = i * m_numTypes + k;
                if (m_RangeMax[coord] > m_maxRangeMax)
                    m_maxRangeMax = m_RangeMax[coord];
            }
        }
    }

    public void setRandomTypes([ReadOnly] ref ParticleLifeSettings settings)
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
        m_maxRangeMax = 0;
        for (int i = 0; i < m_numTypes; i++)
        {
            for (int k = 0; k < m_numTypes; k++)
            {
                int coord = i * m_numTypes + k;
                int icoord = k * m_numTypes + i;
                if (i == k)
                {
                    // coord == icoord
                    m_Attract[coord] = -math.abs(RandomTools.nextGaussianFloat() * settings.attractStd + settings.attractMean);
                    //m_Attract[coord] = (RandomTools.nextGaussianFloat(r) * attractStd + attractMean);
                    m_RangeMin[coord] = settings.radius * 2;
                }
                else
                {
                    m_Attract[coord] = RandomTools.nextGaussianFloat() * settings.attractStd + settings.attractMean;
                    m_Attract[icoord] = RandomTools.nextGaussianFloat() * settings.attractStd + settings.attractMean;
                    m_RangeMin[coord] = math.max(settings.radius * 2, UnityEngine.Random.Range(settings.rangeMinLower, settings.rangeMinUpper));
                }
                m_RangeMax[coord] = math.max(m_RangeMin[coord], UnityEngine.Random.Range(settings.rangeMaxLower, settings.rangeMaxUpper));
                m_RangeMin[icoord] = m_RangeMin[coord];
                m_RangeMax[icoord] = m_RangeMax[coord];
                if (m_RangeMax[coord] > m_maxRangeMax)
                    m_maxRangeMax = m_RangeMax[coord];
            }
        }
    }
}
