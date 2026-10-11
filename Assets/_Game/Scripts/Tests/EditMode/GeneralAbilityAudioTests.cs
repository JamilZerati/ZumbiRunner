using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class GeneralAbilityAudioTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBus _eventBus;
        private GeneralAbilityAudio _audio;
        private AudioSource _source;
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            var go = new GameObject("GeneralAbilityAudio");
            _created.Add(go);
            _source = go.AddComponent<AudioSource>();
            _audio = go.AddComponent<GeneralAbilityAudio>();
            _clip = AudioClip.Create("explosion", 4410, 1, 44100, false);
            _created.Add(_clip);
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

        [Test]
        public void AbilityUsedEvent_PlaysExplosionClipOnce()
        {
            _audio.Configure(_source, _clip);
            _audio.Initialize(_eventBus);

            _eventBus.Publish(new AbilityUsedEvent("grenade", 0));

            Assert.AreEqual(1, _audio.PlayedCount);
        }

        [Test]
        public void AbilityChargeProgressEvent_DoesNotPlayClip()
        {
            _audio.Configure(_source, _clip);
            _audio.Initialize(_eventBus);

            _eventBus.Publish(new AbilityChargeProgressEvent("grenade", 10, 25, 0));

            Assert.AreEqual(0, _audio.PlayedCount);
        }

        [Test]
        public void AbilityUsedEvent_WithoutClip_DoesNotPlayOrThrow()
        {
            _audio.Configure(_source, null);
            _audio.Initialize(_eventBus);

            Assert.DoesNotThrow(() => _eventBus.Publish(new AbilityUsedEvent("grenade", 0)));
            Assert.AreEqual(0, _audio.PlayedCount);
        }

        [Test]
        public void Initialize_Twice_SubscribesOnlyOnce()
        {
            _audio.Configure(_source, _clip);
            _audio.Initialize(_eventBus);
            _audio.Initialize(_eventBus);

            _eventBus.Publish(new AbilityUsedEvent("grenade", 0));

            Assert.AreEqual(1, _audio.PlayedCount);
        }
    }
}
