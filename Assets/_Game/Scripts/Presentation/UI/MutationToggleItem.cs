using System;
using Game.Data.Mutations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class MutationToggleItem : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _bonusText;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private GameObject _selectedIndicator;

        private Action<string> _onToggled;

        public string MutationId { get; private set; }
        public bool IsSelected { get; private set; }
        public Image IconImage => _iconImage;
        public TMP_Text NameText => _nameText;
        public TMP_Text BonusText => _bonusText;
        public Button ToggleButton => _toggleButton;
        public GameObject SelectedIndicator => _selectedIndicator;

        private void Awake()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(HandleClick);
            }
            _onToggled = null;
        }

        public void BindComponents(Image icon = null, TMP_Text name = null, TMP_Text bonus = null, Button toggle = null, GameObject indicator = null)
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(HandleClick);
            }

            if (icon != null) _iconImage = icon;
            if (name != null) _nameText = name;
            if (bonus != null) _bonusText = bonus;
            if (toggle != null) _toggleButton = toggle;
            if (indicator != null) _selectedIndicator = indicator;

            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(HandleClick);
            }
        }

        public void Bind(MutationDefinition definition, bool isSelected, Action<string> onToggled)
        {
            MutationId = definition != null ? definition.Id : string.Empty;
            _onToggled = onToggled;

            if (definition != null)
            {
                if (_nameText != null) _nameText.text = definition.DisplayName;
                if (_bonusText != null) _bonusText.text = $"+{definition.RewardBonus * 100f:0.#}%";
                if (_iconImage != null)
                {
                    _iconImage.sprite = definition.Icon;
                    _iconImage.enabled = definition.Icon != null;
                }
            }

            SetSelected(isSelected);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (_selectedIndicator != null)
            {
                _selectedIndicator.SetActive(selected);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_toggleButton != null)
            {
                _toggleButton.interactable = interactable;
            }
        }

        private void HandleClick()
        {
            if (!string.IsNullOrEmpty(MutationId))
            {
                _onToggled?.Invoke(MutationId);
            }
        }
    }
}
