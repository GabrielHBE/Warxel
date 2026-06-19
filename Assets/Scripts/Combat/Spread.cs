using UnityEngine;

public static class Spread
{
    public const float MAX_SPREAD_VALUE = 3;
    public const float MIN_SPREAD_VALUE = 0;

    private static float minModifier = 0.9f;
    private static float maxModifier = 1.1f;

    public static Quaternion CalculateSpreadRotation(Transform shootPosition, float currentSpread)
    {
        return shootPosition.rotation * Quaternion.Euler(new Vector3(
                                                            Random.Range(-currentSpread, currentSpread),
                                                            Random.Range(-currentSpread, currentSpread),
                                                            Random.Range(-currentSpread, currentSpread)
                                                        ));
    }

    public static float AddSpread(float currentSpread, float spreadIncreaser, float maxSpread)
    {
        return Mathf.Clamp(currentSpread + (spreadIncreaser * Random.Range(minModifier, maxModifier)), MIN_SPREAD_VALUE, maxSpread);
    }

    public static float ResetSpread(float currentSpread, float baseSpread = 0, float spreadRecoveryTime = 1f)
    {
        if (currentSpread < 0.01f)
        {
            return 0;
        }
        
        return Mathf.MoveTowards(currentSpread, baseSpread, Time.deltaTime * spreadRecoveryTime);
    }
}
