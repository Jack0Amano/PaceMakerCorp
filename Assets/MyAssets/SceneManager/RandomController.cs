using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomController
{
    /// <summary>
    /// 確率判定
    /// </summary>
    /// <param name="fPercent">確率 0~1</param>
    /// <returns>当選結果 [true]当選</returns>
    public bool Probability(float fPercent)
    {
        float fProbabilityRate = UnityEngine.Random.value;

        if (fPercent == 1f&& fProbabilityRate == fPercent)
        {
            return true;
        }
        else if (fProbabilityRate < fPercent)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool StaticProbability(float fPercent)
    {
        float fProbabilityRate = UnityEngine.Random.value;

        if (fPercent == 1f && fProbabilityRate == fPercent)
        {
            return true;
        }
        else if (fProbabilityRate < fPercent)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
