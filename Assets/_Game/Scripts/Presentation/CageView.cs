using Game.Gameplay;
using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    public class CageView : MonoBehaviour
    {
        [SerializeField] private Cage targetCage;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text survivorText;

        public Cage TargetCage
        {
            get => targetCage;
            set
            {
                if (targetCage != null)
                {
                    targetCage.OnStateChanged -= HandleStateChanged;
                }
                targetCage = value;
                if (targetCage != null)
                {
                    targetCage.OnStateChanged += HandleStateChanged;
                }
                UpdateVisuals();
            }
        }

        public TMP_Text HpText
        {
            get => hpText;
            set => hpText = value;
        }

        public TMP_Text SurvivorText
        {
            get => survivorText;
            set => survivorText = value;
        }

        private void Start()
        {
            TargetCage = targetCage != null ? targetCage : GetComponent<Cage>();
        }

        private void OnDestroy()
        {
            if (targetCage != null)
            {
                targetCage.OnStateChanged -= HandleStateChanged;
            }
        }

        public void Bind(Cage cage)
        {
            TargetCage = cage;
        }

        private void HandleStateChanged(Cage cage)
        {
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (targetCage == null)
            {
                return;
            }

            if (hpText != null)
            {
                hpText.text = targetCage.IsOpen || targetCage.IsLost ? "0" : $"{targetCage.CurrentHealth}/{targetCage.MaxHealth}";
            }

            if (survivorText != null)
            {
                survivorText.text = targetCage.IsOpen ? "RESCUED" : (targetCage.IsLost ? "LOST" : $"+{targetCage.SurvivorCount}");
            }
        }
    }
}
