using UnityEngine;

public static class Spread
{
    public static Quaternion CalculateSpreadRotation(Transform shootPosition, float currentSpread)
    {
        Vector3 randomSpread = new Vector3(
            Random.Range(-currentSpread, currentSpread),
            Random.Range(-currentSpread, currentSpread),
            Random.Range(-currentSpread, currentSpread)
        ) / 10;

        return shootPosition.rotation * Quaternion.Euler(randomSpread);
    }

    public static float AddSpread(float currentSpread, float spreadIncreaser)
    {
        float minModifier = 0.9f;
        float maxModifier = 1.1f;

        float randomizedIncreaser = spreadIncreaser * Random.Range(minModifier, maxModifier);

        return currentSpread + randomizedIncreaser;
    }

    public static float ResetSpread(float currentSpread, float baseSpread = 0, float spreadRecoveryTime = 1f)
    {
        if (currentSpread < 0.01f)
        {
            return 0;
        }

        float newSpread = Mathf.Lerp(currentSpread, baseSpread, Time.deltaTime * spreadRecoveryTime);
        return newSpread;
    }
}
