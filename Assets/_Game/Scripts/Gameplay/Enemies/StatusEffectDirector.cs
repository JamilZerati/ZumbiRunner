using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Status;
using UnityEngine;

namespace Game.Gameplay
{
    public class StatusEffectDirector : MonoBehaviour, IStatusNeighborhood
    {
        public IStatusCatalog Catalog => throw new NotImplementedException();
        public IEffectInteractionTable Interactions => throw new NotImplementedException();
        public IEventBus EventBus => throw new NotImplementedException();
        public IReadOnlyList<EnemyController> Registered => throw new NotImplementedException();

        public void Initialize(IStatusCatalog catalog, IEffectInteractionTable interactions, IEventBus eventBus = null) => throw new NotImplementedException();
        public void Register(EnemyController enemy) => throw new NotImplementedException();
        public void Unregister(EnemyController enemy) => throw new NotImplementedException();
        public IReadOnlyList<IStatusReceiver> FindNearby(IStatusReceiver origin, float radius, int maxCount) => throw new NotImplementedException();
    }
}
