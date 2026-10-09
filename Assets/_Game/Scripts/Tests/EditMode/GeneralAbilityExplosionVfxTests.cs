using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities.Effects;
using Game.Core.Events;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class GeneralAbilityExplosionVfxTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBus _eventBus;
        private GeneralAbilityExplosionVfx _vfx;
        private SpriteRenderer _renderer;
        private Transform _origin;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();

            var originGo = new GameObject("General");
            _created.Add(originGo);
            originGo.transform.position = new Vector3(-1f, 0f, 42f);
            _origin = originGo.transform;

            var texture = new Texture2D(64, 64);
            _created.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
            _created.Add(sprite);

            var vfxGo = new GameObject("GrenadeExplosionVfx");
            _created.Add(vfxGo);
            _renderer = vfxGo.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _vfx = vfxGo.AddComponent<GeneralAbilityExplosionVfx>();
            _vfx.Configure(_renderer, _origin);
            _vfx.Initialize(_eventBus);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _created.Clear();
        }

        private float WorldDiameter() => _vfx.transform.localScale.x * _renderer.sprite.bounds.size.x;

        [Test]
        public void Initialize_HidesSpriteUntilAbilityIsUsed()
        {
            Assert.IsFalse(_renderer.enabled);
            Assert.IsFalse(_vfx.IsPlaying);
        }

        [Test]
        public void AbilityUsedEvent_ShowsBurstAtGeneralPosition()
        {
            _eventBus.Publish(new AbilityUsedEvent("grenade", 0));

            Assert.IsTrue(_vfx.IsPlaying);
            Assert.IsTrue(_renderer.enabled);
            Assert.AreEqual(1, _vfx.PlayedCount);
            Assert.AreEqual(_origin.position.x, _vfx.transform.position.x, 1e-4f);
            Assert.AreEqual(_origin.position.z, _vfx.transform.position.z, 1e-4f);
        }

        [Test]
        public void Tick_GrowsBurstToGrenadeDiameterAndFadesOut()
        {
            _eventBus.Publish(new AbilityUsedEvent("grenade", 0));
            float startDiameter = WorldDiameter();
            float startAlpha = _renderer.color.a;

            _vfx.Tick(_vfx.Duration * 0.98f);

            Assert.Greater(WorldDiameter(), startDiameter);
            Assert.AreEqual(GrenadeAbilityEffect.DefaultRadius * 2f, WorldDiameter(), 0.5f, "O flash deve cobrir o raio real da Granada.");
            Assert.Less(_renderer.color.a, startAlpha);
        }

        [Test]
        public void Tick_PastDuration_HidesBurstAndStops()
        {
            _eventBus.Publish(new AbilityUsedEvent("grenade", 0));

            _vfx.Tick(_vfx.Duration + 0.01f);

            Assert.IsFalse(_vfx.IsPlaying);
            Assert.IsFalse(_renderer.enabled);
        }

        [Test]
        public void AbilityChargeProgressEvent_DoesNotPlay()
        {
            _eventBus.Publish(new AbilityChargeProgressEvent("grenade", 25, 25, 1));

            Assert.IsFalse(_vfx.IsPlaying);
            Assert.AreEqual(0, _vfx.PlayedCount);
        }
    }
}
