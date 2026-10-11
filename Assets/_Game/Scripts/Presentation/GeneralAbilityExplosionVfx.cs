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
        private const float ArcHeight = 3.5f;
        private const int ArcSegments = 16;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform origin;
        [SerializeField, Min(0.05f)] private float duration = 0.45f;

        private IDisposable _usedSub;
        private float _elapsed = -1f;
        private LineRenderer _arcRenderer;
        private Material _arcMaterial;

        public SpriteRenderer Renderer => spriteRenderer;
        public LineRenderer ArcRenderer => _arcRenderer;
        public Transform Origin => origin;
        public float Duration => duration;
        public bool IsPlaying => _elapsed >= 0f;
        public int PlayedCount { get; private set; }
        public Vector3 LastOriginPosition { get; private set; }
        public Vector3 LastTargetPosition { get; private set; }

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
            Vector3 fallbackPos = origin != null ? origin.position : transform.position;
            Play(fallbackPos, fallbackPos);
        }

        public void Play(Vector3 originPosition, Vector3 targetPosition)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            LastOriginPosition = originPosition;
            LastTargetPosition = targetPosition;
            transform.position = targetPosition + Vector3.up * HeightAboveGround;

            SetupThrowArc(originPosition, targetPosition);

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

        private void SetupThrowArc(Vector3 originPosition, Vector3 targetPosition)
        {
            Vector3 flatDelta = new Vector3(targetPosition.x - originPosition.x, 0f, targetPosition.z - originPosition.z);
            if (flatDelta.sqrMagnitude < 0.01f)
            {
                if (_arcRenderer != null)
                {
                    _arcRenderer.enabled = false;
                }
                return;
            }

            EnsureArcRenderer();
            if (_arcRenderer == null)
            {
                return;
            }

            Vector3 start = originPosition + Vector3.up * HeightAboveGround;
            Vector3 end = targetPosition + Vector3.up * HeightAboveGround;
            _arcRenderer.positionCount = ArcSegments;

            for (int i = 0; i < ArcSegments; i++)
            {
                float t = i / (float)(ArcSegments - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                point.y += 4f * ArcHeight * t * (1f - t);
                _arcRenderer.SetPosition(i, point);
            }

            _arcRenderer.enabled = true;
        }

        private void EnsureArcRenderer()
        {
            if (_arcRenderer != null)
            {
                return;
            }

            var arcGo = new GameObject("ThrowArc");
            arcGo.transform.SetParent(transform, false);
            _arcRenderer = arcGo.AddComponent<LineRenderer>();
            _arcRenderer.useWorldSpace = true;
            _arcRenderer.startWidth = 0.18f;
            _arcRenderer.endWidth = 0.32f;
            _arcRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _arcRenderer.receiveShadows = false;

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                _arcMaterial = new Material(shader);
                _arcRenderer.sharedMaterial = _arcMaterial;
            }
        }

        private void Apply(float progress)
        {
            float easeOut = 1f - Mathf.Pow(1f - progress, 3f);
            float diameter = Mathf.Lerp(StartDiameter, GrenadeAbilityEffect.DefaultRadius * 2f, easeOut);
            float spriteWidth = spriteRenderer.sprite.bounds.size.x;
            float scale = spriteWidth > 0f ? diameter / spriteWidth : 1f;

            transform.localScale = new Vector3(scale, scale, 1f);
            FaceCamera(progress * SpinDegrees);

            float alpha = 1f - progress * progress;
            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;

            if (_arcRenderer != null && _arcRenderer.enabled)
            {
                _arcRenderer.startColor = new Color(1f, 0.85f, 0.2f, alpha * 0.6f);
                _arcRenderer.endColor = new Color(1f, 0.45f, 0.1f, alpha);
            }
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
            if (_arcRenderer != null)
            {
                _arcRenderer.enabled = false;
            }
        }

        private void OnAbilityUsed(AbilityUsedEvent evt)
        {
            if (evt.HasSpatialTarget)
            {
                Vector3 startPos = new Vector3(evt.OriginPosition.X, evt.OriginPosition.Y, evt.OriginPosition.Z);
                Vector3 targetPos = new Vector3(evt.TargetPosition.X, evt.TargetPosition.Y, evt.TargetPosition.Z);
                Play(startPos, targetPos);
                return;
            }

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

            if (_arcMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_arcMaterial);
                }
                else
                {
                    DestroyImmediate(_arcMaterial);
                }
            }
        }
    }
}
