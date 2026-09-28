using UnityEngine;

namespace Game.Presentation
{
    public class ProjectileView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshFilter meshFilter;

        public MeshRenderer MeshRenderer => meshRenderer;
        public MeshFilter MeshFilter => meshFilter;

        public void SetupVisuals(Color color)
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                }
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }
            }

            if (meshFilter.sharedMesh == null)
            {
                var tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                meshFilter.sharedMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
                if (Application.isPlaying)
                {
                    Destroy(tempSphere);
                }
                else
                {
                    DestroyImmediate(tempSphere);
                }
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            mat.color = color;

            if (Application.isPlaying)
            {
                meshRenderer.material = mat;
            }
            else
            {
                meshRenderer.sharedMaterial = mat;
            }
        }
    }
}
