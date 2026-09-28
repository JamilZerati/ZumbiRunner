using System.IO;
using Game.Editor;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class SceneBuilderM3Tests
    {
        [Test]
        public void M3GreyboxScenePath_IsDefinedInExpectedLocation()
        {
            Assert.AreEqual("Assets/_Game/Scenes/M3_Greybox.unity", SceneBuilder.M3GreyboxScenePath);
        }

        [Test]
        public void BuildM3GreyboxScene_CreatesSceneFile()
        {
            SceneBuilder.BuildM3GreyboxScene();

            Assert.IsTrue(File.Exists(SceneBuilder.M3GreyboxScenePath));
        }

        [Test]
        public void BuildM3GreyboxScene_ContainsRequiredGameplayAndGateComponents()
        {
            SceneBuilder.BuildM3GreyboxScene();

            var mover = Object.FindAnyObjectByType<LaneMover>();
            Assert.IsNotNull(mover, "LaneMover must exist in M3 Greybox scene.");

            var rb = mover.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb, "General must have a Rigidbody for physics trigger interactions.");
            Assert.IsTrue(rb.isKinematic, "General Rigidbody must be kinematic.");
            Assert.IsFalse(rb.useGravity, "General Rigidbody must not use gravity.");

            var squad = Object.FindAnyObjectByType<SquadController>();
            Assert.IsNotNull(squad, "SquadController must exist in M3 Greybox scene.");
            Assert.AreEqual(3, squad.SquadCount, "SquadController should be initialized with 3 soldiers.");

            var visual = Object.FindAnyObjectByType<SquadVisualController>();
            Assert.IsNotNull(visual, "SquadVisualController must exist in M3 Greybox scene.");

            var hud = Object.FindAnyObjectByType<SquadCountHud>();
            Assert.IsNotNull(hud, "SquadCountHud must exist in M3 Greybox scene.");

            var cam = Object.FindAnyObjectByType<FollowCamera>();
            Assert.IsNotNull(cam, "FollowCamera must exist in M3 Greybox scene.");

            var gatePairs = Object.FindObjectsByType<GatePair>(FindObjectsSortMode.None);
            Assert.AreEqual(3, gatePairs.Length, "Scene should contain exactly 3 GatePair instances.");

            var gates = Object.FindObjectsByType<Gate>(FindObjectsSortMode.None);
            Assert.AreEqual(6, gates.Length, "Scene should contain exactly 6 Gate instances (2 per pair).");

            var gateViews = Object.FindObjectsByType<GateView>(FindObjectsSortMode.None);
            Assert.AreEqual(6, gateViews.Length, "Scene should contain exactly 6 GateView instances (1 per gate).");

            foreach (var pair in gatePairs)
            {
                Assert.AreEqual(2, pair.Gates.Count, "Each GatePair must have exactly 2 registered gates.");
            }

            foreach (var gate in gates)
            {
                var collider = gate.GetComponent<BoxCollider>();
                Assert.IsNotNull(collider, "Gate must have a BoxCollider.");
                Assert.IsTrue(collider.isTrigger, "Gate collider must be a trigger.");
                Assert.IsNotNull(gate.Perk, "Gate must have a PerkDefinition assigned.");
                Assert.IsNotNull(gate.ParentPair, "Gate must have a ParentPair reference.");

                var view = gate.GetComponent<GateView>();
                Assert.IsNotNull(view, "Gate must have a GateView component.");
                Assert.IsNotNull(view.LabelText, "GateView must have a LabelText reference.");
                Assert.IsNotNull(view.PanelRenderer, "GateView must have a PanelRenderer reference.");
                Assert.IsNotNull(view.FrameRenderer, "GateView must have a FrameRenderer reference.");
            }
        }
    }
}
