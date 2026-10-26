using System;
using System.Collections.Generic;
using Game.Meta.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class StageSelectionWidget : MonoBehaviour
    {
        [Serializable] public class StageItemBinding { public string StageId; public Button Button; public TMP_Text StarsText; public GameObject LockIndicator; }

        [SerializeField] private TMP_Text _stageTitleText, _starsText;
        [SerializeField] private GameObject _lockIndicator;
        [SerializeField] private Button _prevButton, _nextButton;
        [SerializeField] private List<StageItemBinding> _stageBindings = new();

        private IStageMapService _stageMapService;
        private readonly List<string> _availableStages = new();
        private string _selectedStageId;

        public event Action<string> OnStageSelected;
        public string SelectedStageId => _selectedStageId;
        public IReadOnlyList<string> AvailableStages => _availableStages;
        public TMP_Text StageTitleText => _stageTitleText;
        public TMP_Text StarsText => _starsText;
        public GameObject LockIndicator => _lockIndicator;
        public Button PrevButton => _prevButton;
        public Button NextButton => _nextButton;

        private void Awake() => WireButtons();
        private void OnDestroy() { UnwireButtons(); OnStageSelected = null; }

        public void BindComponents(TMP_Text title = null, TMP_Text stars = null, GameObject lockObj = null, Button prev = null, Button next = null, IEnumerable<StageItemBinding> bindings = null)
        {
            UnwireButtons();
            if (title != null) _stageTitleText = title;
            if (stars != null) _starsText = stars;
            if (lockObj != null) _lockIndicator = lockObj;
            if (prev != null) _prevButton = prev;
            if (next != null) _nextButton = next;
            if (bindings != null) { _stageBindings.Clear(); _stageBindings.AddRange(bindings); }
            WireButtons();
            Refresh();
        }

        public void Initialize(IStageMapService stageMapService, IEnumerable<string> stages = null)
        {
            _stageMapService = stageMapService;
            _availableStages.Clear();
            if (stages != null) _availableStages.AddRange(stages);
            else _availableStages.AddRange(new[] { "stage_01", "stage_02", "stage_03", "stage_04", "stage_05" });

            string highest = _stageMapService?.GetHighestUnlockedStage();
            if (!string.IsNullOrEmpty(highest) && IsStageUnlocked(highest)) _selectedStageId = highest;
            else if (_availableStages.Count > 0) _selectedStageId = _availableStages[0];
            Refresh();
        }

        public bool SelectStage(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId) || !IsStageUnlocked(stageId)) return false;
            _selectedStageId = stageId;
            Refresh();
            OnStageSelected?.Invoke(_selectedStageId);
            return true;
        }

        public bool IsStageUnlocked(string stageId) => !string.IsNullOrWhiteSpace(stageId) && (_stageMapService == null || _stageMapService.IsStageUnlocked(stageId));
        public int GetStageStars(string stageId) => (!string.IsNullOrWhiteSpace(stageId) && _stageMapService != null) ? _stageMapService.GetStars(stageId) : 0;

        public void Refresh()
        {
            bool hasSelection = !string.IsNullOrEmpty(_selectedStageId);
            bool unlocked = hasSelection && IsStageUnlocked(_selectedStageId);
            if (_stageTitleText != null) _stageTitleText.text = hasSelection ? _selectedStageId : string.Empty;
            if (_starsText != null) _starsText.text = (hasSelection ? GetStageStars(_selectedStageId) : 0).ToString();
            if (_lockIndicator != null) _lockIndicator.SetActive(!unlocked);

            int idx = _availableStages.IndexOf(_selectedStageId);
            if (_prevButton != null) _prevButton.interactable = idx > 0;
            if (_nextButton != null) _nextButton.interactable = idx >= 0 && idx < _availableStages.Count - 1;

            for (int i = 0; i < _stageBindings.Count; i++)
            {
                var binding = _stageBindings[i];
                if (binding == null || string.IsNullOrEmpty(binding.StageId)) continue;
                bool itemUnlocked = IsStageUnlocked(binding.StageId);
                if (binding.Button != null) binding.Button.interactable = itemUnlocked;
                if (binding.StarsText != null) binding.StarsText.text = GetStageStars(binding.StageId).ToString();
                if (binding.LockIndicator != null) binding.LockIndicator.SetActive(!itemUnlocked);
            }
        }

        public void SelectNext() { int idx = _availableStages.IndexOf(_selectedStageId); if (idx >= 0 && idx + 1 < _availableStages.Count) SelectStage(_availableStages[idx + 1]); }
        public void SelectPrevious() { int idx = _availableStages.IndexOf(_selectedStageId); if (idx > 0) SelectStage(_availableStages[idx - 1]); }

        private void WireButtons()
        {
            _prevButton?.onClick.AddListener(SelectPrevious);
            _nextButton?.onClick.AddListener(SelectNext);
            for (int i = 0; i < _stageBindings.Count; i++)
            {
                var b = _stageBindings[i];
                if (b?.Button != null && !string.IsNullOrEmpty(b.StageId)) { string id = b.StageId; b.Button.onClick.AddListener(() => SelectStage(id)); }
            }
        }

        private void UnwireButtons()
        {
            _prevButton?.onClick.RemoveListener(SelectPrevious);
            _nextButton?.onClick.RemoveListener(SelectNext);
            for (int i = 0; i < _stageBindings.Count; i++) _stageBindings[i]?.Button?.onClick.RemoveAllListeners();
        }
    }
}
