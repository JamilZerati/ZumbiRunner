using System.Collections.Generic;
using Game.Data.Mutations;
using Game.Meta.Progression;
using TMPro;
using UnityEngine;

namespace Game.Presentation.UI
{
    public class MutationSelectionWidget : MonoBehaviour
    {
        public const int MaxSelectedMutations = 3;

        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private MutationToggleItem _itemPrefab;
        [SerializeField] private TMP_Text _totalBonusText;
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private TMP_Text _lockedReasonText;

        private readonly List<MutationToggleItem> _instantiatedItems = new();
        private readonly List<string> _selectedMutations = new();
        private IMutationCatalog _catalog;
        private IProgressionService _progressionService;

        public bool AreMutationsUnlocked => _progressionService != null && _progressionService.AreMutationsUnlocked;
        public IReadOnlyList<string> SelectedMutationIds => _selectedMutations;
        public IReadOnlyList<MutationToggleItem> Items
        {
            get
            {
                if (_instantiatedItems.Count == 0 && _itemsContainer != null)
                {
                    _instantiatedItems.AddRange(_itemsContainer.GetComponentsInChildren<MutationToggleItem>(true));
                }
                return _instantiatedItems;
            }
        }
        public Transform ItemsContainer => _itemsContainer;
        public MutationToggleItem ItemPrefab => _itemPrefab;
        public TMP_Text TotalBonusText => _totalBonusText;
        public GameObject LockedOverlay => _lockedOverlay;
        public TMP_Text LockedReasonText => _lockedReasonText;

        public float TotalBonusPercentage
        {
            get
            {
                if (_catalog == null || _selectedMutations.Count == 0) return 0f;
                float total = 0f;
                for (int i = 0; i < _selectedMutations.Count; i++)
                {
                    string id = _selectedMutations[i];
                    if (!string.IsNullOrEmpty(id) && _catalog.TryGet(id, out var def) && def != null)
                    {
                        total += def.RewardBonus;
                    }
                }
                return total;
            }
        }

        public void BindComponents(
            Transform itemsContainer = null,
            MutationToggleItem itemPrefab = null,
            TMP_Text totalBonusText = null,
            GameObject lockedOverlay = null,
            TMP_Text lockedReasonText = null,
            IEnumerable<MutationToggleItem> explicitItems = null)
        {
            if (itemsContainer != null) _itemsContainer = itemsContainer;
            if (itemPrefab != null) _itemPrefab = itemPrefab;
            if (totalBonusText != null) _totalBonusText = totalBonusText;
            if (lockedOverlay != null) _lockedOverlay = lockedOverlay;
            if (lockedReasonText != null) _lockedReasonText = lockedReasonText;

            if (explicitItems != null)
            {
                _instantiatedItems.Clear();
                _instantiatedItems.AddRange(explicitItems);
            }

            Refresh();
        }

        public void Initialize(IMutationCatalog catalog, IProgressionService progressionService)
        {
            _catalog = catalog;
            _progressionService = progressionService;
            _selectedMutations.Clear();

            if (_instantiatedItems.Count == 0 && _itemsContainer != null)
            {
                _instantiatedItems.AddRange(_itemsContainer.GetComponentsInChildren<MutationToggleItem>(true));
            }

            if (_itemPrefab != null && _itemsContainer != null && _catalog != null)
            {
                ClearInstantiatedItems();

                var entries = _catalog.Entries ?? _catalog.Mutations;
                if (entries != null)
                {
                    for (int i = 0; i < entries.Count; i++)
                    {
                        var def = entries[i];
                        if (def == null) continue;

                        var item = Instantiate(_itemPrefab, _itemsContainer);
                        item.Bind(def, false, id => ToggleMutation(id));
                        _instantiatedItems.Add(item);
                    }
                }
            }
            else if (_instantiatedItems.Count > 0 && _catalog != null)
            {
                var entries = _catalog.Entries ?? _catalog.Mutations;
                for (int i = 0; i < _instantiatedItems.Count; i++)
                {
                    var item = _instantiatedItems[i];
                    if (item == null) continue;

                    MutationDefinition def = null;
                    if (!string.IsNullOrEmpty(item.MutationId))
                    {
                        _catalog.TryGet(item.MutationId, out def);
                    }
                    else if (entries != null && i < entries.Count)
                    {
                        def = entries[i];
                    }

                    if (def != null)
                    {
                        item.Bind(def, _selectedMutations.Contains(def.Id), id => ToggleMutation(id));
                    }
                }
            }

            Refresh();
        }

        public bool ToggleMutation(string mutationId)
        {
            if (string.IsNullOrWhiteSpace(mutationId) || !AreMutationsUnlocked)
            {
                return false;
            }

            if (_selectedMutations.Contains(mutationId))
            {
                _selectedMutations.Remove(mutationId);
                Refresh();
                return true;
            }

            if (_selectedMutations.Count >= MaxSelectedMutations)
            {
                // Rejeita a seleção de novas mutações além do teto de 3 (CT-9)
                return false;
            }

            _selectedMutations.Add(mutationId);
            Refresh();
            return true;
        }

        public void Refresh()
        {
            bool unlocked = AreMutationsUnlocked;
            if (_lockedOverlay != null)
            {
                _lockedOverlay.SetActive(!unlocked);
            }

            if (_lockedReasonText != null && !unlocked)
            {
                _lockedReasonText.text = "Derrote o Boss 1 para desbloquear mutações";
            }

            for (int i = 0; i < _instantiatedItems.Count; i++)
            {
                var item = _instantiatedItems[i];
                if (item != null)
                {
                    item.SetSelected(_selectedMutations.Contains(item.MutationId));
                    item.SetInteractable(unlocked);
                }
            }

            UpdateBonusText();
        }

        private void UpdateBonusText()
        {
            if (_totalBonusText != null)
            {
                _totalBonusText.text = $"+{TotalBonusPercentage * 100f:0.#}% Moedas";
            }
        }

        private void ClearInstantiatedItems()
        {
            for (int i = _instantiatedItems.Count - 1; i >= 0; i--)
            {
                var item = _instantiatedItems[i];
                if (item != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(item.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(item.gameObject);
                    }
                }
            }
            _instantiatedItems.Clear();
        }
    }
}
