using System.Collections.Generic;

public static class Firing
{
    #region Enums & Structs
    [System.Serializable]
    public enum FireMode
    {
        Auto,
        Single,
        Burst
    }

    [System.Serializable]
    public struct BurstModeSettings
    {
        public int bulletsPerTap;
        public float timeBetweenBursts;
    }

    [System.Serializable]
    public class FiringValues
    {
        public int rateOfFire;
        public int bulletsPerShot = 1;
        public List<FireMode> fireModes;
        public BurstModeSettings burstModeSettings;
        public float interval => 60f / rateOfFire;
    }
    #endregion

    #region State
    private static FireMode crrentMode;
    private static float nextTimeToFire = 0f;
    private static float burstTimer = 0f;
    private static int bulletsShotInCurrentBurst = 0;
    private static bool isBursting = false;
    private static bool isFirstShot = false;
    private static int recoilPositionIndex = -1;
    private static bool hasShotThisFrame = false;
    private static bool isFiring = false;
    private static bool _isInputHeld = false;

    public static void ResetState()
    {
        nextTimeToFire = 0f;
        burstTimer = 0f;
        bulletsShotInCurrentBurst = 0;
        isBursting = false;
        isFirstShot = false;
        recoilPositionIndex = -1;
        hasShotThisFrame = false;
        isFiring = false;
        _isInputHeld = false;
    }
    #endregion

    #region Fire Mode Management
    public static FireMode GetCurrentFireMode()
    {
        return crrentMode;
    }

    public static FireMode SwitchFireMode(List<FireMode> availableModes)
    {
        if (availableModes == null || availableModes.Count == 0)
            return FireMode.Auto;

        int currentIndex = availableModes.IndexOf(crrentMode);
        
        if (currentIndex == -1)
        {
            crrentMode = availableModes[0];
        }
        else
        {
            currentIndex = (currentIndex + 1) % availableModes.Count;
            crrentMode = availableModes[currentIndex];
        }

        if (crrentMode != FireMode.Burst)
        {
            isBursting = false;
            bulletsShotInCurrentBurst = 0;
            burstTimer = 0f;
        }

        recoilPositionIndex = -1;
        hasShotThisFrame = false;
        nextTimeToFire = 0f;
        isFiring = false;
        _isInputHeld = false;

        return crrentMode;
    }

    public static bool CanSwitchFireMode(List<FireMode> availableModes)
    {
        return availableModes != null && availableModes.Count > 1;
    }
    #endregion

    #region Shooting Logic
    public struct ShootResult
    {
        public bool shouldShoot;
        public bool didShoot;
        public bool isFirstShot;
        public int recoilIndex;
        public bool shouldResetShotState;
    }

    public static ShootResult ProcessShooting(
        FiringValues values,
        bool isInputHeld,
        bool isInputPressed,
        bool isReloading,
        bool isRolling,
        bool isDead,
        int currentAmmo,
        float deltaTime)
    {
        var result = new ShootResult
        {
            shouldShoot = false,
            didShoot = false,
            isFirstShot = isFirstShot,
            recoilIndex = -1,
            shouldResetShotState = false
        };

        hasShotThisFrame = false;
        _isInputHeld = isInputHeld;

        if (isReloading || isRolling || isDead || currentAmmo <= 0)
        {
            result.shouldResetShotState = true;
            isFiring = false;
            return result;
        }

        switch (crrentMode)
        {
            case FireMode.Auto:
                ProcessAutoFire(values, isInputHeld, ref result);
                break;
            case FireMode.Single:
                ProcessSingleFire(values, isInputPressed, ref result);
                break;
            case FireMode.Burst:
                ProcessBurstFire(values, isInputPressed, deltaTime, ref result);
                break;
        }

        if (result.shouldShoot)
        {
            isFirstShot = true;
            hasShotThisFrame = true;
            isFiring = true;
        }

        return result;
    }

    private static void ProcessAutoFire(FiringValues settings, bool isInputHeld, ref ShootResult result)
    {
        if (isInputHeld)
        {
            isFiring = true;
            
            if (nextTimeToFire <= 0f && !hasShotThisFrame)
            {
                result.shouldShoot = true;
                result.didShoot = true;
                nextTimeToFire = settings.interval;
            }
        }
        else
        {
            isFiring = false;
            
            if (nextTimeToFire <= 0f)
            {
                result.shouldResetShotState = true;
                recoilPositionIndex = -1;
                isFirstShot = false;
            }

        }
    }

    private static void ProcessSingleFire(FiringValues settings, bool isInputPressed, ref ShootResult result)
    {
        if (isInputPressed && nextTimeToFire <= 0f && !hasShotThisFrame)
        {
            result.shouldShoot = true;
            result.didShoot = true;
            nextTimeToFire = settings.interval;
            isFiring = true;
        }
        else if (!isInputPressed && nextTimeToFire <= 0f)
        {
            result.shouldResetShotState = true;
            recoilPositionIndex = -1;
            isFirstShot = false;
            isFiring = false;
        }
    }

    private static void ProcessBurstFire(FiringValues settings, bool isInputPressed, float deltaTime, ref ShootResult result)
    {
        if (!isBursting && isInputPressed && nextTimeToFire <= 0f)
        {
            isBursting = true;
            bulletsShotInCurrentBurst = 0;
            burstTimer = 0f;
            isFiring = true;
        }
        else if (!isBursting)
        {
            result.shouldResetShotState = true;
            recoilPositionIndex = -1;
            isFirstShot = false;
            isFiring = false;
            return;
        }

        if (isBursting)
        {
            burstTimer -= deltaTime;
            if (burstTimer < 0) burstTimer = 0;

            if (burstTimer <= 0f && bulletsShotInCurrentBurst < settings.burstModeSettings.bulletsPerTap && !hasShotThisFrame)
            {
                result.shouldShoot = true;
                result.didShoot = true;
                bulletsShotInCurrentBurst++;
                burstTimer = settings.interval;
                isFiring = true;
            }

            if (bulletsShotInCurrentBurst >= settings.burstModeSettings.bulletsPerTap)
            {
                isBursting = false;
                nextTimeToFire = settings.burstModeSettings.timeBetweenBursts;
                recoilPositionIndex = -1;
                isFirstShot = false;
                if (!isInputPressed)
                {
                    isFiring = false;
                }
            }
        }
    }
    #endregion

    #region Recoil Management
    public static int GetNextRecoilIndex(int patternLength)
    {
        recoilPositionIndex = (recoilPositionIndex + 1) % patternLength;
        return recoilPositionIndex;
    }

    public static void ResetRecoilIndex()
    {
        recoilPositionIndex = -1;
    }

    public static bool IsFirstShot()
    {
        return isFirstShot;
    }
    #endregion

    #region Utility Methods
    public static float GetTimeToNextFire()
    {
        return nextTimeToFire;
    }

    public static bool IsBursting()
    {
        return isBursting;
    }

    public static int GetBulletsInBurst()
    {
        return bulletsShotInCurrentBurst;
    }

    public static float GetBurstTimer()
    {
        return burstTimer;
    }

    public static void UpdateTimeToFire(float deltaTime)
    {
        if (nextTimeToFire > 0)
        {
            nextTimeToFire -= deltaTime;
            if (nextTimeToFire < 0) nextTimeToFire = 0;
            
            if (nextTimeToFire <= 0)
            {
                if (crrentMode == FireMode.Auto && !_isInputHeld)
                {
                    isFiring = false;
                }
            }
        }
    }

    public static bool IsFiring()
    {
        return isFiring;
    }
    #endregion
}