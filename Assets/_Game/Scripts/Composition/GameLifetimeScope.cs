using Game.Core;
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
        }
    }
}
