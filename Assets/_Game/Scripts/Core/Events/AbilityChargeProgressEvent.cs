namespace Game.Core.Events
{
    public readonly struct AbilityChargeProgressEvent
    {
        public string AbilityId { get; }
        public int CurrentKills { get; }
        public int TargetKills { get; }
        public int CurrentCharges { get; }

        public AbilityChargeProgressEvent(string abilityId, int currentKills, int targetKills, int currentCharges)
        {
            AbilityId = abilityId;
            CurrentKills = currentKills;
            TargetKills = targetKills;
            CurrentCharges = currentCharges;
        }
    }
}
