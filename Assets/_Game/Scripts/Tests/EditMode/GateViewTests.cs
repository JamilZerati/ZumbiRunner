using System.Collections.Generic;
using Game.Core;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Data;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class GateViewTests
    {
        private List<Object> toDestroy;

        [SetUp]
        public void SetUp()
        {
            toDestroy = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            if (toDestroy != null)
            {
                for (int i = toDestroy.Count - 1; i >= 0; i--)
                {
                    if (toDestroy[i] != null)
                    {
                        Object.DestroyImmediate(toDestroy[i]);
                    }
                }
                toDestroy.Clear();
            }
        }

        private T Track<T>(T obj) where T : Object
        {
            if (obj != null)
            {
                toDestroy.Add(obj);
            }
            return obj;
        }

        private PerkDefinition CreatePerk(string id, string name, params IPerkEffect[] effects)
        {
            var perk = Track(ScriptableObject.CreateInstance<PerkDefinition>());
            perk.SetData(id, name, new List<IPerkEffect>(effects));
            return perk;
        }

        private (GateView view, TextMeshProUGUI text, Renderer panel, Renderer frame) CreateTestGateView()
        {
            var go = Track(new GameObject("TestGateView"));
            var view = go.AddComponent<GateView>();

            var textGo = Track(new GameObject("Label"));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();

            var panelGo = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            panelGo.name = "Panel";
            panelGo.transform.SetParent(go.transform, false);
            var panelRenderer = panelGo.GetComponent<Renderer>();
            panelRenderer.sharedMaterial = Track(new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit")));

            var frameGo = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            frameGo.name = "Frame";
            frameGo.transform.SetParent(go.transform, false);
            var frameRenderer = frameGo.GetComponent<Renderer>();
            frameRenderer.sharedMaterial = Track(new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit")));

            view.LabelText = text;
            view.PanelRenderer = panelRenderer;
            view.FrameRenderer = frameRenderer;

            return (view, text, panelRenderer, frameRenderer);
        }

        private class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }
            public FakeSquad(int count) => SquadCount = count;
            public bool Add(int amount) { SquadCount += amount; return true; }
            public bool Remove(int amount) { SquadCount -= amount; return true; }
            public bool Multiply(int factor) { SquadCount *= factor; return true; }
            public bool Divide(int divisor) { SquadCount /= divisor; return true; }
            public bool SetCount(int newCount) { SquadCount = newCount; return true; }
        }

        [Test]
        public void SetPerk_WithPositiveBonus_SetsTextAndPositiveHighlightColor()
        {
            var (view, text, panel, frame) = CreateTestGateView();
            var perk = CreatePerk("add_5", "+5", new AddSoldiersEffect(5));

            view.SetPerk(perk);

            Assert.AreEqual("+5", text.text);
            Assert.AreEqual(GateView.PositiveColor, view.CurrentColor);
            Assert.IsTrue(panel.sharedMaterial.color == GateView.PositiveColor);
            Assert.IsTrue(frame.sharedMaterial.color == GateView.PositiveColor);
            Assert.IsTrue(text.color == GateView.ActiveTextColor);
        }

        [Test]
        public void SetPerk_WithMultiplyBonus_SetsPositiveHighlightColor()
        {
            var (view, text, panel, frame) = CreateTestGateView();
            var perk = CreatePerk("mult_2", "x2", new MultiplySoldiersEffect(2));

            view.SetPerk(perk);

            Assert.AreEqual("x2", text.text);
            Assert.AreEqual(GateView.PositiveColor, view.CurrentColor);
            Assert.IsTrue(panel.sharedMaterial.color == GateView.PositiveColor);
            Assert.IsTrue(text.color == GateView.ActiveTextColor);
        }

        [Test]
        public void SetPerk_WithNegativePenalty_SetsTextAndNegativeHighlightColor()
        {
            var (view, text, panel, frame) = CreateTestGateView();
            var perk = CreatePerk("sub_3", "-3", new AddSoldiersEffect(-3));

            view.SetPerk(perk);

            Assert.AreEqual("-3", text.text);
            Assert.AreEqual(GateView.NegativeColor, view.CurrentColor);
            Assert.IsTrue(panel.sharedMaterial.color == GateView.NegativeColor);
            Assert.IsTrue(frame.sharedMaterial.color == GateView.NegativeColor);
            Assert.IsTrue(text.color == GateView.ActiveTextColor);
        }

        [Test]
        public void SetPerk_WithDividePenalty_SetsNegativeHighlightColor()
        {
            var (view, text, panel, frame) = CreateTestGateView();
            var perk = CreatePerk("div_2", "÷2", new DivideSoldiersEffect(2));

            view.SetPerk(perk);

            Assert.AreEqual("÷2", text.text);
            Assert.AreEqual(GateView.NegativeColor, view.CurrentColor);
            Assert.IsTrue(panel.sharedMaterial.color == GateView.NegativeColor);
            Assert.IsTrue(text.color == GateView.ActiveTextColor);
        }

        [Test]
        public void SetConsumed_True_SetsIsConsumedAndAppliesConsumedColor()
        {
            var (view, text, panel, frame) = CreateTestGateView();
            var perk = CreatePerk("add_5", "+5", new AddSoldiersEffect(5));
            view.SetPerk(perk);

            view.SetConsumed(true);

            Assert.IsTrue(view.IsConsumed);
            Assert.AreEqual(GateView.ConsumedColor, view.CurrentColor);
            Assert.IsTrue(panel.sharedMaterial.color == GateView.ConsumedColor);
            Assert.IsTrue(frame.sharedMaterial.color == GateView.ConsumedColor);
            Assert.IsTrue(text.color == GateView.ConsumedTextColor);
        }

        [Test]
        public void SetConsumed_False_RestoresActiveHighlightColor()
        {
            var (view, text, panel, frame) = CreateTestGateView();
            var perk = CreatePerk("add_5", "+5", new AddSoldiersEffect(5));
            view.SetPerk(perk);

            view.SetConsumed(true);
            Assert.IsTrue(view.IsConsumed);

            view.SetConsumed(false);
            Assert.IsFalse(view.IsConsumed);
            Assert.AreEqual(GateView.PositiveColor, view.CurrentColor);
            Assert.IsTrue(panel.sharedMaterial.color == GateView.PositiveColor);
            Assert.IsTrue(text.color == GateView.ActiveTextColor);
        }

        [Test]
        public void Initialize_WithGateAndPerk_WiresComponentsCorrectly()
        {
            var (view, text, panel, frame) = CreateTestGateView();
            var gateGo = Track(new GameObject("Gate"));
            var gate = gateGo.AddComponent<Gate>();
            var perk = CreatePerk("add_10", "+10", new AddSoldiersEffect(10));
            gate.Initialize(0, perk);

            view.Initialize(perk, gate);

            Assert.AreEqual("+10", text.text);
            Assert.AreEqual(GateView.PositiveColor, view.CurrentColor);
        }

        [Test]
        public void OnGateTriggered_FromParentPair_AutomaticallySetsConsumed()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate0"));
            var gate = gateGo.AddComponent<Gate>();
            var perk = CreatePerk("add_5", "+5", new AddSoldiersEffect(5));
            gate.Initialize(0, perk, pair);

            var (view, text, panel, frame) = CreateTestGateView();
            view.Initialize(perk, gate);

            Assert.IsFalse(view.IsConsumed);

            var squad = new FakeSquad(5);
            pair.TryTrigger(0, squad);

            Assert.IsTrue(view.IsConsumed);
            Assert.AreEqual(GateView.ConsumedColor, view.CurrentColor);
        }

        [Test]
        public void SetPerk_WithNullPerk_ClearsTextAndDoesNotThrow()
        {
            var (view, text, panel, frame) = CreateTestGateView();

            Assert.DoesNotThrow(() => view.SetPerk(null));
            Assert.AreEqual(string.Empty, text.text);
            Assert.AreEqual(GateView.PositiveColor, view.CurrentColor);
        }

        [Test]
        public void SetPerk_WithoutRenderersOrLabel_DoesNotThrow()
        {
            var go = Track(new GameObject("MinimalGateView"));
            var view = go.AddComponent<GateView>();
            var perk = CreatePerk("add_5", "+5", new AddSoldiersEffect(5));

            Assert.DoesNotThrow(() => view.SetPerk(perk));
            Assert.AreEqual(GateView.PositiveColor, view.CurrentColor);
            Assert.DoesNotThrow(() => view.SetConsumed(true));
            Assert.IsTrue(view.IsConsumed);
        }
    }
}
