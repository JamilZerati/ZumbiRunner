using System.Collections.Generic;
using Game.Meta.Debut;
using Game.Meta.Rewards;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using Game.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.Meta
{
    public class DebutJourneyServiceTests
    {
        private sealed class FakeRewardSink : IRewardSink
        {
            private readonly HashSet<string> _granted = new();
            public IWallet Wallet => null;
            public IEntitlements Entitlements => null;

            public bool HasGranted(string grantId) => _granted.Contains(grantId);
            public void RecordGrant(string grantId) => _granted.Add(grantId);
            public void AddItem(string itemId, int quantity) { }
            public void UnlockContent(string contentId) { }
        }

        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();
        }

        private T CreateComponent<T>(string name = null) where T : Component
        {
            var go = new GameObject(name ?? typeof(T).Name);
            _createdObjects.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void Definition_HasSevenDailyMilestones_AndValidTrackId()
        {
            var service = new DebutJourneyService();
            var definition = service.Definition;

            Assert.AreEqual("debut_journey", definition.TrackId);
            Assert.AreEqual(7, definition.Milestones.Count);

            int[] expectedCoins = { 100, 150, 200, 250, 300, 400, 500 };
            for (int i = 0; i < 7; i++)
            {
                var milestone = definition.Milestones[i];
                Assert.AreEqual(i, milestone.Index);
                Assert.AreEqual(i + 1, milestone.RequiredPoints);
                Assert.AreEqual($"debut_day_{i + 1}", milestone.FreeReward.GrantId);

                var currencyReward = milestone.FreeReward.Rewards[0] as CurrencyReward;
                Assert.IsNotNull(currencyReward);
                Assert.AreEqual(CurrencyKind.Coins, currencyReward.Currency);
                Assert.AreEqual(expectedCoins[i], currencyReward.Amount);
            }
        }

        [Test]
        public void CanClaimDay_RejectsInvalidBoundsAndUnreachedDays()
        {
            var service = new DebutJourneyService();
            var track = new ProgressTrack(service.Definition);

            Assert.IsFalse(service.CanClaimDay(null, 0, 1));
            Assert.IsFalse(service.CanClaimDay(track, -1, 1));
            Assert.IsFalse(service.CanClaimDay(track, 7, 7));
            Assert.IsFalse(service.CanClaimDay(track, 0, 0));
            Assert.IsFalse(service.CanClaimDay(track, 1, 1));

            track.AddPoints(1);
            Assert.IsTrue(service.CanClaimDay(track, 0, 1));
            Assert.IsFalse(service.CanClaimDay(track, 1, 1));
        }

        [Test]
        public void ClaimDay_GrantsRewardIdempotently_AndRejectsInvalidCalls()
        {
            var service = new DebutJourneyService();
            var track = new ProgressTrack(service.Definition);
            var sink = new FakeRewardSink();

            track.AddPoints(1);

            Assert.IsFalse(service.ClaimDay(null, 0, 1, sink));
            Assert.IsFalse(service.ClaimDay(track, 0, 1, null));
            Assert.IsFalse(service.ClaimDay(track, 1, 1, sink));

            bool claimed = service.ClaimDay(track, 0, 1, sink);
            Assert.IsTrue(claimed);
            Assert.IsTrue(sink.HasGranted("debut_day_1"));

            bool claimedAgain = service.ClaimDay(track, 0, 1, sink);
            Assert.IsFalse(claimedAgain);
        }

        [Test]
        public void GetDaysStatus_ReturnsCorrectStatusForEachDay()
        {
            var service = new DebutJourneyService();
            var track = new ProgressTrack(service.Definition);
            var sink = new FakeRewardSink();

            var initialStatus = service.GetDaysStatus(track, currentLoginDay: 1);
            Assert.AreEqual(7, initialStatus.Count);
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(DebutDayStatus.Locked, initialStatus[i]);
            }

            track.AddPoints(2);
            var day2Status = service.GetDaysStatus(track, currentLoginDay: 2);
            Assert.AreEqual(DebutDayStatus.Available, day2Status[0]);
            Assert.AreEqual(DebutDayStatus.Available, day2Status[1]);
            Assert.AreEqual(DebutDayStatus.Locked, day2Status[2]);

            service.ClaimDay(track, 0, 2, sink);
            var afterClaim = service.GetDaysStatus(track, currentLoginDay: 2);
            Assert.AreEqual(DebutDayStatus.Claimed, afterClaim[0]);
            Assert.AreEqual(DebutDayStatus.Available, afterClaim[1]);
            Assert.AreEqual(DebutDayStatus.Locked, afterClaim[2]);
        }

        [Test]
        public void DebutJourneyView_DisplaysStatus_AndAllowsClaimingViaButton()
        {
            var view = CreateComponent<DebutJourneyView>("DebutJourneyView");
            var closeButton = CreateComponent<Button>("CloseButton");
            var claimButton = CreateComponent<Button>("ClaimButton");
            var titleText = CreateComponent<TextMeshProUGUI>("TitleText");
            var feedbackText = CreateComponent<TextMeshProUGUI>("FeedbackText");

            var statusTexts = new TMP_Text[7];
            for (int i = 0; i < 7; i++)
            {
                statusTexts[i] = CreateComponent<TextMeshProUGUI>($"Day_{i + 1}_Text");
            }

            view.BindComponents(closeButton, claimButton, statusTexts, titleText, feedbackText);

            var service = new DebutJourneyService();
            var track = new ProgressTrack(service.Definition);
            var sink = new FakeRewardSink();

            track.AddPoints(1);
            view.Initialize(service, track, currentLoginDay: 1, sink);
            view.Open();

            Assert.AreEqual(UILayerType.Modal, view.LayerType);
            Assert.IsTrue(view.IsOpen);
            Assert.IsTrue(claimButton.interactable);
            StringAssert.Contains("Disponivel", statusTexts[0].text);
            StringAssert.Contains("Bloqueado", statusTexts[1].text);

            int claimedDayIndex = -1;
            view.OnDayClaimed += day => claimedDayIndex = day;

            bool claimed = view.ClaimAvailableDay();
            Assert.IsTrue(claimed);
            Assert.AreEqual(0, claimedDayIndex);
            Assert.IsTrue(sink.HasGranted("debut_day_1"));
            StringAssert.Contains("Resgatado", statusTexts[0].text);
            Assert.IsFalse(claimButton.interactable);

            bool closeFired = false;
            view.OnCloseClicked += () => closeFired = true;
            closeButton.onClick.Invoke();
            Assert.IsTrue(closeFired);
            Assert.IsFalse(view.IsOpen);
        }
    }
}
