using Game.Core.Status;
using Game.Gameplay;
using UnityEngine;

namespace Game.Presentation
{
    public class EnemyStatusTint : MonoBehaviour
    {
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        private static readonly Color FrozenColor = Color.cyan;
        private static readonly Color BurnColor = new Color(1f, 0.5f, 0f);
        private static readonly Color PoisonColor = Color.green;
        private static readonly Color SlowColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color FreezeColor = new Color(0.8f, 0.9f, 1f);

        [SerializeField] private EnemyController enemy;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color baseColor = Color.white;

        private MaterialPropertyBlock _propBlock;
        private bool _initialized;

        public static Color ColorFor(StatusEffectController status, Color baseColor)
        {
            if (status == null)
            {
                return baseColor;
            }

            if (status.Has(StatusKind.Frozen))
            {
                return FrozenColor;
            }
            if (status.Has(StatusKind.Burn))
            {
                return BurnColor;
            }
            if (status.Has(StatusKind.Poison))
            {
                return PoisonColor;
            }
            if (status.Has(StatusKind.Slow))
            {
                return SlowColor;
            }
            if (status.Has(StatusKind.Freeze))
            {
                return FreezeColor;
            }

            return baseColor;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (enemy == null)
            {
                enemy = GetComponent<EnemyController>();
            }
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }
            if (targetRenderer != null && targetRenderer.sharedMaterial != null)
            {
                if (targetRenderer.sharedMaterial.HasProperty(BaseColorPropertyId))
                {
                    baseColor = targetRenderer.sharedMaterial.GetColor(BaseColorPropertyId);
                }
                else if (targetRenderer.sharedMaterial.HasProperty(ColorPropertyId))
                {
                    baseColor = targetRenderer.sharedMaterial.GetColor(ColorPropertyId);
                }
            }
            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }

            if (enemy != null && targetRenderer != null)
            {
                _initialized = true;
            }
        }

        private void LateUpdate()
        {
            EnsureInitialized();
            if (enemy == null || targetRenderer == null)
            {
                return;
            }

            Color color = ColorFor(enemy.Status, baseColor);
            targetRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorPropertyId, color);
            _propBlock.SetColor(ColorPropertyId, color);
            targetRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
