using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class TemplatesWindow
    {
        private VGuiStyleSO Style => VGuiStyleSO.Style;

        private static List<VidaAssetCollection> Collections => GithubConnector.AssetCollections;
        private static string[] SelectedTemplates = new string[15];
        
        public void Draw(Vector2 windowSize)
        {
            if (Collections == null || Collections.Count <= 0)
            {
                _ = GithubConnector.ReadAssetCollectionsAsync(false); // Asenkron çağrı
                return;
            }

            if (Collections.Count > 0)
            {
                GUILayout.BeginHorizontal();
                {
                    // Get All Templates from _collections
                    var templates = Collections.SelectMany(x => x.Templates).Distinct().ToArray();
                    
                    bool isSearching = DrawForSearchBar(windowSize);
                    if (!isSearching)
                    {
                        DrawTemplateLister(windowSize,templates,0);
                    }
                    
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }

        }

        private bool DrawForSearchBar(Vector2 windowSize)
        {
            var str = MainToolbar.search;
            if(str.Length < 3) return false;
            
            var items = Collections.Where(x => x.Name.ToLower().Contains(str.ToLower()) || str.ToLower().Contains(x.Name.ToLower())).ToArray();

            float boxWidth = _maxWidths[0];
            string selectedTemplate = SelectedTemplates[0];

            GUIStyle background1 = VCustomGUI.GetBoxStyle(Style.Background);
            GUILayout.Space(10);
            GUILayout.BeginVertical(background1);
            {
                GUILayout.Space(10);
                sliderValue[0] = GUILayout.BeginScrollView(sliderValue[0], false, false, GUILayout.Width(boxWidth), GUILayout.Height(windowSize.y - 100));
                {
                    GUILayout.BeginVertical(GUILayout.Height(items.Length * 25),GUILayout.Width(boxWidth));
                    {
                        foreach (var item in items)
                        {
                            GUIContent content = new GUIContent(item.Name);
                            content.tooltip = item.Name;
                            GUIStyle customButtonStyle = new GUIStyle(GUI.skin.button);

                            bool isSelected = selectedTemplate == item.Name;
                            GUI.backgroundColor = isSelected ? Style.ButtonSelected: Style.Button;
                            

                            customButtonStyle.fontSize = item.Name.Length > 15 ? 11 : 12;
                            customButtonStyle.alignment = TextAnchor.MiddleCenter;
                            // SET BUTTON ALIGNMENT TO CENTER
                            if (GUILayout.Button(content,customButtonStyle,GUILayout.Height(25),GUILayout.Width(boxWidth-10)))
                            {
                                SelectedTemplates[0] = item.Name;
                                //EditorPrefs.SetString($"Lister_{placement}", item);
                                ResetEditorPrefs(1);
                            }

                            GUI.backgroundColor = VGUIStyle.DefaultColor;
                            GUILayout.Space(5);
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            
            RenderItemInfo(windowSize,selectedTemplate,1,boxWidth);
            return true;
        }


        
        public Vector2[] sliderValue = new Vector2[10];
        
        private float[] _maxWidths = new float[] { 100,110,160,160};
        private void DrawTemplateLister(Vector2 windowSize,string[] items,int placement = 0,float totalWidth = 0,bool checkNext = true)
        {
            string mainTemplate = SelectedTemplates[0];
            string selectedTemplate = SelectedTemplates[placement];
            float boxWidth = placement > _maxWidths.Length - 1 ? _maxWidths[^1] : _maxWidths[placement];


            Rect currentRect = GUILayoutUtility.GetRect(0,0);
            GUI.Box(new Rect(currentRect.x,currentRect.y,boxWidth,windowSize.y-100),"",VGUIStyle.GetBoxStyle(VGUIStyle.BackgroundSoft));
            
            
            sliderValue[placement] = GUILayout.BeginScrollView(sliderValue[placement], false, false, GUILayout.Width(boxWidth),GUILayout.Height(windowSize.y - 100));
            {
                GUILayout.BeginVertical(GUILayout.Height(items.Length * 25), GUILayout.Width(boxWidth));
                {
                    GUILayout.Space(10);

                    foreach (var item in items)
                    {
                        GUIContent content = new GUIContent(item);
                        content.tooltip = item;
                        GUIStyle customButtonStyle = new GUIStyle(GUI.skin.button);

                        bool isSelected = selectedTemplate == item;
                        GUI.backgroundColor = isSelected ? Style.ButtonSelected : Style.Button;


                        customButtonStyle.fontSize = item.Length > 15 ? 11 : 12;
                        customButtonStyle.alignment = TextAnchor.MiddleCenter;
                        // SET BUTTON ALIGNMENT TO CENTER
                        if (GUILayout.Button(content, customButtonStyle, GUILayout.Height(25),
                                GUILayout.Width(boxWidth - 10)))
                        {
                            SelectedTemplates[placement] = item;
                            //EditorPrefs.SetString($"Lister_{placement}", item);
                            ResetEditorPrefs(placement + 1);
                        }

                        GUI.backgroundColor = VGUIStyle.DefaultColor;
                        GUILayout.Space(5);
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10);


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
                    .Where(x => x.SeparatedMenu.Length > placement && x.Templates.Contains(mainTemplate))
                    .Select(x => x.SeparatedMenu[placement]).Distinct().ToArray();
            }
            // If placement is not 0, then we are looking for the selected template
            else
            {
                nextItems = Collections
                    .Where(x => x.SeparatedMenu.Length >= placement && x.Templates.Contains(mainTemplate) && x.SeparatedMenu.Contains(selectedTemplate))
                    .Select(x => (x.SeparatedMenu.Length>placement?x.SeparatedMenu[placement] : x.Name)).Distinct().ToArray();
               
            }
            
            
            if (nextItems.Length > 0)
            {
                DrawTemplateLister(windowSize,nextItems,placement + 1,(totalWidth+boxWidth));
            }
            else
            {
                nextItems = Collections
                    .Where(x => (x.SeparatedMenu[^1] == selectedTemplate))
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
            
            
            Rect currentRect = GUILayoutUtility.GetRect(0,0);
            
            GUIStyle background1 = VCustomGUI.GetDefaultBoxStyle(Style.Background);
            GUI.Box(new Rect(currentRect.x,currentRect.y,boxWidth,400),String.Empty,background1);
            
            
            sliderValue[placement] = GUILayout.BeginScrollView(sliderValue[placement],false,false, GUILayout.Width(boxWidth), GUILayout.Height(400));
            {

                GUILayout.Space(15);
                collection.Name.VTitle(true,Style.TextHeader,fontSize:18);
                
                
                GUILayout.Space(20);
                "Description".VTitle(true,Style.TextHeader,TextAnchor.MiddleLeft,14);
                
                
                var labels = collection.Info.Split("/n");
                foreach (var label in labels)
                {
                    label.VLabel(Style.TextOther);
                }
                
                GUILayout.Space(40);
                GUILayout.FlexibleSpace();
                    
                "Actions".VTitle(true,Style.TextHeader,TextAnchor.MiddleLeft,14);
                //VidaEditorGUI.Title("Actions",true,TextAnchor.MiddleLeft);
                GUILayout.Space(5);
                
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Download",GUILayout.Height(30),GUILayout.Width(100)))
                    {
                        Download(itemName);
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

                    
                GUILayout.Space(20);
            }
            GUILayout.EndScrollView();
            
            
        }

        private void Download(string itemName)
        {
            _=GithubConnector.DownloadItemAsync(itemName);
        }
        
        
        public static void ResetEditorPrefs(int start = 0)
        {
            for (int i = start; i < 10; i++)
            {
                SelectedTemplates[i] = "";
            }
        }
        

    }
    
}