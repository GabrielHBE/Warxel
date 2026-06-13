using UnityEngine;

public static class Spread
{
    private static float minModifier = 0.9f;
    private static float maxModifier = 1.1f;

    public static Quaternion CalculateSpreadRotation(Transform shootPosition, float currentSpread)
    {
        Vector3 randomSpread = new Vector3(
            Random.Range(-currentSpread, currentSpread),
            Random.Range(-currentSpread, currentSpread),
            Random.Range(-currentSpread, currentSpread)
        );

        return shootPosition.rotation * Quaternion.Euler(randomSpread);
    }

    public static float AddSpread(float currentSpread, float spreadIncreaser, float maxSpread)
    {
        float randomizedIncreaser = spreadIncreaser * Random.Range(minModifier, maxModifier);

        if(currentSpread + randomizedIncreaser > maxSpread)
        {
            return maxSpread;
        }

        return currentSpread + randomizedIncreaser;
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
