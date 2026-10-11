using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Presentation
{
    public class AbilityAimIndicator : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _renderer;
        private Material _material;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Color _normalColor = new Color(0f, 1f, 1f, 0.5f);
        private readonly Color _cancelColor = new Color(1f, 0f, 0f, 0.5f);

        public bool IsVisible => _renderer != null && _renderer.enabled;
        public bool IsCanceling { get; private set; }

        public void Initialize()
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<MeshRenderer>(true);
            }

            if (_renderer == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.transform.SetParent(transform, false);

                var collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyImmediate(collider);
                }

                _renderer = go.GetComponent<MeshRenderer>();
            }

            _renderer.enabled = false;
            EnsureMaterial();
            UpdateColor();
        }

        private void Awake()
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<MeshRenderer>(true);
            }

            if (_renderer != null)
            {
                _renderer.enabled = false;
                EnsureMaterial();
                UpdateColor();
            }
        }

        private void EnsureMaterial()
        {
            if (_material != null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            _material = new Material(shader);
            _material.SetFloat("_Surface", 1f);
            _material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_ZWrite", 0);
            _material.renderQueue = (int)RenderQueue.Transparent;

            if (_renderer != null)
            {
                _renderer.sharedMaterial = _material;
            }
        }

        public void Show(float radius)
        {
            if (_renderer == null)
            {
                Initialize();
            }
            else
            {
                EnsureMaterial();
            }

            if (_renderer != null)
            {
                _renderer.enabled = true;
            }

            float diameter = radius * 2f;
            transform.localScale = new Vector3(diameter, 0.1f, diameter);

            IsCanceling = false;
            UpdateColor();
        }

        public void Hide()
        {
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }
        }

        public void UpdateAim(Vector3 worldPos, bool isCanceling)
        {
            transform.position = new Vector3(worldPos.x, 0.05f, worldPos.z);

            if (IsCanceling != isCanceling)
            {
                IsCanceling = isCanceling;
                UpdateColor();
            }
        }

        private void UpdateColor()
        {
            EnsureMaterial();
            if (_material != null)
            {
                var targetColor = IsCanceling ? _cancelColor : _normalColor;
                _material.SetColor(BaseColorId, targetColor);
                _material.SetColor(ColorId, targetColor);
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_material);
                }
                else
                {
                    DestroyImmediate(_material);
                }
            }
        }
    }
}
