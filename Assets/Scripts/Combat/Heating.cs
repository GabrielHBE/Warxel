using UnityEngine;

public class Heating
{
    /// <summary>
    /// Aumenta o calor da arma
    /// </summary>
    public static float HandleHeating(HeatValues heatValues, float deltaTime)
    {
        // Usa o heatingRate definido no Inspector
        return Mathf.MoveTowards(
            heatValues.heatState.currentHeat,
            heatValues.maxHeat,
            deltaTime
        );
    }

    /// <summary>
    /// Resfria a arma
    /// </summary>
    public static float HandleCooling(HeatValues heatValues, float deltaTime)
    {
        // Se estiver superaquecido, usa overheatedCoolingRate, senão usa coolingRate
        float coolingRate = isOverheated(heatValues)
            ? heatValues.overheatedCoolingRate
            : heatValues.coolingRate;

        float newHeat = Mathf.MoveTowards(
            heatValues.heatState.currentHeat,
            0,
            coolingRate * deltaTime
        );

        // Se o calor chegou a 0, reseta o estado de superaquecimento
        if (newHeat <= 0f)
        {
            heatValues.heatState.isOverheated = false;
        }

        return newHeat;
    }

    /// <summary>
    /// Verifica se a arma está superaquecida
    /// </summary>
    public static bool isOverheated(HeatValues heatValues)
    {
        // Retorna o estado armazenado OU verifica se o calor atingiu o máximo
        if (heatValues.heatState.isOverheated)
            return true;

        // Se não está marcado como superaquecido, verifica se atingiu o limite
        if (heatValues.heatState.currentHeat >= heatValues.maxHeat)
        {
            heatValues.heatState.isOverheated = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Verifica se pode atirar (não está superaquecido)
    /// </summary>
    public static bool CanShoot(HeatValues heatValues)
    {
        return !heatValues.heatState.isOverheated && heatValues.heatState.currentHeat < heatValues.maxHeat;
    }

    /// <summary>
    /// Reseta o calor para 0
    /// </summary>
    public static void ResetHeat(HeatValues heatValues)
    {
        heatValues.heatState.currentHeat = 0;
        heatValues.heatState.isOverheated = false;
    }

    public static bool ShouldHeat(Firing.FireMode mode, bool isInputHeld)
    {

        switch (mode)
        {
            case Firing.FireMode.Auto:
                return isInputHeld;
            case Firing.FireMode.Single:
                return false;
            case Firing.FireMode.Burst:
                bool isBursting = Firing.IsBursting();
                return isInputHeld || isBursting;
            default:
                return false;
        }
    }

    [System.Serializable]
    public class HeatValues
    {
        public float coolingRate = 5;
        public float overheatedCoolingRate = 2;
        public float maxHeat = 100;
        public HeatState heatState = new HeatState { currentHeat = 0, isOverheated = false };

    }

    public struct HeatState
    {
        public float currentHeat;
        public bool isOverheated;
    }
}