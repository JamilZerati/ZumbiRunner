using Game.Core;
using Game.Core.Analytics;
using Game.Meta.Stages;
using Game.Meta.Wallet;
using Game.Presentation.Settings;
using VContainer;
using VContainer.Unity;

namespace Game.Composition
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();
            builder.Register<GameStateMachine>(Lifetime.Singleton).As<IGameStateMachine>();
            builder.Register<MockAnalyticsService>(Lifetime.Singleton).As<IAnalyticsService>();
            builder.Register<Wallet>(Lifetime.Singleton).As<IWallet>();
            builder.Register<StageMapService>(Lifetime.Singleton).As<IStageMapService>();
            builder.Register<AudioSettings>(Lifetime.Singleton).As<IAudioSettings>();
            builder.Register<LocalizationSettings>(Lifetime.Singleton).As<ILocalizationSettings>();
        }
    }
}
