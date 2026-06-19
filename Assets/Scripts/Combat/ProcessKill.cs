using UnityEngine;

public static class ProcessKill
{

    public static void ProcessInfantryKill(GameObject itemUsedToKill, bool isHeadshot, string victimName)
    {
        EliminationMarker.Instance.InstantiateInfantryKillImage();

        if (isHeadshot) AccountManager.Instance.status.AddHeadShotKill();
        if (itemUsedToKill != null)
        {
            UpgradeLevel up = itemUsedToKill.GetComponent<UpgradeLevel>();
            if (up != null) up.AddKill();
        }

        MutualProcess(itemUsedToKill, victimName);
    }

    public static void ProcessVehicleKill(GameObject itemUsedToKill, string[] victimName)
    {
        foreach (string name in victimName)
        {
            EliminationMarker.Instance.InstantiateVehicleKillImage();

            if (itemUsedToKill != null)
            {
                UpgradeLevel up = itemUsedToKill.GetComponent<UpgradeLevel>();
                if (up != null) up.AddKill();
            }
            MutualProcess(itemUsedToKill, name);
        }

    }

    private static void MutualProcess(GameObject itemUsedToKill, string name)
    {
        KillFeedDisplay.Instance.AddKill(AccountManager.Instance.account_name, name, itemUsedToKill != null ? itemUsedToKill.name : "Placeholder");
        AccountManager.Instance.status.AddKill();
        AccountManager.Instance.AddPointsToLevelUp(10);
    }

}