using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Vida.Editor
{
    public class MainToolbar
    {
        private string[] _keys =  new string[]{ "Home", "Templates", "Settings" };
        
        public void Draw(Vector2 windowSize)
        {
            SirenixEditorGUI.BeginBox(GUILayout.Width(windowSize.x), GUILayout.Height(20));
            {
                SirenixEditorGUI.BeginHorizontalToolbar();
                {
                    for (int i = 0; i < _keys.Length; i++)
                    {
                        var key = _keys[i];
                        GUI.color = IsSelected(i) ? Color.white : Color.gray;
                        if (SirenixEditorGUI.ToolbarButton(new GUIContent(key),IsSelected(i)))
                        {
                            TemplatesWindow.ResetEditorPrefs();
                            SetSelected(i);
                        }
                        GUILayout.Space(10);
                    }
                    
                    GUILayout.FlexibleSpace();
                    GUI.color = Color.white;
                    
                    
                    if (SirenixEditorGUI.ToolbarButton(SdfIconType.XSquareFill,false))
                    {
                        TemplatesWindow.ResetEditorPrefs();
                        GithubConnector.ResetConnection();
                        ReloadNeeded = true;
                    }
                    if (SirenixEditorGUI.ToolbarButton(SdfIconType.ArrowRepeat,false))
                    {
                        EditorPrefs.DeleteKey("Collections");
                        TemplatesWindow.ResetEditorPrefs();
                        GithubConnector.ReadInfoFile();
                        ReloadNeeded = true;
                    }
                }
                SirenixEditorGUI.EndHorizontalToolbar();
            }
            SirenixEditorGUI.EndBox();
            
            GUI.color = Color.white;
        }
    
        public static bool ReloadNeeded
        {
            get => EditorPrefs.GetBool("MainToolbarNeedReload", false);
            set => EditorPrefs.SetBool("MainToolbarNeedReload", value);
        }
        
        
        public string GetSelected()
        {
            return _keys[EditorPrefs.GetInt("MainToolbarSelectedIndex", 0)];
        }
    
        private bool IsSelected(int index)
        {
            return EditorPrefs.GetInt("MainToolbarSelectedIndex", 0) == index;
        }
        private void SetSelected(int index)
        {
            EditorPrefs.SetInt("MainToolbarSelectedIndex", index);
        }
    }
}