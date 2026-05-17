using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class TextureLoader
    {
        
        public static Texture2D GetTexture(string path)
        {
            string normalizedPath = path.TrimStart('/');
            string normalPath = "Assets/framework/Editor/Resources/Icons/" + normalizedPath;
            string packagePath = "Packages/com.vida.framework/Editor/Resources/Icons/" + normalizedPath;

            var item = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            if (!item)
            {
                item = AssetDatabase.LoadAssetAtPath<Texture2D>(packagePath);
            }
            
 
            
            

            return item;
        }
    }
}
