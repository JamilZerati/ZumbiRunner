using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Core.Events;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class SquadCountHudTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private GameObject hudObject;
        private SquadCountHud hud;
        private TextMeshProUGUI textComponent;
        private EventBus eventBus;

        [SetUp]
        public void SetUp()
        {
            hudObject = CreateTrackedGameObject("SquadCountHud");
            hud = hudObject.AddComponent<SquadCountHud>();

            var textObject = CreateTrackedGameObject("CountText");
            textObject.transform.SetParent(hudObject.transform, false);
            textComponent = textObject.AddComponent<TextMeshProUGUI>();

            hud.SetCountText(textComponent);
            eventBus = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }
            createdObjects.Clear();
        }

        private GameObject CreateTrackedGameObject(string name)
        {
            var go = new GameObject(name);
            createdObjects.Add(go);
            return go;
        }

        [Test]
        public void Initialize_SetsInitialCountAndFormatsText()
        {
            hud.Initialize(eventBus, initialCount: 5);

            Assert.AreEqual(5, hud.DisplayedCount);
            Assert.AreEqual("Tropa: 5", textComponent.text);
        }

        [Test]
        public void SetCount_UpdatesDisplayedCountAndText()
        {
            hud.Initialize(eventBus, initialCount: 1);

            hud.SetCount(12);

            Assert.AreEqual(12, hud.DisplayedCount);
            Assert.AreEqual("Tropa: 12", textComponent.text);
        }

        [Test]
        public void SquadSizeChangedEvent_UpdatesDisplayedCountAndTextAutomatically()
        {
            hud.Initialize(eventBus, initialCount: 3);

            eventBus.Publish(new SquadSizeChangedEvent(3, 7));

            Assert.AreEqual(7, hud.DisplayedCount);
            Assert.AreEqual("Tropa: 7", textComponent.text);
        }

        [Test]
        public void Initialize_WhenCalledMultipleTimes_ReplacesPreviousSubscription()
        {
            var firstBus = new EventBus();
            var secondBus = new EventBus();

            hud.Initialize(firstBus, initialCount: 2);
            hud.Initialize(secondBus, initialCount: 4);

            firstBus.Publish(new SquadSizeChangedEvent(2, 9));
            Assert.AreEqual(4, hud.DisplayedCount);
            Assert.AreEqual("Tropa: 4", textComponent.text);

            secondBus.Publish(new SquadSizeChangedEvent(4, 15));
            Assert.AreEqual(15, hud.DisplayedCount);
            Assert.AreEqual("Tropa: 15", textComponent.text);
        }

        [Test]
        public void SetCount_WithoutTextComponent_UpdatesDisplayedCountWithoutThrowing()
        {
            hud.SetCountText(null);

            Assert.DoesNotThrow(() => hud.SetCount(20));
            Assert.AreEqual(20, hud.DisplayedCount);
        }

        [Test]
        public void OnDestroy_UnsubscribesFromEventBus()
        {
            hud.Initialize(eventBus, initialCount: 3);

            typeof(SquadCountHud)
                .GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(hud, null);

            Assert.DoesNotThrow(() => eventBus.Publish(new SquadSizeChangedEvent(3, 10)));
            Assert.AreEqual(3, hud.DisplayedCount);
            Assert.AreEqual("Tropa: 3", textComponent.text);
        }
    }
}
