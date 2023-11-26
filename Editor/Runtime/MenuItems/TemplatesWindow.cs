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
        private static List<VidaAssetCollection> _collections;

        
        public void Draw(Vector2 windowSize)
        {
            if (MainToolbar.ReloadNeeded)
            {
                _collections = GithubConnector.AssetCollections;
                MainToolbar.ReloadNeeded = false;
            }

            if (_collections == null)
            {
                GithubConnector.ReadInfoFile();
                _collections = GithubConnector.AssetCollections;
                return;
            }
            
            
            SirenixEditorGUI.BeginHorizontalToolbar();
            {
                if (_collections == null)
                {
                    GithubConnector.ReadInfoFile();
                    _collections = GithubConnector.AssetCollections;
                }
                // Get All Templates from _collections
                var templates = _collections.SelectMany(x => x.Templates).Distinct().ToArray();
                
                DrawTemplateLister(windowSize,templates,0);
                
                GUILayout.FlexibleSpace();
                
    
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        public Vector2[] sliderValue = new Vector2[10];
        
        private void DrawTemplateLister(Vector2 windowSize,string[] items,int placement = 0,bool checkNext = true)
        {
            
            string mainTemplate = EditorPrefs.GetString($"Lister_{0}");
            string selectedTemplate = EditorPrefs.GetString($"Lister_{placement}");
            
            float boxWidth = windowSize.x * 0.15f;
            if (boxWidth > 150) boxWidth = 150;
            SirenixEditorGUI.BeginBox(GUILayout.Width(boxWidth),GUILayout.Height(windowSize.y - 100));
            {

                sliderValue[placement] = GUILayout.BeginScrollView(sliderValue[placement], false, false, GUILayout.Width(boxWidth), GUILayout.Height(windowSize.y - 100));
                {
                    GUILayout.BeginVertical(GUILayout.Height(items.Length * 37));
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

                            customButtonStyle.fontSize = item.Length > 15 ? 10 : 12;
                            customButtonStyle.contentOffset = new Vector2(5, 0);
                            customButtonStyle.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button(content,customButtonStyle,GUILayout.Height(35),GUILayout.Width(boxWidth)))
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
                RenderItemInfo(windowSize,selectedTemplate,placement+1);
                return;
            }

            var nextItems = Array.Empty<string>();

            // If placement is 0, then we are looking for the main template
            if (placement == 0)
            {
                nextItems = _collections
                    .Where(x => x.separatedMenu.Length > placement && x.Templates.Contains(mainTemplate))
                    .Select(x => x.separatedMenu[placement]).Distinct().ToArray();
            }
            // If placement is not 0, then we are looking for the selected template
            else
            {
                nextItems = _collections
                    .Where(x => x.separatedMenu.Length >= placement && x.Templates.Contains(mainTemplate) && x.separatedMenu.Contains(selectedTemplate))
                    .Select(x => (x.separatedMenu.Length>placement?x.separatedMenu[placement] : x.Name)).Distinct().ToArray();
               
            }
            
            
            if (nextItems.Length > 0)
            {
                DrawTemplateLister(windowSize,nextItems,placement + 1);
            }
            else
            {
                nextItems = _collections
                    .Where(x => (x.separatedMenu[^1] == selectedTemplate))
                    .Select(x => x.Name).Distinct().ToArray();

                if (nextItems.Length > 0)
                {
                    DrawTemplateLister(windowSize,nextItems,placement + 1,false);
                }
                else
                {
                    RenderItemInfo(windowSize,selectedTemplate,placement+1);
                }
            }
        }
        
        
        private void RenderItemInfo(Vector2 windowSize,string itemName,int placement)
        {
            var collection = _collections.FirstOrDefault(x => x.Name == itemName);
            if (collection == null) return;
            
            GUILayout.Space(25);
            
            float boxWidth = windowSize.x * 0.4f;
            // get current x
            float currentX = windowSize.x * 0.15f;
            if(currentX > 150) currentX = 150;
            currentX *= placement + 1;
            if(boxWidth + currentX > windowSize.x) boxWidth = windowSize.x - currentX;
            
            SirenixEditorGUI.BeginBox(GUILayout.Width(boxWidth),GUILayout.Height(400));
            {
                sliderValue[placement] = GUILayout.BeginScrollView(sliderValue[placement], false, false, GUILayout.Width(boxWidth), GUILayout.Height(400));
                {
                    GUILayout.Label(collection.Name);
                    GUILayout.Space(10);
                    GUILayout.Label(collection.Info);
                    GUILayout.Space(25);
                    GUILayout.Label("Download Location:" + collection.DownloadLocation);
                    GUILayout.Space(25);
                    if (GUILayout.Button("Download"))
                    {
                        Download(itemName);
                    }
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
        
        private void RenderAllCollections()
        {
            foreach (var collection in _collections)
            {
                GUILayout.Label(collection.Name + " - " + collection.Location + " - " + collection.Menu + " - " + collection.Info);
            }
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