using System;
using System.Runtime.CompilerServices;
using Game.Core;
using Game.Core.Events;
using TMPro;
using UnityEngine;

[assembly: InternalsVisibleTo("Game.Editor")]
[assembly: InternalsVisibleTo("Game.Tests.EditMode")]

namespace Game.Presentation
{
    public class SquadCountHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private string format = "Tropa: {0}";

        private IEventBus eventBus;
        private IDisposable squadSizeSubscription;

        public int DisplayedCount { get; private set; }
        public TMP_Text CountText => countText;
        public string Format => format;

        public void Initialize(IEventBus bus = null, int initialCount = 1)
        {
            if (squadSizeSubscription != null)
            {
                squadSizeSubscription.Dispose();
                squadSizeSubscription = null;
            }

            eventBus = bus;

            if (eventBus != null)
            {
                squadSizeSubscription = eventBus.Subscribe<SquadSizeChangedEvent>(OnSquadSizeChanged);
            }

            SetCount(initialCount);
        }

        private void OnSquadSizeChanged(SquadSizeChangedEvent evt)
        {
            SetCount(evt.NewCount);
        }

        public void SetCount(int count)
        {
            DisplayedCount = count;
            if (countText != null)
            {
                countText.text = string.Format(format, count);
            }
        }

        internal void SetCountText(TMP_Text text)
        {
            countText = text;
        }

        private void OnDestroy()
        {
            if (squadSizeSubscription != null)
            {
                squadSizeSubscription.Dispose();
                squadSizeSubscription = null;
            }
        }
    }
}
