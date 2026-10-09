using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class DebugTextOverlayTests
    {
        private List<Object> _toDestroy;

        [SetUp]
        public void SetUp()
        {
            _toDestroy = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_toDestroy != null)
            {
                for (int i = _toDestroy.Count - 1; i >= 0; i--)
                {
                    if (_toDestroy[i] != null)
                    {
                        Object.DestroyImmediate(_toDestroy[i]);
                    }
                }
                _toDestroy.Clear();
            }
        }

        private T Track<T>(T obj) where T : Object
        {
            if (obj != null) _toDestroy.Add(obj);
            return obj;
        }

        [Test]
        public void Initialize_SubscreveEventosEAtualizaTextoNoUpdate()
        {
            var go = Track(new GameObject("DebugOverlay"));
            var overlay = go.AddComponent<DebugTextOverlay>();

            var textGo = Track(new GameObject("Text"));
            textGo.transform.SetParent(go.transform);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();

            var bus = new EventBus();
            var squadGo = Track(new GameObject("Squad"));
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(7, bus);

            overlay.ConfigureComponents(tmp);
            overlay.Initialize(bus, squad: squad);

            // Simula frame de update
            overlay.Refresh();

            StringAssert.Contains("Tropa: 7", tmp.text);
            StringAssert.Contains("[DEBUG OVERLAY]", tmp.text);
        }

        [Test]
        public void Eventos_PortoesSinergiasECargas_AtualizamHistoricoNoOverlay()
        {
            var go = Track(new GameObject("DebugOverlay"));
            var overlay = go.AddComponent<DebugTextOverlay>();

            var textGo = Track(new GameObject("Text"));
            var tmp = textGo.AddComponent<TextMeshProUGUI>();

            var bus = new EventBus();
            overlay.ConfigureComponents(tmp);
            overlay.Initialize(bus);

            bus.Publish(new GateTriggeredEvent(1, "perk_rapid_fire"));
            bus.Publish(new SynergyTriggeredEvent("shatter", null, 15, 30));
            bus.Publish(new AbilityChargeProgressEvent("grenade", 12, 25, 1));
            bus.Publish(new AbilityUsedEvent("grenade", 0));

            overlay.Refresh();

            StringAssert.Contains("L1:perk_rapid_fire", tmp.text);
            StringAssert.Contains("shatter (15 -> 30)", tmp.text);
            StringAssert.Contains("12/25 abates", tmp.text);
            StringAssert.Contains("Usos: 1", tmp.text);
        }

        [Test]
        public void Toggle_AlternaVisibilidadeDoTexto()
        {
            var go = Track(new GameObject("DebugOverlay"));
            var overlay = go.AddComponent<DebugTextOverlay>();

            var textGo = Track(new GameObject("Text"));
            var tmp = textGo.AddComponent<TextMeshProUGUI>();

            var bus = new EventBus();
            overlay.ConfigureComponents(tmp);
            overlay.Initialize(bus);

            Assert.IsTrue(overlay.IsVisible);
            Assert.IsTrue(textGo.activeSelf);

            overlay.Toggle();

            Assert.IsFalse(overlay.IsVisible);
            Assert.IsFalse(textGo.activeSelf);

            overlay.Toggle();

            Assert.IsTrue(overlay.IsVisible);
            Assert.IsTrue(textGo.activeSelf);
        }

        [Test]
        public void Awake_QuandoNaoForDebugBuild_DesativaGameObject()
        {
            var go = Track(new GameObject("DebugOverlay"));
            var overlay = go.AddComponent<DebugTextOverlay>();
            overlay.DebugBuildCheck = () => false;

            overlay.CheckAndApplyBuildMode();

            Assert.IsFalse(go.activeSelf);
        }
    }
}
