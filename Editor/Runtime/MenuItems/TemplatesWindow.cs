﻿using System;
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
        private static List<VidaAssetCollection> Collections => GithubConnector.AssetCollections;
        private static string[] SelectedTemplates = new string[15]; 

        public void Draw(Vector2 windowSize)
        {
            if (Collections == null)
            {
                if (Collections == null || Collections.Count <= 0)
                {
                    GithubConnector.ReadInfoFile(false);
                    return;
                }
            }

            if (Collections.Count > 0)
            {
                SirenixEditorGUI.BeginHorizontalToolbar();
                {
                    // Get All Templates from _collections
                    var templates = Collections.SelectMany(x => x.Templates).Distinct().ToArray();
                    DrawTemplateLister(windowSize,templates,0);
                    GUILayout.FlexibleSpace();
                }
                SirenixEditorGUI.EndHorizontalToolbar();
            }

        }

        public Vector2[] sliderValue = new Vector2[10];
        
        private float[] _maxWidths = new float[] { 100,110,160,160};
        private void DrawTemplateLister(Vector2 windowSize,string[] items,int placement = 0,float totalWidth = 0,bool checkNext = true)
        {
            string mainTemplate = SelectedTemplates[0];
            string selectedTemplate = SelectedTemplates[placement];
            float boxWidth = placement > _maxWidths.Length - 1 ? _maxWidths[^1] : _maxWidths[placement];
            
            
            SirenixEditorGUI.BeginBox(GUILayout.Width(boxWidth),GUILayout.Height(windowSize.y - 100));
            {
                sliderValue[placement] = GUILayout.BeginScrollView(sliderValue[placement], false, false, GUILayout.Width(boxWidth),GUILayout.Height(windowSize.y - 100));
                {
                    GUILayout.BeginVertical(GUILayout.Height(items.Length * 25),GUILayout.Width(boxWidth));
                    {
                        foreach (var item in items)
                        {
                            GUIContent content = new GUIContent(item);
                            content.tooltip = item;
                            GUIStyle customButtonStyle = new GUIStyle(GUI.skin.button);

                            bool isSelected = selectedTemplate == item;
                            GUI.backgroundColor = isSelected ? VidaGUIStyles.MatteGray: VidaGUIStyles.LightGray;
                            

                            customButtonStyle.fontSize = item.Length > 15 ? 11 : 12;
                            customButtonStyle.alignment = TextAnchor.MiddleCenter;
                            // SET BUTTON ALIGNMENT TO CENTER
                            if (GUILayout.Button(content,customButtonStyle,GUILayout.Height(25),GUILayout.Width(boxWidth-10)))
                            {
                                SelectedTemplates[placement] = item;
                                //EditorPrefs.SetString($"Lister_{placement}", item);
                                ResetEditorPrefs(placement+1);
                            }

                            GUI.backgroundColor = VidaGUIStyles.DefaultColor;
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
                    GUI.backgroundColor = VidaGUIStyles.SoftGreen;
                    VidaEditorGUI.Title(collection.Name,true);
                    GUI.backgroundColor = VidaGUIStyles.DefaultColor;
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
                
            }
            SirenixEditorGUI.EndBox();
        }

        private void Download(string itemName)
        {
            GithubConnector.DownloadItem(itemName);
        }
        
        
        public static void ResetEditorPrefs(int start = 0)
        {
            for (int i = start; i < 10; i++)
            {
                SelectedTemplates[i] = "";
                //EditorPrefs.SetString($"Lister_{i}", "");
            }
        }
    }
    
}