using UnityEditor;
using UnityEngine;
using Vida.Framework.CodeEditor;

namespace Vida.Framework.Editor
{
    public class MainToolbar
    {
        public static string search = "";
        private string[] _keys =  new string[]{ "Home", "Templates","Codes", "Settings" };
        
        public void Draw(Vector2 windowSize)
        {
            GUILayout.BeginHorizontal();
            
            Rect currentRect = GUILayoutUtility.GetRect(0,0);
            GUI.Box(new Rect(currentRect.x,currentRect.y,windowSize.x+10,25),"",VGUIStyle.GetBoxStyle(VGUIStyle.BackgroundSoft));
            
            float width = windowSize.x * 0.3f;
            int selected = GUILayout.Toolbar(GetSelectedIndex(), _keys,GUILayout.Width(width));
            if (selected != GetSelectedIndex())
            {
                TemplatesWindow.ResetEditorPrefs();
                SetSelected(selected);
            }

            if (GetSelectedIndex() == 1)
            {
                GUILayout.Space(10);
                search = EditorGUILayout.TextField(search, GUILayout.Width(windowSize.x*0.2f));
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset"))
            {
                TemplatesWindow.ResetEditorPrefs();
                GithubConnector.ResetConnection();
                ReloadNeeded = true;
            }
            if(GUILayout.Button("Reload"))
            {
                EditorPrefs.DeleteKey("Collections");
                TemplatesWindow.ResetEditorPrefs();
                GithubConnector.ReadInfoFile();
                DataReader.LoadData();
                ReloadNeeded = true;
            }
            if (GUILayout.Button("Login"))
            {
                VidaFramework.Connection = false;
                VidaFramework.AutoConnect = false;
                VGitLogin.ShowWindow(null);
            }
                
            
            GUILayout.EndHorizontal();

            
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
        public int GetSelectedIndex()
        {
            return EditorPrefs.GetInt("MainToolbarSelectedIndex", 0);
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