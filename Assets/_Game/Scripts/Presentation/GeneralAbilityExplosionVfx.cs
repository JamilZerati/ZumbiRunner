using System;
using Game.Core;
using Game.Core.Abilities.Effects;
using Game.Core.Events;
using UnityEngine;

namespace Game.Presentation
{
    public class GeneralAbilityExplosionVfx : MonoBehaviour
    {
        private const float StartDiameter = 0.5f;
        private const float HeightAboveGround = 1f;
        private const float SpinDegrees = 35f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform origin;
        [SerializeField, Min(0.05f)] private float duration = 0.45f;

        private IDisposable _usedSub;
        private float _elapsed = -1f;

        public SpriteRenderer Renderer => spriteRenderer;
        public Transform Origin => origin;
        public float Duration => duration;
        public bool IsPlaying => _elapsed >= 0f;
        public int PlayedCount { get; private set; }

        public void Configure(SpriteRenderer renderer, Transform burstOrigin)
        {
            spriteRenderer = renderer;
            origin = burstOrigin;
        }

        public void Initialize(IEventBus eventBus)
        {
            _usedSub?.Dispose();
            _usedSub = eventBus?.Subscribe<AbilityUsedEvent>(OnAbilityUsed);
            Stop();
        }

        public void Play()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            if (origin != null)
            {
                transform.position = origin.position + Vector3.up * HeightAboveGround;
            }

            _elapsed = 0f;
            PlayedCount++;
            spriteRenderer.enabled = true;
            Apply(0f);
        }

        public void Tick(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            _elapsed += deltaTime;
            float progress = Mathf.Clamp01(_elapsed / duration);
            if (progress >= 1f)
            {
                Stop();
                return;
            }

            Apply(progress);
        }

        private void Apply(float progress)
        {
            float easeOut = 1f - Mathf.Pow(1f - progress, 3f);
            float diameter = Mathf.Lerp(StartDiameter, GrenadeAbilityEffect.DefaultRadius * 2f, easeOut);
            float spriteWidth = spriteRenderer.sprite.bounds.size.x;
            float scale = spriteWidth > 0f ? diameter / spriteWidth : 1f;

            transform.localScale = new Vector3(scale, scale, 1f);
            FaceCamera(progress * SpinDegrees);

            var color = spriteRenderer.color;
            color.a = 1f - progress * progress;
            spriteRenderer.color = color;
        }

        private void FaceCamera(float spin)
        {
            var cam = Camera.main;
            var facing = cam != null ? cam.transform.rotation : Quaternion.identity;
            transform.rotation = facing * Quaternion.Euler(0f, 0f, spin);
        }

        private void Stop()
        {
            _elapsed = -1f;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        private void OnAbilityUsed(AbilityUsedEvent evt)
        {
            Play();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _usedSub?.Dispose();
            _usedSub = null;
        }
    }
}
