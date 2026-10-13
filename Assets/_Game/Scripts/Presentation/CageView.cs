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
            if (targetCage == null)
            {
                targetCage = GetComponent<Cage>();
            }

            if (targetCage != null)
            {
                targetCage.OnStateChanged -= HandleStateChanged;
                targetCage.OnStateChanged += HandleStateChanged;
            }

            UpdateVisuals();
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
                if (targetCage.IsOpen || targetCage.IsLost)
                {
                    hpText.text = "0";
                }
                else
                {
                    hpText.text = $"{targetCage.CurrentHealth}/{targetCage.MaxHealth}";
                }
            }

            if (survivorText != null)
            {
                if (targetCage.IsOpen)
                {
                    survivorText.text = "RESCUED";
                }
                else if (targetCage.IsLost)
                {
                    survivorText.text = "LOST";
                }
                else
                {
                    survivorText.text = $"+{targetCage.SurvivorCount}";
                }
            }
        }
    }
}
