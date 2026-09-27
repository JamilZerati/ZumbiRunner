using Game.Core.Perks;
using Game.Data;
using Game.Gameplay;
using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    public class GateView : MonoBehaviour
    {
        public static readonly Color PositiveColor = new Color(0.1f, 0.6f, 1f, 1f);
        public static readonly Color NegativeColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        public static readonly Color ConsumedColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Renderer panelRenderer;
        [SerializeField] private Renderer frameRenderer;

        private PerkDefinition currentPerk;
        private Color activeColor = PositiveColor;
        private Gate registeredGate;
        private GatePair registeredPair;

        public TMP_Text LabelText
        {
            get => labelText;
            set => labelText = value;
        }

        public Renderer PanelRenderer
        {
            get => panelRenderer;
            set => panelRenderer = value;
        }

        public Renderer FrameRenderer
        {
            get => frameRenderer;
            set => frameRenderer = value;
        }

        public bool IsConsumed { get; private set; }
        public PerkDefinition CurrentPerk => currentPerk;
        public Color CurrentColor { get; private set; } = PositiveColor;

        private void Start()
        {
            if (registeredGate == null)
            {
                registeredGate = GetComponent<Gate>();
            }

            if (registeredGate != null && registeredPair == null && registeredGate.ParentPair != null)
            {
                registeredPair = registeredGate.ParentPair;
                registeredPair.OnGateTriggered += HandleGatePairTriggered;
            }

            if (currentPerk == null && registeredGate != null && registeredGate.Perk != null)
            {
                SetPerk(registeredGate.Perk);
            }
        }

        public void Initialize(PerkDefinition perk, Gate gate = null)
        {
            if (registeredPair != null)
            {
                registeredPair.OnGateTriggered -= HandleGatePairTriggered;
                registeredPair = null;
            }

            registeredGate = gate;
            if (gate != null && gate.ParentPair != null)
            {
                registeredPair = gate.ParentPair;
                registeredPair.OnGateTriggered += HandleGatePairTriggered;
            }

            if (perk == null && gate != null)
            {
                perk = gate.Perk;
            }

            SetPerk(perk);
        }

        public void SetPerk(PerkDefinition perk)
        {
            currentPerk = perk;

            if (labelText != null)
            {
                labelText.text = perk != null ? perk.DisplayName : string.Empty;
            }

            bool isPositive = true;
            if (perk != null && !string.IsNullOrEmpty(perk.DisplayName))
            {
                string name = perk.DisplayName.Trim();
                if (name.StartsWith("-") || name.StartsWith("÷") || name.StartsWith("/"))
                {
                    isPositive = false;
                }
            }

            activeColor = isPositive ? PositiveColor : NegativeColor;
            CurrentColor = IsConsumed ? ConsumedColor : activeColor;
            ApplyColor(CurrentColor);
        }

        public void SetConsumed(bool consumed)
        {
            IsConsumed = consumed;
            CurrentColor = consumed ? ConsumedColor : activeColor;
            ApplyColor(CurrentColor);
        }

        private void HandleGatePairTriggered(int laneIndex, Gate triggeredGate)
        {
            SetConsumed(true);
        }

        private void ApplyColor(Color color)
        {
            SetRendererColor(panelRenderer, color);
            SetRendererColor(frameRenderer, color);

            if (labelText != null)
            {
                labelText.color = color;
            }
        }

        private static void SetRendererColor(Renderer rend, Color color)
        {
            if (rend == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                if (rend.material != null)
                {
                    rend.material.color = color;
                }
            }
            else
            {
                if (rend.sharedMaterial != null)
                {
                    rend.sharedMaterial.color = color;
                }
            }
        }

        private void OnDestroy()
        {
            if (registeredPair != null)
            {
                registeredPair.OnGateTriggered -= HandleGatePairTriggered;
                registeredPair = null;
            }
        }
    }
}
