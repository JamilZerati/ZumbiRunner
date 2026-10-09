using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Policies;
using Game.Core.Events;
using Game.Core.State;
using Game.Data;
using Game.Gameplay.Abilities;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode
{
    public class GeneralAbilityHudTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private EventBus _eventBus;
        private GameObject _hudGo;
        private GeneralAbilityHud _hud;
        private Slider _slider;
        private TextMeshProUGUI _chargesText;
        private Button _manualBtn;
        private Button _modeBtn;
        private TextMeshProUGUI _modeText;
        private GeneralAbilityController _controller;
        private GeneralAbilityDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();

            var controllerGo = CreateTrackedGameObject("GeneralAbilityController");
            _controller = controllerGo.AddComponent<GeneralAbilityController>();

            _definition = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            _definition.SetData("grenade", "Granada", "Dano em area", 25, null, null);
            _controller.Initialize(_definition, _eventBus);

            _hudGo = CreateTrackedGameObject("GeneralAbilityHud");
            _hud = _hudGo.AddComponent<GeneralAbilityHud>();

            var sliderGo = CreateTrackedGameObject("ProgressSlider");
            sliderGo.transform.SetParent(_hudGo.transform, false);
            _slider = sliderGo.AddComponent<Slider>();

            var chargesGo = CreateTrackedGameObject("ChargesText");
            chargesGo.transform.SetParent(_hudGo.transform, false);
            _chargesText = chargesGo.AddComponent<TextMeshProUGUI>();

            var manualBtnGo = CreateTrackedGameObject("ManualButton");
            manualBtnGo.transform.SetParent(_hudGo.transform, false);
            _manualBtn = manualBtnGo.AddComponent<Button>();

            var modeBtnGo = CreateTrackedGameObject("ModeButton");
            modeBtnGo.transform.SetParent(_hudGo.transform, false);
            _modeBtn = modeBtnGo.AddComponent<Button>();

            var modeTextGo = CreateTrackedGameObject("ModeText");
            modeTextGo.transform.SetParent(modeBtnGo.transform, false);
            _modeText = modeTextGo.AddComponent<TextMeshProUGUI>();

            _hud.ConfigureComponents(_slider, _chargesText, _manualBtn, _modeBtn, _modeText);
        }

        [TearDown]
        public void TearDown()
        {
            if (_definition != null)
            {
                Object.DestroyImmediate(_definition);
                _definition = null;
            }

            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();
        }

        private GameObject CreateTrackedGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        private class FakeAbilitySettings : IAbilitySettings
        {
            public HeroAbilityTriggerMode TriggerMode { get; set; }

            public FakeAbilitySettings(HeroAbilityTriggerMode initialMode)
            {
                TriggerMode = initialMode;
            }

            public HeroAbilityTriggerMode LoadTriggerMode() => TriggerMode;

            public void SaveTriggerMode(HeroAbilityTriggerMode mode)
            {
                TriggerMode = mode;
            }
        }

        [Test]
        public void Initialize_ComSettingsAuto_ConfiguraPolicyAutoEVisualAuto()
        {
            var settings = new FakeAbilitySettings(HeroAbilityTriggerMode.Auto);

            _hud.Initialize(_controller, _eventBus, settings);

            Assert.AreEqual(HeroAbilityTriggerMode.Auto, _hud.CurrentMode);
            Assert.AreEqual("Auto", _modeText.text);
            Assert.AreEqual(HeroAbilityTriggerMode.Auto, _controller.TriggerPolicy.Mode);
            Assert.AreEqual(0, _hud.DisplayedCharges);
            Assert.IsFalse(_manualBtn.interactable);
        }

        [Test]
        public void Initialize_ComSettingsManual_ConfiguraPolicyManualEVisualManual()
        {
            var settings = new FakeAbilitySettings(HeroAbilityTriggerMode.Manual);

            _hud.Initialize(_controller, _eventBus, settings);

            Assert.AreEqual(HeroAbilityTriggerMode.Manual, _hud.CurrentMode);
            Assert.AreEqual("Manual", _modeText.text);
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, _controller.TriggerPolicy.Mode);
        }

        [Test]
        public void AbilityChargeProgressEvent_AtualizaSliderETextoDeCargas()
        {
            var settings = new FakeAbilitySettings(HeroAbilityTriggerMode.Auto);
            _hud.Initialize(_controller, _eventBus, settings);

            _eventBus.Publish(new AbilityChargeProgressEvent("grenade", currentKills: 15, targetKills: 25, currentCharges: 0));

            Assert.AreEqual(15f, _slider.value);
            Assert.AreEqual(25f, _slider.maxValue);
            Assert.AreEqual("Cargas: 0", _chargesText.text);
            Assert.IsFalse(_manualBtn.interactable);

            _eventBus.Publish(new AbilityChargeProgressEvent("grenade", currentKills: 25, targetKills: 25, currentCharges: 2));

            Assert.AreEqual(25f, _slider.value);
            Assert.AreEqual("Cargas: 2", _chargesText.text);
            Assert.IsTrue(_manualBtn.interactable);
        }

        [Test]
        public void AbilityUsedEvent_AtualizaCargasRestantesEInteratividadeDoBotao()
        {
            var settings = new FakeAbilitySettings(HeroAbilityTriggerMode.Auto);
            _hud.Initialize(_controller, _eventBus, settings);

            _eventBus.Publish(new AbilityChargeProgressEvent("grenade", 0, 25, 1));
            Assert.IsTrue(_manualBtn.interactable);

            _eventBus.Publish(new AbilityUsedEvent("grenade", remainingCharges: 0));

            Assert.AreEqual(0, _hud.DisplayedCharges);
            Assert.AreEqual("Cargas: 0", _chargesText.text);
            Assert.IsFalse(_manualBtn.interactable);
        }

        [Test]
        public void BotaoManual_ComCargasDisponiveis_InvocaTriggerAbilityNoController()
        {
            var settings = new FakeAbilitySettings(HeroAbilityTriggerMode.Manual);
            _hud.Initialize(_controller, _eventBus, settings);

            var effect = new FakeAbilityEffect();
            _definition.SetData("grenade", "Granada", "Area", 25, effect, null);
            _controller.Initialize(_definition, _eventBus, new ManualHeroAbilityTriggerPolicy(), new RunConfig { BonusAbilityCharges = 1 });

            Assert.AreEqual(1, _controller.CurrentCharges);
            Assert.IsTrue(_manualBtn.interactable);

            _manualBtn.onClick.Invoke();

            Assert.IsTrue(effect.WasExecuted);
            Assert.AreEqual(0, _controller.CurrentCharges);
            Assert.IsFalse(_manualBtn.interactable);
        }

        [Test]
        public void AlternarModo_ToggleMode_AlternaEntreAutoEManual_AtualizaPolicyEPersisteSettings()
        {
            var settings = new FakeAbilitySettings(HeroAbilityTriggerMode.Auto);
            _hud.Initialize(_controller, _eventBus, settings);

            _modeBtn.onClick.Invoke();

            Assert.AreEqual(HeroAbilityTriggerMode.Manual, _hud.CurrentMode);
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, settings.TriggerMode);
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, _controller.TriggerPolicy.Mode);
            Assert.AreEqual("Manual", _modeText.text);

            _modeBtn.onClick.Invoke();

            Assert.AreEqual(HeroAbilityTriggerMode.Auto, _hud.CurrentMode);
            Assert.AreEqual(HeroAbilityTriggerMode.Auto, settings.TriggerMode);
            Assert.AreEqual(HeroAbilityTriggerMode.Auto, _controller.TriggerPolicy.Mode);
            Assert.AreEqual("Auto", _modeText.text);
        }

        [Test]
        public void PlayerPrefsAbilitySettings_PersisteECarregaValorCorretamente()
        {
            const string testKey = "Test.Ability.TriggerMode";
            PlayerPrefs.DeleteKey(testKey);

            try
            {
                var settings = new PlayerPrefsAbilitySettings(testKey, HeroAbilityTriggerMode.Auto);

                Assert.AreEqual(HeroAbilityTriggerMode.Auto, settings.LoadTriggerMode());

                settings.SaveTriggerMode(HeroAbilityTriggerMode.Manual);
                Assert.AreEqual(HeroAbilityTriggerMode.Manual, settings.LoadTriggerMode());

                settings.SaveTriggerMode(HeroAbilityTriggerMode.Auto);
                Assert.AreEqual(HeroAbilityTriggerMode.Auto, settings.LoadTriggerMode());
            }
            finally
            {
                PlayerPrefs.DeleteKey(testKey);
            }
        }

        [Test]
        public void OnDestroy_RemoveAssinaturasDoEventBus()
        {
            var settings = new FakeAbilitySettings(HeroAbilityTriggerMode.Auto);
            _hud.Initialize(_controller, _eventBus, settings);

            typeof(GeneralAbilityHud)
                .GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(_hud, null);

            _eventBus.Publish(new AbilityChargeProgressEvent("grenade", currentKills: 20, targetKills: 25, currentCharges: 3));

            Assert.AreEqual(0, _hud.DisplayedCharges);
            Assert.AreEqual(0, _hud.DisplayedKills);
        }

        private class FakeAbilityEffect : IAbilityEffect
        {
            public string Description => "Fake effect";
            public bool WasExecuted { get; private set; }

            public void Execute(AbilityExecutionContext context)
            {
                WasExecuted = true;
            }
        }
    }
}
