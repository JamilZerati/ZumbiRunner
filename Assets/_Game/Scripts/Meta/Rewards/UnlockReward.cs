using System;

namespace Game.Meta.Rewards
{
    public class UnlockReward : IReward
    {
        public string RewardType => "Unlock";
        public string ContentId { get; }

        public UnlockReward(string contentId)
        {
            ContentId = contentId;
        }

        public void Apply(IRewardSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.UnlockContent(ContentId);
        }
    }
}
