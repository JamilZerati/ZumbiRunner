using System;
using System.Collections.Generic;
using Game.Meta.Barracks;
using Game.Meta.Time;
using Game.Meta.Wallet;

namespace Game.Tests.EditMode.Meta.Doubles
{
    public class FakeBarracksService : IBarracksService
    {
        private readonly ITimeProvider _timeProvider;
        private readonly IWallet _wallet;
        private readonly Dictionary<BarracksBuildingType, int> _levels = new Dictionary<BarracksBuildingType, int>();

        private struct ActiveConstruction
        {
            public bool IsActive;
            public BarracksBuildingType Type;
            public DateTime EndUtc;
            public int TargetLevel;
        }

        private ActiveConstruction _construction;
        private int _adSpeedupsUsedToday;
        private DateTime _lastAdUsageDayUtc;
        private DateTime _lastCanteenCollectionUtc;

        public bool IsClockManipulated => _timeProvider.IsClockManipulated;

        public FakeBarracksService(ITimeProvider timeProvider, IWallet wallet)
        {
            _timeProvider = timeProvider;
            _wallet = wallet;

            foreach (BarracksBuildingType type in Enum.GetValues(typeof(BarracksBuildingType)))
            {
                _levels[type] = 1;
            }

            _lastAdUsageDayUtc = timeProvider.UtcNow.Date;
            _lastCanteenCollectionUtc = timeProvider.UtcNow;
        }

        public void SetBuildingLevel(BarracksBuildingType type, int level)
        {
            _levels[type] = level;
        }

        public int GetBuildingLevel(BarracksBuildingType type)
        {
            return _levels.TryGetValue(type, out int lv) ? lv : 0;
        }

        public bool IsUnderConstruction(BarracksBuildingType type)
        {
            return _construction.IsActive && _construction.Type == type;
        }

        public TimeSpan GetRemainingConstructionTime(BarracksBuildingType type)
        {
            if (!IsUnderConstruction(type)) return TimeSpan.Zero;
            var remaining = _construction.EndUtc - _timeProvider.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public int GetInstantCompleteMedalCost(BarracksBuildingType type)
        {
            if (!IsUnderConstruction(type)) return 0;
            double remainingMinutes = GetRemainingConstructionTime(type).TotalMinutes;
            return Math.Max(5, (int)Math.Ceiling(remainingMinutes / 5.0));
        }

        public bool CanStartUpgrade(BarracksBuildingType type, out string failReason)
        {
            if (IsClockManipulated)
            {
                failReason = "Relógio do sistema inconsistente.";
                return false;
            }

            if (_construction.IsActive)
            {
                failReason = "Já existe uma obra em andamento no Quartel.";
                return false;
            }

            int currentLevel = GetBuildingLevel(type);
            if (currentLevel >= 5)
            {
                failReason = "Construção já atingiu o nível máximo.";
                return false;
            }

            int nextLevel = currentLevel + 1;
            int coinCost = GetCoinCost(type, nextLevel);
            int recruitCost = GetRecruitCost(type, nextLevel);

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
            if (!CanStartUpgrade(type, out _)) return false;

            int nextLevel = GetBuildingLevel(type) + 1;
            int coinCost = GetCoinCost(type, nextLevel);
            int recruitCost = GetRecruitCost(type, nextLevel);

            // Pré-validação atômica estrita antes de debitar
            if (coinCost > 0 && _wallet.GetBalance(CurrencyKind.Coins) < coinCost) return false;
            if (recruitCost > 0 && _wallet.GetBalance(CurrencyKind.Recruits) < recruitCost) return false;

            if (coinCost > 0) _wallet.TrySpend(CurrencyKind.Coins, coinCost, "BarracksUpgrade");
            if (recruitCost > 0) _wallet.TrySpend(CurrencyKind.Recruits, recruitCost, "BarracksUpgrade");

            int durationSeconds = 3600;
            _construction = new ActiveConstruction
            {
                IsActive = true,
                Type = type,
                EndUtc = _timeProvider.UtcNow.AddSeconds(durationSeconds),
                TargetLevel = nextLevel
            };

            return true;
        }

        public bool TrySpeedupWithAd(BarracksBuildingType type)
        {
            if (!IsUnderConstruction(type)) return false;

            DateTime today = _timeProvider.UtcNow.Date;
            if (today > _lastAdUsageDayUtc)
            {
                _adSpeedupsUsedToday = 0;
                _lastAdUsageDayUtc = today;
            }

            if (_adSpeedupsUsedToday >= 3) return false;

            _adSpeedupsUsedToday++;
            _construction.EndUtc = _construction.EndUtc.Subtract(TimeSpan.FromMinutes(30));
            if (_construction.EndUtc < _timeProvider.UtcNow)
            {
                _construction.EndUtc = _timeProvider.UtcNow;
            }

            return true;
        }

        public bool TryInstantCompleteWithMedals(BarracksBuildingType type)
        {
            if (!IsUnderConstruction(type)) return false;

            int medalCost = GetInstantCompleteMedalCost(type);
            if (!_wallet.TrySpend(CurrencyKind.Medals, medalCost, "BarracksInstantComplete"))
            {
                return false;
            }

            _levels[type] = _construction.TargetLevel;
            _construction = default;
            return true;
        }

        public bool TryCompleteUpgrade(BarracksBuildingType type)
        {
            if (!IsUnderConstruction(type)) return false;
            if (_timeProvider.UtcNow < _construction.EndUtc) return false;

            _levels[type] = _construction.TargetLevel;
            _construction = default;
            return true;
        }

        public int GetPendingOfflineCoins()
        {
            if (IsClockManipulated) return 0;

            var elapsed = _timeProvider.UtcNow - _lastCanteenCollectionUtc;
            if (elapsed.TotalSeconds <= 0) return 0;

            double cappedHours = Math.Min(8.0, elapsed.TotalHours);
            int ratePerHour = 50 * GetBuildingLevel(BarracksBuildingType.Canteen);
            return (int)Math.Floor(cappedHours * ratePerHour);
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
            _lastCanteenCollectionUtc = _timeProvider.UtcNow; // Descarta excedente além de 8h
            _wallet.Add(CurrencyKind.Coins, collectedCoins, "CanteenOffline");
            return true;
        }

        public int GetInitialSquadBonus()
        {
            return 2 * GetBuildingLevel(BarracksBuildingType.Recruitment);
        }

        public float GetBaseDamageMultiplier()
        {
            return 1.0f + 0.05f * GetBuildingLevel(BarracksBuildingType.Armory);
        }

        public int CalculateRecoveredSoldiers(int soldiersLost)
        {
            int recovered = soldiersLost / 10;
            int cap = 2 * GetBuildingLevel(BarracksBuildingType.Infirmary);
            return Math.Min(recovered, cap);
        }

        public IReadOnlyList<string> GetUnlockedAmmoPerks()
        {
            int level = GetBuildingLevel(BarracksBuildingType.Ammunition);
            var perks = new List<string>();
            if (level >= 1) perks.Add("ammo_fire");
            if (level >= 2) perks.Add("ammo_shock");
            if (level >= 3) perks.Add("ammo_cryo");
            if (level >= 4) perks.Add("ammo_toxic");
            return perks;
        }

        private static int GetCoinCost(BarracksBuildingType type, int level)
        {
            switch (type)
            {
                case BarracksBuildingType.Recruitment: return 100 * level;
                case BarracksBuildingType.Armory: return 150 * level;
                case BarracksBuildingType.Canteen: return 80 * level;
                case BarracksBuildingType.Infirmary: return 120 * level;
                case BarracksBuildingType.Ammunition: return 0;
                default: return 100 * level;
            }
        }

        private static int GetRecruitCost(BarracksBuildingType type, int level)
        {
            switch (type)
            {
                case BarracksBuildingType.Recruitment: return 5 * level;
                case BarracksBuildingType.Armory: return 0;
                case BarracksBuildingType.Canteen: return 3 * level;
                case BarracksBuildingType.Infirmary: return 5 * level;
                case BarracksBuildingType.Ammunition: return 10 * level;
                default: return 0;
            }
        }
    }
}
