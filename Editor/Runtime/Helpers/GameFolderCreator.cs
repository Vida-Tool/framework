using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public static class GameFolderCreator
    {
        public static void Create()
        {
            // Ana klasör
            string anaKlasorYolu = "Assets/Vida/!Game";

            // Alt klasörler
            string[] altKlasorler = new string[]
            {
                "Prefabs", "Scripts", "Materials", "Textures", "Animations", "Audio", "Fonts", "Models", "Scenes",
                "Shaders", "Sprites"
            };

            // Ana klasörü oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Vida"))
            {
                AssetDatabase.CreateFolder("Assets", "Vida");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Vida/!Game"))
            {
                AssetDatabase.CreateFolder("Assets/Vida", "!Game");
            }

            // Alt klasörleri oluştur
            foreach (string altKlasorAdi in altKlasorler)
            {
                string altKlasorYolu = Path.Combine(anaKlasorYolu, altKlasorAdi);
                if (!AssetDatabase.IsValidFolder(altKlasorYolu))
                {
                    AssetDatabase.CreateFolder(anaKlasorYolu, altKlasorAdi);
                }
            }

            // İşlem tamamlandı mesajı
            Debug.Log("Klasör yapısı oluşturuldu.");
        }
    }
}