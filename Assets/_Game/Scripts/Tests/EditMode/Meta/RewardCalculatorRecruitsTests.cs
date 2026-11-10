using System;
using Game.Core;
using Game.Core.Events;
using Game.Core.State;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    [TestFixture]
    public class RewardCalculatorRecruitsTests
    {
        private RewardCalculator _calculator;
        private Wallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _calculator = new RewardCalculator();
            _wallet = new Wallet(new EventBus());
        }

        [Test]
        public void CT_16_Calculate_WhenZeroRescuedAndZeroRecovered_DoesNotAddRecruitsReward()
        {
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 100,
                Rescued = 0,
                RecoveredSoldiers = 0
            };

            var bundle = _calculator.Calculate(result, null);

            Assert.IsNotNull(bundle);
            bool foundRecruits = false;
            foreach (var reward in bundle.Rewards)
            {
                if (reward is CurrencyReward cr && cr.Currency == CurrencyKind.Recruits)
                {
                    foundRecruits = true;
                }
            }

            Assert.IsFalse(foundRecruits, "Quando não houver recrutas resgatados ou recuperados, nenhuma recompensa de Recruits deve existir.");
        }

        [Test]
        public void CT_16_Calculate_WhenRescuedPositive_AddsRecruitsReward()
        {
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 100,
                Rescued = 5,
                RecoveredSoldiers = 0
            };

            var bundle = _calculator.Calculate(result, null);

            CurrencyReward recruitsReward = null;
            foreach (var reward in bundle.Rewards)
            {
                if (reward is CurrencyReward cr && cr.Currency == CurrencyKind.Recruits)
                {
                    recruitsReward = cr;
                    break;
                }
            }

            Assert.IsNotNull(recruitsReward);
            Assert.AreEqual(5, recruitsReward.Amount);
        }

        [Test]
        public void CT_16_Calculate_WhenRecoveredSoldiersPositive_AddsRecruitsReward()
        {
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 100,
                Rescued = 0,
                RecoveredSoldiers = 3
            };

            var bundle = _calculator.Calculate(result, null);

            CurrencyReward recruitsReward = null;
            foreach (var reward in bundle.Rewards)
            {
                if (reward is CurrencyReward cr && cr.Currency == CurrencyKind.Recruits)
                {
                    recruitsReward = cr;
                    break;
                }
            }

            Assert.IsNotNull(recruitsReward);
            Assert.AreEqual(3, recruitsReward.Amount);
        }

        [Test]
        public void CT_16_Calculate_CombinesRescuedAndRecoveredSoldiers()
        {
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 100,
                Rescued = 4,
                RecoveredSoldiers = 2
            };

            var bundle = _calculator.Calculate(result, null);

            CurrencyReward recruitsReward = null;
            foreach (var reward in bundle.Rewards)
            {
                if (reward is CurrencyReward cr && cr.Currency == CurrencyKind.Recruits)
                {
                    recruitsReward = cr;
                    break;
                }
            }

            Assert.IsNotNull(recruitsReward);
            Assert.AreEqual(6, recruitsReward.Amount); // 4 + 2 = 6
        }

        [Test]
        public void CT_16_WalletApplyBundle_WhenZeroRecruits_DoesNotThrowArgumentOutOfRangeException()
        {
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 50,
                Rescued = 0,
                RecoveredSoldiers = 0
            };

            var bundle = _calculator.Calculate(result, null);

            Assert.DoesNotThrow(() =>
            {
                foreach (var reward in bundle.Rewards)
                {
                    if (reward is CurrencyReward cr)
                    {
                        _wallet.Add(cr.Currency, cr.Amount, "RunReward");
                    }
                }
            });

            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Recruits));
            Assert.AreEqual(50, _wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void CT_16_WalletApplyBundle_WhenRecruitsPositive_CreditsToWallet()
        {
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 50,
                Rescued = 3,
                RecoveredSoldiers = 2
            };

            var bundle = _calculator.Calculate(result, null);

            foreach (var reward in bundle.Rewards)
            {
                if (reward is CurrencyReward cr)
                {
                    _wallet.Add(cr.Currency, cr.Amount, "RunReward");
                }
            }

            Assert.AreEqual(5, _wallet.GetBalance(CurrencyKind.Recruits));
            Assert.AreEqual(50, _wallet.GetBalance(CurrencyKind.Coins));
        }
    }
}
