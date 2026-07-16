using System.Collections.Generic;
using UnityEngine;

namespace ProcessReload
{
    public static class Reload
    {
        [System.Serializable]
        public class ReloadValues
        {
            public bool isSingleReload;
            public float timeToTransferAmmo;
            public float reloadTime;
            public int magCount;
            public int bulletsPerMag;
            [HideInInspector] public List<int> mags = new List<int>();

            public void PopulateMags()
            {
                for (int i = 0; i < magCount; i++)
                {
                    mags.Add(bulletsPerMag);
                }
            }

            public int GetCurrentMagAmmo() => mags.Count > 0 ? mags[^1] : 0;
            public int GetTotalReserveAmmo()
            {
                int total = 0;
                for (int i = 0; i < mags.Count - 1; i++)
                {
                    total += mags[i];
                }
                return total;
            }

            public bool IsMagazineEmpty() => GetCurrentMagAmmo() == 0;
            public bool IsMagazineFull() => GetCurrentMagAmmo() >= bulletsPerMag;

            public int FindMagazineWithMostAmmo()
            {
                int max = 0;
                int index = 0;
                for (int i = 0; i < magCount; i++)
                {
                    if (mags[i] > max)
                    {
                        max = mags[i];
                        index = i;
                    }
                }
                return index;
            }

            public int FindMagazineWithLeastAmmo()
            {
                int min = int.MaxValue;
                int index = -1;

                for (int i = 0; i < mags.Count; i++)
                {
                    int mag = mags[i];
                    if (mag > 0 && mag < min)
                    {
                        min = mag;
                        index = i;
                    }
                }

                return index;
            }

            public int FindMagazineWithMostSpace()
            {
                int max = mags.Count > 0 ? mags[0] : 0;
                int index = 0;

                for (int i = 1; i < mags.Count; i++)
                {
                    int mag = mags[i];
                    if (mag > max && mag < bulletsPerMag)
                    {
                        max = mag;
                        index = i;
                    }
                }

                return max >= bulletsPerMag ? -1 : index;
            }
        }

        public static class ReloadLogic
        {
            public struct ReloadResult
            {
                public bool isReloading;
                public bool canShoot;
                public float remainingCooldown;
                public bool shouldFinishReload;
            }

            public static bool CanStartReload(ReloadValues reloadValues, bool isFiring, bool isReloading, bool isRolling, int reserveAmmo)
            {
                if (isFiring || isReloading || isRolling)
                    return false;

                if (reserveAmmo <= 0)
                    return false;

                if (reloadValues.IsMagazineFull())
                    return false;

                return true;
            }

            public static ReloadResult ProcessStandardReload(
                ReloadValues reloadValues,
                float currentCooldown,
                float deltaTime,
                bool isLastBullet)
            {
                ReloadResult result = new ReloadResult
                {
                    isReloading = true,
                    canShoot = false,
                    remainingCooldown = currentCooldown - deltaTime,
                    shouldFinishReload = false
                };

                if (result.remainingCooldown <= 0)
                {
                    // Transfer ammo from reserve to current mag
                    TransferMagazineAmmo(reloadValues, isLastBullet);
                    result.isReloading = false;
                    result.canShoot = true;
                    result.shouldFinishReload = true;
                    result.remainingCooldown = 0;
                }

                return result;
            }

            public static void TransferMagazineAmmo(ReloadValues reloadValues, bool isLastBullet)
            {
                if (reloadValues.mags.Count < 2)
                    return;

                int maxIndex = reloadValues.FindMagazineWithMostAmmo();
                if (maxIndex < 0 || maxIndex == reloadValues.mags.Count - 1)
                    return;

                int maxAmmo = reloadValues.mags[maxIndex];
                int currentAmmo = reloadValues.mags[^1];
                int temp = currentAmmo;

                // Fill current mag with the ammo from the fullest reserve mag
                reloadValues.mags[^1] = maxAmmo;

                // Put the old current mag ammo into the reserve mag
                if (!isLastBullet)
                {
                    reloadValues.mags[maxIndex] = temp;
                }
                else
                {
                    // Special case: if current mag was empty, don't add extra bullet
                    reloadValues.mags[maxIndex] = 0;
                }
            }

            public static bool ProcessSingleReload(
                ReloadValues reloadValues,
                bool isReloading,
                bool canReload,
                bool isFiring,
                out bool shouldContinueReloading)
            {
                shouldContinueReloading = false;

                if (!isReloading || !canReload || isFiring)
                    return false;

                if (reloadValues.IsMagazineFull())
                    return false;

                shouldContinueReloading = true;
                return true;
            }

            public static void TransferBulletBetweenMags(ReloadValues reloadValues)
            {
                if (reloadValues.mags.Count < 2)
                    return;

                int fromIndex = reloadValues.FindMagazineWithLeastAmmo();
                int toIndex = reloadValues.FindMagazineWithMostSpace();

                if (fromIndex == -1 || toIndex == -1 || fromIndex == toIndex)
                    return;

                int fromAmmo = reloadValues.mags[fromIndex];
                int toAmmo = reloadValues.mags[toIndex];
                int spaceAvailable = reloadValues.bulletsPerMag - toAmmo;

                int transferAmount = Mathf.Min(fromAmmo, spaceAvailable);

                if (transferAmount > 0)
                {
                    reloadValues.mags[fromIndex] -= transferAmount;
                    reloadValues.mags[toIndex] += transferAmount;
                }
            }

            public static float CalculateReloadTime(ReloadValues reloadValues, bool isEmpty)
            {
                float totalTime = reloadValues.reloadTime;
                if (isEmpty)
                {
                    totalTime += Weapon.LAST_MAG_RELOAD_TIMER_INCREASER; // Additional time for empty mag reload
                }
                return totalTime;
            }

            public static bool IsReloadPossible(ReloadValues reloadValues)
            {
                if (reloadValues.mags.Count < 2)
                    return false;

                int reserveAmmo = reloadValues.GetTotalReserveAmmo();
                int currentAmmo = reloadValues.GetCurrentMagAmmo();

                // Can reload if there's reserve ammo and current mag isn't full
                return reserveAmmo > 0 && currentAmmo < reloadValues.bulletsPerMag;
            }
        }
    }
}