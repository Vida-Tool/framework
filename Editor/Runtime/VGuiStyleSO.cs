using UnityEngine;

namespace Vida.Framework
{
    
    [CreateAssetMenu(fileName = "VGuiStyleSO")]
    public class VGuiStyleSO : ScriptableObject
    {
        [field:SerializeField] public Color TextPrimary { get; set; } = new Color();
        [field:SerializeField] public Color TextHeader {get ; set;} = new Color();
        [field:SerializeField] public Color TextOther { get; set; } = new Color(0.92f, 0.92f, 0.92f, 1f);
        
        
        [field:SerializeField] public Color Background { get; set; } = new Color();
        [field:SerializeField] public Color Popup { get; set; } = new Color();

        
        [field:SerializeField] public Color LightBorderColor { get; set; } = new Color();
        [field:SerializeField] public Color DarkBorderColor { get; set; } = new Color();
        
        
        
        [field:SerializeField] public Color Button { get; set; } = new Color(0.82f, 0.82f, 0.82f, 1f);
        [field:SerializeField] public Color ButtonSelected { get; set; } = new Color(0.5f, 0.5f, 0.5f, 1f);

        
        private static VGuiStyleSO _style;
        public static VGuiStyleSO Style
        {
            get
            {
                if (_style == null)
                {
                    _style = Resources.Load<VGuiStyleSO>("Vida GUI Style");

                    if (_style == null)
                    {
                        _style = ScriptableObject.CreateInstance<VGuiStyleSO>();
                        
                        // if the folder does not exist, create it
                        if (!System.IO.Directory.Exists("Assets/Resources"))
                        {
                            UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                        }
                        
                        UnityEditor.AssetDatabase.CreateAsset(_style, "Assets/Resources/Vida GUI Style.asset");
                    }
                }

                return _style;
            }
        }
    }
}