using System;
using System.Collections.Generic;

namespace Game.Meta.Barracks
{
    public interface IBarracksService
    {
        bool IsClockManipulated { get; }
        int GetBuildingLevel(BarracksBuildingType type);
        bool IsUnderConstruction(BarracksBuildingType type);
        TimeSpan GetRemainingConstructionTime(BarracksBuildingType type);
        int GetInstantCompleteMedalCost(BarracksBuildingType type);

        int GetUpgradeCoinCost(BarracksBuildingType type);
        int GetUpgradeRecruitCost(BarracksBuildingType type);
        TimeSpan GetUpgradeDuration(BarracksBuildingType type);

        bool CanStartUpgrade(BarracksBuildingType type, out string failReason);
        bool TryStartUpgrade(BarracksBuildingType type);
        bool TrySpeedupWithAd(BarracksBuildingType type);
        bool TryInstantCompleteWithMedals(BarracksBuildingType type);
        bool TryCompleteUpgrade(BarracksBuildingType type);

        int GetPendingOfflineCoins();
        bool TryCollectOfflineCoins(bool doubleWithAd, out int collectedCoins);

        int GetInitialSquadBonus();
        float GetBaseDamageMultiplier();
        int CalculateRecoveredSoldiers(int soldiersLost);
        IReadOnlyList<string> GetUnlockedAmmoPerks();
    }
}
