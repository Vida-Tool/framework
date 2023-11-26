using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Vida.Editor
{
    public class TemplatesWindow
    {
        public static List<VidaAssetCollection> Collections { get; set; }


        public void Draw(Vector2 windowSize)
        {
            if (MainToolbar.ReloadNeeded)
            {
                Collections = GithubConnector.AssetCollections;
                MainToolbar.ReloadNeeded = false;
            }

            if (Collections == null)
            {
                GithubConnector.ReadInfoFile();
                Collections = GithubConnector.AssetCollections;
                return;
            }
            
            
            SirenixEditorGUI.BeginHorizontalToolbar();
            {
                if (Collections == null)
                {
                    GithubConnector.ReadInfoFile();
                    Collections = GithubConnector.AssetCollections;
                }

                if (Collections.Count > 0)
                {
                    // Get All Templates from _collections
                    var templates = Collections.SelectMany(x => x.Templates).Distinct().ToArray();
                    DrawTemplateLister(windowSize,templates,0);
                    GUILayout.FlexibleSpace();
                }
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        public Vector2[] sliderValue = new Vector2[10];
        
        private float[] _maxWidths = new float[] { 100,110,120,160};
        private void DrawTemplateLister(Vector2 windowSize,string[] items,int placement = 0,float totalWidth = 0,bool checkNext = true)
        {
            string mainTemplate = EditorPrefs.GetString($"Lister_{0}");
            string selectedTemplate = EditorPrefs.GetString($"Lister_{placement}");
            float maxWidth = placement > _maxWidths.Length - 1 ? _maxWidths[^1] : _maxWidths[placement];
            float boxWidth = windowSize.x * (maxWidth * 0.001f);
            if (boxWidth > maxWidth) boxWidth = maxWidth;
            
            
            SirenixEditorGUI.BeginBox(GUILayout.Width(boxWidth),GUILayout.Height(windowSize.y - 100));
            {
                sliderValue[placement] = GUILayout.BeginScrollView(sliderValue[placement], false, false, GUILayout.Height(windowSize.y - 100));
                {
                    GUILayout.BeginVertical(GUILayout.Height(items.Length * 32));
                    {
                        foreach (var item in items)
                        {
                            GUIContent content = new GUIContent(item);
                            content.tooltip = item;
                            GUIStyle customButtonStyle = new GUIStyle(GUI.skin.button);

                            bool isSelected = selectedTemplate == item;
      
                            
                            customButtonStyle.normal.background = isSelected?
                                TextureLoader.GetTexture("button-selected.png") :
                                TextureLoader.GetTexture("button.png");

                            customButtonStyle.fontSize = item.Length > 15 ? 11 : 12;
                            customButtonStyle.alignment = TextAnchor.MiddleCenter;
                            if (GUILayout.Button(content,customButtonStyle,GUILayout.Height(35)))
                            {
                                EditorPrefs.SetString($"Lister_{placement}", item);
                                ResetEditorPrefs(placement+1);
                            }

                            GUILayout.Space(5);
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
            SirenixEditorGUI.EndBox();

            if (!checkNext)
            {
                RenderItemInfo(windowSize,selectedTemplate,placement+1,(boxWidth + totalWidth));
                return;
            }

            var nextItems = Array.Empty<string>();

            // If placement is 0, then we are looking for the main template
            if (placement == 0)
            {
                nextItems = Collections
                    .Where(x => x.separatedMenu.Length > placement && x.Templates.Contains(mainTemplate))
                    .Select(x => x.separatedMenu[placement]).Distinct().ToArray();
            }
            // If placement is not 0, then we are looking for the selected template
            else
            {
                nextItems = Collections
                    .Where(x => x.separatedMenu.Length >= placement && x.Templates.Contains(mainTemplate) && x.separatedMenu.Contains(selectedTemplate))
                    .Select(x => (x.separatedMenu.Length>placement?x.separatedMenu[placement] : x.Name)).Distinct().ToArray();
               
            }
            
            
            if (nextItems.Length > 0)
            {
                DrawTemplateLister(windowSize,nextItems,placement + 1,(totalWidth+boxWidth));
            }
            else
            {
                nextItems = Collections
                    .Where(x => (x.separatedMenu[^1] == selectedTemplate))
                    .Select(x => x.Name).Distinct().ToArray();

                if (nextItems.Length > 0)
                {
                    DrawTemplateLister(windowSize,nextItems,placement + 1,(totalWidth + boxWidth),false);
                }
                else
                {
                    RenderItemInfo(windowSize,selectedTemplate,placement+1,(boxWidth + totalWidth));
                }
            }
        }
        
        
        private void RenderItemInfo(Vector2 windowSize,string itemName,int placement,float totalWidth)
        {
            var collection = Collections.FirstOrDefault(x => x.Name == itemName);
            if (collection == null) return;
            
            GUILayout.Space(25);


            float start = totalWidth;
            float windowWidth = windowSize.x;
            float boxWidth = (windowWidth - start);
            boxWidth = Mathf.Clamp(boxWidth, 160, 400);
            
            
            SirenixEditorGUI.BeginBox(GUILayout.Width(boxWidth),GUILayout.Height(400));
            {
                sliderValue[placement] = GUILayout.BeginScrollView(sliderValue[placement],false,false, GUILayout.Width(boxWidth), GUILayout.Height(400));
                {
                    GUILayout.Space(10);
                    VidaEditorGUI.Title(collection.Name,true);
                    GUILayout.Label("You can download this template by clicking the button below.");
                    GUILayout.Space(20);
                    VidaEditorGUI.Title("Information",true,TextAnchor.MiddleLeft);
                    GUILayout.Label(collection.Info);
                    GUILayout.Space(40);
                    // set flexible
                    GUILayout.FlexibleSpace();
                    
                    VidaEditorGUI.Title("Actions",true,TextAnchor.MiddleLeft);
                    GUILayout.Space(5);
                    if (GUILayout.Button("Download",GUILayout.Height(30),GUILayout.Width(100)))
                    {
                        Download(itemName);
                    }
                    
                    GUILayout.Space(20);
                }
                GUILayout.EndScrollView();
                GUI.color = Color.white;
                
            }
            SirenixEditorGUI.EndBox();
        }

        private void Download(string itemName)
        {
            GithubConnector.DownloadItem(itemName);
        }
        
        
        private void ResetEditorPrefs(int start)
        {
            for (int i = start; i < 10; i++)
            {
                EditorPrefs.SetString($"Lister_{i}", "");
            }
        }
    }
    
}