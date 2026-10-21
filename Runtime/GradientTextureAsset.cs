using UnityEngine;

namespace GradientTextureGenerator.Runtime
{
    public class GradientTextureAsset : ScriptableObject
    {
        [SerializeField]
        private Texture2D _texture;

        [SerializeField]
        private Gradient _gradient = new();
        
        public Texture2D Texture => _texture;
        
        public Gradient Gradient => _gradient;
        
        public void Configure(Texture2D texture, Gradient gradient)
        {
            _texture = texture;
            _gradient = gradient;
        }
    }
}