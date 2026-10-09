using UnityEngine;

namespace Game.Presentation
{
    public class AbilityAimIndicator : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private Material _material;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Color _normalColor = new Color(0f, 1f, 1f, 0.5f); // Cyan semi-transparent
        private Color _cancelColor = new Color(1f, 0f, 0f, 0.5f); // Red semi-transparent

        public bool IsVisible => _renderer != null && _renderer.enabled;
        public bool IsCanceling { get; private set; }

        public void Initialize()
        {
            // Cria um cilindro primitivo achatado como indicador
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(transform, false);
            
            // Remove o colisor para no interferir na fsica
            var collider = go.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);

            _renderer = go.GetComponent<MeshRenderer>();
            _renderer.enabled = false;

            _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _material.SetFloat("_Surface", 1); // Transparent
            _material.SetColor(BaseColorId, _normalColor);
            _renderer.material = _material;
        }

        public void Show(float radius)
        {
            if (_renderer != null)
            {
                _renderer.enabled = true;
            }

            // Primitive cylinder has a diameter of 1
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
            if (_material != null)
            {
                _material.SetColor(BaseColorId, IsCanceling ? _cancelColor : _normalColor);
            }
        }
    }
}
