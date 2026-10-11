using System;
using Game.Core;
using Game.Core.Events;
using UnityEngine;

namespace Game.Presentation
{
    public class GeneralAbilityAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip explosionClip;

        private IDisposable _usedSub;

        public AudioSource Source => audioSource;
        public AudioClip ExplosionClip => explosionClip;
        public int PlayedCount { get; private set; }

        public void Configure(AudioSource source, AudioClip clip)
        {
            audioSource = source;
            explosionClip = clip;
        }

        public void Initialize(IEventBus eventBus)
        {
            _usedSub?.Dispose();
            _usedSub = eventBus?.Subscribe<AbilityUsedEvent>(OnAbilityUsed);
        }

        private void OnAbilityUsed(AbilityUsedEvent evt)
        {
            if (audioSource == null || explosionClip == null)
            {
                return;
            }

            audioSource.PlayOneShot(explosionClip);
            PlayedCount++;
        }

        private void OnDestroy()
        {
            _usedSub?.Dispose();
            _usedSub = null;
        }
    }
}
