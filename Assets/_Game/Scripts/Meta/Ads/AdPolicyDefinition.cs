namespace Game.Meta.Ads
{
    public class AdPolicyDefinition
    {
        public int InterstitialMinStage { get; set; } = 5;
        public int InterstitialStageInterval { get; set; } = 3;
        public int InterstitialMinTimeBetweenAdsSeconds { get; set; } = 90;
        public int InterstitialCooldownAfterRewardedOrPurchaseSeconds { get; set; } = 120;
        public int InterstitialMinStageFirstSessionOfDay { get; set; } = 2;
        public int DailyRewardedLimit { get; set; } = 20;
        public int DailyBarracksSpeedupLimit { get; set; } = 3;
    }
}
