using Game.Core;
using Game.Core.Ads;
using Game.Core.Analytics;
using Game.Infrastructure.Ads;
using Game.Meta.Ads;
using Game.Meta.Barracks;
using Game.Meta.Stages;
using Game.Meta.Time;
using Game.Meta.Tracks;
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
            builder.Register<MonotonicTimeProvider>(Lifetime.Singleton).As<ITimeProvider>();
            builder.Register<Entitlements>(Lifetime.Singleton).As<IEntitlements>();
            builder.Register<AdPolicyDefinition>(Lifetime.Singleton);
            builder.Register<AdPolicy>(Lifetime.Singleton).As<IAdPolicy>();
            builder.Register<LevelPlayAdService>(Lifetime.Singleton).As<IAdService>();
            builder.RegisterInstance(BarracksCatalog.CreateDefault()).As<IBarracksCatalog>();
            builder.Register<BarracksService>(Lifetime.Singleton).As<IBarracksService>();
        }
    }
}
