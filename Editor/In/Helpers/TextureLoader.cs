﻿using UnityEditor;
using UnityEngine;

namespace VidaFramework.Editor
{
    public class TextureLoader
    {
        public static Texture2D GetTexture(string path)
        {
            string normalPath = "Assets/vida-framework/Editor/Resources/Icons/" + path;
            string packagePath = "Packages/vida-framework/Editor/Resources/Icons/" + path;

            var item = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            if (!item)
            {
                item = AssetDatabase.LoadAssetAtPath<Texture2D>(packagePath);
            }

            return item;
        }
    }
}