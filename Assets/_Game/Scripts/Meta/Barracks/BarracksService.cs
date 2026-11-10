using System;
using System.Collections.Generic;
using Game.Core;
using Game.Meta.Barracks.Events;
using Game.Meta.Save;
using Game.Meta.Time;
using Game.Meta.Wallet;

namespace Game.Meta.Barracks
{
    public class BarracksService : IBarracksService
    {
        private readonly ITimeProvider _timeProvider;
        private readonly IWallet _wallet;
        private readonly BarracksCatalog _catalog;
        private readonly BarracksState _state;
        private readonly IEventBus _eventBus;

        public bool IsClockManipulated => _timeProvider.IsClockManipulated;

        public BarracksService(
            ITimeProvider timeProvider,
            IWallet wallet,
            BarracksCatalog catalog = null,
            BarracksState state = null,
            IEventBus eventBus = null)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _catalog = catalog ?? BarracksCatalog.CreateDefault();
            _state = state ?? new BarracksState();
            _eventBus = eventBus;

            if (_state.BuildingLevels == null || _state.BuildingLevels.Count == 0)
            {
                _state.BuildingLevels = new Dictionary<string, int>
                {
                    ["Recruitment"] = 1,
                    ["Armory"] = 1,
                    ["Canteen"] = 1,
                    ["Infirmary"] = 1,
                    ["Ammunition"] = 1
                };
            }

            if (_state.LastCanteenCollectionUtcTicks <= 0)
            {
                _state.LastCanteenCollectionUtcTicks = _timeProvider.UtcNow.Ticks;
            }
        }

        public void SetBuildingLevel(BarracksBuildingType type, int level)
        {
            _state.SetLevel(type.ToString(), level);
        }

        public int GetBuildingLevel(BarracksBuildingType type)
        {
            return _state.GetLevel(type.ToString());
        }

        public bool IsUnderConstruction(BarracksBuildingType type)
        {
            return !string.IsNullOrEmpty(_state.ActiveConstructionType) &&
                   string.Equals(_state.ActiveConstructionType, type.ToString(), StringComparison.Ordinal);
        }

        public TimeSpan GetRemainingConstructionTime(BarracksBuildingType type)
        {
            if (!IsUnderConstruction(type))
            {
                return TimeSpan.Zero;
            }

            DateTime referenceTime = IsClockManipulated
                ? new DateTime(_timeProvider.LastSeenUtcTicks, DateTimeKind.Utc)
                : _timeProvider.UtcNow;

            DateTime endUtc = new DateTime(_state.ConstructionEndUtcTicks, DateTimeKind.Utc);
            TimeSpan remaining = endUtc - referenceTime;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public int GetInstantCompleteMedalCost(BarracksBuildingType type)
        {
            if (!IsUnderConstruction(type))
            {
                return 0;
            }

            double remainingMinutes = GetRemainingConstructionTime(type).TotalMinutes;
            return Math.Max(5, (int)Math.Ceiling(remainingMinutes / 5.0));
        }

        public int GetUpgradeCoinCost(BarracksBuildingType type)
        {
            int nextLevel = GetBuildingLevel(type) + 1;
            BarracksBuildingDefinition def = _catalog.Get(type);
            BarracksBuildingLevelDefinition levelDef = def?.GetLevel(nextLevel);
            return levelDef?.CoinCost ?? 0;
        }

        public int GetUpgradeRecruitCost(BarracksBuildingType type)
        {
            int nextLevel = GetBuildingLevel(type) + 1;
            BarracksBuildingDefinition def = _catalog.Get(type);
            BarracksBuildingLevelDefinition levelDef = def?.GetLevel(nextLevel);
            return levelDef?.RecruitCost ?? 0;
        }

        public TimeSpan GetUpgradeDuration(BarracksBuildingType type)
        {
            int nextLevel = GetBuildingLevel(type) + 1;
            BarracksBuildingDefinition def = _catalog.Get(type);
            BarracksBuildingLevelDefinition levelDef = def?.GetLevel(nextLevel);
            int seconds = levelDef?.DurationSeconds ?? 0;
            return seconds > 0 ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
        }

        public bool CanStartUpgrade(BarracksBuildingType type, out string failReason)
        {
            if (IsClockManipulated)
            {
                failReason = "Relógio do sistema inconsistente.";
                return false;
            }

            if (!string.IsNullOrEmpty(_state.ActiveConstructionType))
            {
                failReason = "Já existe uma obra em andamento no Quartel.";
                return false;
            }

            BarracksBuildingDefinition def = _catalog.Get(type);
            int maxLevel = def?.MaxLevel ?? 5;
            int currentLevel = GetBuildingLevel(type);
            if (currentLevel >= maxLevel)
            {
                failReason = "Construção já atingiu o nível máximo.";
                return false;
            }

            int coinCost = GetUpgradeCoinCost(type);
            int recruitCost = GetUpgradeRecruitCost(type);

            if (coinCost > 0 && _wallet.GetBalance(CurrencyKind.Coins) < coinCost)
            {
                failReason = "Moedas insuficientes.";
                return false;
            }

            if (recruitCost > 0 && _wallet.GetBalance(CurrencyKind.Recruits) < recruitCost)
            {
                failReason = "Recrutas insuficientes.";
                return false;
            }

            failReason = null;
            return true;
        }

        public bool TryStartUpgrade(BarracksBuildingType type)
        {
            if (!CanStartUpgrade(type, out _))
            {
                return false;
            }

            int coinCost = GetUpgradeCoinCost(type);
            int recruitCost = GetUpgradeRecruitCost(type);

            if (coinCost > 0 && _wallet.GetBalance(CurrencyKind.Coins) < coinCost)
            {
                return false;
            }

            if (recruitCost > 0 && _wallet.GetBalance(CurrencyKind.Recruits) < recruitCost)
            {
                return false;
            }

            if (coinCost > 0)
            {
                if (!_wallet.TrySpend(CurrencyKind.Coins, coinCost, "BarracksUpgrade"))
                {
                    return false;
                }
            }

            if (recruitCost > 0)
            {
                if (!_wallet.TrySpend(CurrencyKind.Recruits, recruitCost, "BarracksUpgrade"))
                {
                    if (coinCost > 0)
                    {
                        _wallet.Add(CurrencyKind.Coins, coinCost, "BarracksUpgradeRollback");
                    }
                    return false;
                }
            }

            int nextLevel = GetBuildingLevel(type) + 1;
            TimeSpan duration = GetUpgradeDuration(type);

            _state.ActiveConstructionType = type.ToString();
            _state.ConstructionEndUtcTicks = _timeProvider.UtcNow.Add(duration).Ticks;
            _state.TargetLevel = nextLevel;

            return true;
        }

        public bool TrySpeedupWithAd(BarracksBuildingType type)
        {
            if (IsClockManipulated)
            {
                return false;
            }

            if (!IsUnderConstruction(type))
            {
                return false;
            }

            if (GetRemainingConstructionTime(type) <= TimeSpan.Zero)
            {
                return false;
            }

            DateTime today = _timeProvider.UtcNow.Date;
            DateTime lastAdUsageDate = _state.LastAdUsageDateUtcTicks > 0
                ? new DateTime(_state.LastAdUsageDateUtcTicks, DateTimeKind.Utc).Date
                : DateTime.MinValue;

            if (today > lastAdUsageDate)
            {
                _state.AdSpeedupsUsedToday = 0;
                _state.LastAdUsageDateUtcTicks = today.Ticks;
            }

            if (_state.AdSpeedupsUsedToday >= 3)
            {
                return false;
            }

            _state.AdSpeedupsUsedToday++;
            _state.LastAdUsageDateUtcTicks = today.Ticks;

            DateTime currentEnd = new DateTime(_state.ConstructionEndUtcTicks, DateTimeKind.Utc);
            DateTime newEnd = currentEnd.Subtract(TimeSpan.FromMinutes(30));
            if (newEnd < _timeProvider.UtcNow)
            {
                newEnd = _timeProvider.UtcNow;
            }

            _state.ConstructionEndUtcTicks = newEnd.Ticks;
            return true;
        }

        public bool TryInstantCompleteWithMedals(BarracksBuildingType type)
        {
            if (IsClockManipulated)
            {
                return false;
            }

            if (!IsUnderConstruction(type))
            {
                return false;
            }

            int medalCost = GetInstantCompleteMedalCost(type);
            if (!_wallet.TrySpend(CurrencyKind.Medals, medalCost, "BarracksInstantComplete"))
            {
                return false;
            }

            int targetLevel = _state.TargetLevel;
            _state.SetLevel(type.ToString(), targetLevel);
            _state.ActiveConstructionType = null;
            _state.ConstructionEndUtcTicks = 0;
            _state.TargetLevel = 0;

            _eventBus?.Publish(new BuildingUpgradeCompletedEvent(type, targetLevel));
            return true;
        }

        public bool TryCompleteUpgrade(BarracksBuildingType type)
        {
            if (IsClockManipulated)
            {
                return false;
            }

            if (!IsUnderConstruction(type))
            {
                return false;
            }

            DateTime endUtc = new DateTime(_state.ConstructionEndUtcTicks, DateTimeKind.Utc);
            if (_timeProvider.UtcNow < endUtc)
            {
                return false;
            }

            int targetLevel = _state.TargetLevel;
            _state.SetLevel(type.ToString(), targetLevel);
            _state.ActiveConstructionType = null;
            _state.ConstructionEndUtcTicks = 0;
            _state.TargetLevel = 0;

            _eventBus?.Publish(new BuildingUpgradeCompletedEvent(type, targetLevel));
            return true;
        }

        public int GetPendingOfflineCoins()
        {
            if (IsClockManipulated)
            {
                return 0;
            }

            DateTime lastCollection = _state.LastCanteenCollectionUtcTicks > 0
                ? new DateTime(_state.LastCanteenCollectionUtcTicks, DateTimeKind.Utc)
                : _timeProvider.UtcNow;

            TimeSpan elapsed = _timeProvider.UtcNow - lastCollection;
            if (elapsed.TotalSeconds <= 0)
            {
                return 0;
            }

            double cappedHours = Math.Min(8.0, elapsed.TotalHours);
            BarracksBuildingDefinition canteenDef = _catalog.Get(BarracksBuildingType.Canteen);
            int level = GetBuildingLevel(BarracksBuildingType.Canteen);
            BarracksBuildingLevelDefinition levelDef = canteenDef?.GetLevel(level);
            int coinsPerHour = levelDef?.CoinsPerHour ?? (50 * level);

            return (int)Math.Floor(cappedHours * coinsPerHour);
        }

        public bool TryCollectOfflineCoins(bool doubleWithAd, out int collectedCoins)
        {
            int pending = GetPendingOfflineCoins();
            if (pending <= 0)
            {
                collectedCoins = 0;
                return false;
            }

            collectedCoins = doubleWithAd ? pending * 2 : pending;
            _state.LastCanteenCollectionUtcTicks = _timeProvider.UtcNow.Ticks;
            _wallet.Add(CurrencyKind.Coins, collectedCoins, "CanteenOffline");
            return true;
        }

        public int GetInitialSquadBonus()
        {
            BarracksBuildingDefinition def = _catalog.Get(BarracksBuildingType.Recruitment);
            int level = GetBuildingLevel(BarracksBuildingType.Recruitment);
            BarracksBuildingLevelDefinition levelDef = def?.GetLevel(level);
            return levelDef != null ? (int)levelDef.EffectValue : (2 * level);
        }

        public float GetBaseDamageMultiplier()
        {
            BarracksBuildingDefinition def = _catalog.Get(BarracksBuildingType.Armory);
            int level = GetBuildingLevel(BarracksBuildingType.Armory);
            BarracksBuildingLevelDefinition levelDef = def?.GetLevel(level);
            float bonus = levelDef != null ? levelDef.EffectValue : (0.05f * level);
            return 1.0f + bonus;
        }

        public int CalculateRecoveredSoldiers(int soldiersLost)
        {
            if (soldiersLost <= 0)
            {
                return 0;
            }

            int recovered = soldiersLost / 10;
            BarracksBuildingDefinition def = _catalog.Get(BarracksBuildingType.Infirmary);
            int level = GetBuildingLevel(BarracksBuildingType.Infirmary);
            BarracksBuildingLevelDefinition levelDef = def?.GetLevel(level);
            int cap = levelDef != null ? (int)levelDef.EffectValue : (2 * level);
            return Math.Min(recovered, cap);
        }

        public IReadOnlyList<string> GetUnlockedAmmoPerks()
        {
            int level = GetBuildingLevel(BarracksBuildingType.Ammunition);
            BarracksBuildingDefinition def = _catalog.Get(BarracksBuildingType.Ammunition);
            var perks = new List<string>();

            if (def != null && def.Levels != null)
            {
                for (int lv = 1; lv <= level && lv <= def.Levels.Count; lv++)
                {
                    var levelDef = def.GetLevel(lv);
                    if (!string.IsNullOrEmpty(levelDef?.UnlockedPerkId))
                    {
                        perks.Add(levelDef.UnlockedPerkId);
                    }
                }
            }
            else
            {
                if (level >= 1) perks.Add("ammo_fire");
                if (level >= 2) perks.Add("ammo_shock");
                if (level >= 3) perks.Add("ammo_cryo");
                if (level >= 4) perks.Add("ammo_toxic");
            }

            return perks;
        }
    }
}
