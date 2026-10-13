using Game.Gameplay;
using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    public class BarricadeView : MonoBehaviour
    {
        [SerializeField] private Barricade targetBarricade;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text statusText;

        public Barricade TargetBarricade
        {
            get => targetBarricade;
            set
            {
                if (targetBarricade != null)
                {
                    targetBarricade.OnStateChanged -= HandleStateChanged;
                    targetBarricade.OnDamaged -= HandleDamaged;
                    targetBarricade.OnDestroyed -= HandleDestroyed;
                }

                targetBarricade = value;

                if (targetBarricade != null)
                {
                    targetBarricade.OnStateChanged += HandleStateChanged;
                    targetBarricade.OnDamaged += HandleDamaged;
                    targetBarricade.OnDestroyed += HandleDestroyed;
                }

                UpdateVisuals();
            }
        }

        public TMP_Text HpText
        {
            get => hpText;
            set => hpText = value;
        }

        public TMP_Text StatusText
        {
            get => statusText;
            set => statusText = value;
        }

        private void Start()
        {
            if (targetBarricade == null)
            {
                TargetBarricade = GetComponent<Barricade>();
            }
            else
            {
                TargetBarricade = targetBarricade;
            }
        }

        private void OnDestroy()
        {
            if (targetBarricade != null)
            {
                targetBarricade.OnStateChanged -= HandleStateChanged;
                targetBarricade.OnDamaged -= HandleDamaged;
                targetBarricade.OnDestroyed -= HandleDestroyed;
            }
        }

        public void Bind(Barricade barricade)
        {
            TargetBarricade = barricade;
        }

        private void HandleStateChanged(Barricade barricade)
        {
            UpdateVisuals();
        }

        private void HandleDamaged(Barricade barricade, int damage)
        {
            UpdateVisuals();
        }

        private void HandleDestroyed(Barricade barricade)
        {
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (targetBarricade == null)
            {
                return;
            }

            if (hpText != null)
            {
                hpText.text = targetBarricade.IsDestroyed
                    ? "0"
                    : $"{targetBarricade.CurrentHp}/{targetBarricade.MaxHp}";
            }

            if (statusText != null)
            {
                statusText.text = targetBarricade.IsDestroyed
                    ? "DESTROYED"
                    : "DEFENDING";
            }
        }
    }
}
