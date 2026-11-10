using System;
using System.Collections.Generic;
using Game.Core.Ads;
using Game.Meta.Time;
using Game.Meta.Tracks;

namespace Game.Meta.Ads
{
    public class AdPolicy : IAdPolicy
    {
        private readonly ITimeProvider _timeProvider;
        private readonly IEntitlements _entitlements;
        private readonly AdPolicyDefinition _definition;
        private readonly AdPolicyState _state;

        public AdPolicyDefinition Definition => _definition;
        public AdPolicyState State => _state;

        public AdPolicy(
            ITimeProvider timeProvider,
            IEntitlements entitlements,
            AdPolicyDefinition definition = null,
            AdPolicyState state = null)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _entitlements = entitlements;
            _definition = definition ?? new AdPolicyDefinition();
            _state = state ?? new AdPolicyState();

            EnsureDateTracking();
        }

        private void EnsureDateTracking()
        {
            DateTime today = _timeProvider.UtcNow.Date;
            if (_state.CurrentSessionDateUtcTicks == 0)
            {
                _state.CurrentSessionDateUtcTicks = today.Ticks;
            }
            if (_state.DailyRewardedDateUtcTicks == 0)
            {
                _state.DailyRewardedDateUtcTicks = today.Ticks;
            }
        }

        private void CheckDailyRollover()
        {
            DateTime today = _timeProvider.UtcNow.Date;

            if (_state.DailyRewardedDateUtcTicks == 0)
            {
                _state.DailyRewardedDateUtcTicks = today.Ticks;
            }
            else
            {
                DateTime lastDailyDate = new DateTime(_state.DailyRewardedDateUtcTicks, DateTimeKind.Utc).Date;
                if (today > lastDailyDate)
                {
                    _state.DailyRewardedCount = 0;
                    _state.PlacementDailyCounts.Clear();
                    _state.DailyRewardedDateUtcTicks = today.Ticks;
                }
            }

            if (_state.CurrentSessionDateUtcTicks == 0)
            {
                _state.CurrentSessionDateUtcTicks = today.Ticks;
            }
            else
            {
                DateTime lastSessionDate = new DateTime(_state.CurrentSessionDateUtcTicks, DateTimeKind.Utc).Date;
                if (today > lastSessionDate)
                {
                    _state.CurrentSessionDateUtcTicks = today.Ticks;
                    _state.SessionsStartedInCurrentDay = 0;
                    _state.StagesCompletedInCurrentSession = 0;
                }
            }
        }

        public bool CanShowInterstitial(int playerHighestStage)
        {
            CheckDailyRollover();

            if (_entitlements != null && (_entitlements.HasEntitlement("no_ads") || _entitlements.HasEntitlement("remove_ads")))
            {
                return false;
            }

            if (playerHighestStage < _definition.InterstitialMinStage)
            {
                return false;
            }

            bool isFirstSessionOfDay = _state.SessionsStartedInCurrentDay <= 1;
            if (isFirstSessionOfDay && _state.StagesCompletedInCurrentSession < _definition.InterstitialMinStageFirstSessionOfDay)
            {
                return false;
            }

            if (_state.StagesCompletedSinceLastInterstitial < _definition.InterstitialStageInterval)
            {
                return false;
            }

            long nowTicks = _timeProvider.UtcNow.Ticks;

            if (_state.LastAdShownUtcTicks > 0)
            {
                long elapsedTicks = nowTicks - _state.LastAdShownUtcTicks;
                long minTimeTicks = TimeSpan.FromSeconds(_definition.InterstitialMinTimeBetweenAdsSeconds).Ticks;
                if (elapsedTicks < minTimeTicks)
                {
                    return false;
                }
            }

            if (_state.LastRewardedOrPurchaseUtcTicks > 0)
            {
                long elapsedTicks = nowTicks - _state.LastRewardedOrPurchaseUtcTicks;
                long cooldownTicks = TimeSpan.FromSeconds(_definition.InterstitialCooldownAfterRewardedOrPurchaseSeconds).Ticks;
                if (elapsedTicks < cooldownTicks)
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanShowRewarded(AdPlacement placement)
        {
            CheckDailyRollover();

            if (_state.DailyRewardedCount >= _definition.DailyRewardedLimit)
            {
                return false;
            }

            if (placement == AdPlacement.BarracksSpeedup)
            {
                string key = placement.ToString();
                if (_state.PlacementDailyCounts.TryGetValue(key, out int count) && count >= _definition.DailyBarracksSpeedupLimit)
                {
                    return false;
                }
            }

            return true;
        }

        public void RecordAdShown(AdPlacement placement, AdType adType)
        {
            CheckDailyRollover();

            long nowTicks = _timeProvider.UtcNow.Ticks;
            _state.LastAdShownUtcTicks = nowTicks;

            if (adType == AdType.Rewarded)
            {
                _state.LastRewardedOrPurchaseUtcTicks = nowTicks;
                _state.DailyRewardedCount++;

                string key = placement.ToString();
                _state.PlacementDailyCounts[key] = _state.PlacementDailyCounts.GetValueOrDefault(key, 0) + 1;
            }
            else if (adType == AdType.Interstitial)
            {
                _state.LastInterstitialUtcTicks = nowTicks;
                _state.StagesCompletedSinceLastInterstitial = 0;
            }
        }

        public void RecordPurchase()
        {
            _state.LastRewardedOrPurchaseUtcTicks = _timeProvider.UtcNow.Ticks;
        }

        public void RecordSessionStart()
        {
            DateTime today = _timeProvider.UtcNow.Date;
            DateTime lastSessionDate = _state.CurrentSessionDateUtcTicks == 0
                ? DateTime.MinValue
                : new DateTime(_state.CurrentSessionDateUtcTicks, DateTimeKind.Utc).Date;

            if (today > lastSessionDate || _state.SessionsStartedInCurrentDay == 0)
            {
                _state.CurrentSessionDateUtcTicks = today.Ticks;
                _state.SessionsStartedInCurrentDay = 1;
                _state.StagesCompletedInCurrentSession = 0;
            }
            else
            {
                _state.SessionsStartedInCurrentDay++;
                _state.StagesCompletedInCurrentSession = 0;
            }

            CheckDailyRollover();
        }

        public void RecordStageCompleted()
        {
            _state.StagesCompletedSinceLastInterstitial++;
            _state.StagesCompletedInCurrentSession++;
        }
    }
}
