#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vida.Framework.Editor
{
    [InitializeOnLoad]
    internal static class VidaFrameworkToolbarButton
    {
        private const string ToolbarTypeName = "UnityEditor.Toolbar";
        private const string RootFieldName = "m_Root";
        private const string RightToolbarZoneName = "ToolbarZoneRightAlign";
        private const string ButtonName = "vida-framework-toolbar-button";
        private const string ButtonText = "FRAMEWORK";

        private static readonly Color ButtonColor = new Color32(0x1B, 0x47, 0x6A, 0xFF);
        private static readonly Color ButtonHoverColor = new Color32(0x28, 0x74, 0xA6, 0xFF);
        private static readonly Color ButtonPressedColor = new Color32(0x12, 0x30, 0x48, 0xFF);
        private static readonly Color BorderColor = new Color32(0x4C, 0xC6, 0xFF, 0xFF);
        private static readonly Color TextColor = new Color32(0xF0, 0xF5, 0xFF, 0xFF);

        private static readonly Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType(ToolbarTypeName);
        private static readonly FieldInfo RootField = ToolbarType?.GetField(RootFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        static VidaFrameworkToolbarButton()
        {
            EditorApplication.update -= UpdateToolbarButton;
            EditorApplication.update += UpdateToolbarButton;
        }

        private static void UpdateToolbarButton()
        {
            if (ToolbarType == null || RootField == null)
            {
                EditorApplication.update -= UpdateToolbarButton;
                return;
            }

            UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            if (toolbars.Length == 0)
            {
                return;
            }

            if (TryAddButton(toolbars[0]))
            {
                EditorApplication.update -= UpdateToolbarButton;
            }
        }

        private static bool TryAddButton(UnityEngine.Object toolbar)
        {
            if (RootField.GetValue(toolbar) is not VisualElement root)
            {
                return false;
            }

            VisualElement rightToolbarZone = root.Q(RightToolbarZoneName);
            if (rightToolbarZone == null)
            {
                return false;
            }

            if (rightToolbarZone.Q<Button>(ButtonName) != null)
            {
                return true;
            }

            Button frameworkButton = CreateButton();
            rightToolbarZone.Add(frameworkButton);
            return true;
        }

        private static Button CreateButton()
        {
            Button frameworkButton = new Button(OpenFrameworkMenu)
            {
                name = ButtonName,
                tooltip = "Open Vida Framework Menu"
            };

            frameworkButton.ClearClassList();
            ApplyButtonStyle(frameworkButton, ButtonColor);
            AddButtonLabel(frameworkButton);
            RegisterButtonStateCallbacks(frameworkButton);

            return frameworkButton;
        }

        private static void AddButtonLabel(Button button)
        {
            Label label = new Label(ButtonText)
            {
                pickingMode = PickingMode.Ignore
            };

            label.style.flexGrow = 1f;
            label.style.color = TextColor;
            label.style.fontSize = 10f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;

            button.Add(label);
        }

        private static void RegisterButtonStateCallbacks(Button button)
        {
            button.RegisterCallback<MouseEnterEvent>(_ => ApplyButtonStyle(button, ButtonHoverColor));
            button.RegisterCallback<MouseLeaveEvent>(_ => ApplyButtonStyle(button, ButtonColor));
            button.RegisterCallback<MouseDownEvent>(_ => ApplyButtonStyle(button, ButtonPressedColor));
            button.RegisterCallback<MouseUpEvent>(_ => ApplyButtonStyle(button, ButtonHoverColor));
        }

        private static void ApplyButtonStyle(Button button, Color backgroundColor)
        {
            button.focusable = false;
            button.style.display = DisplayStyle.Flex;
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            button.style.width = 132f;
            button.style.height = 22f;
            button.style.marginLeft = 6f;
            button.style.marginRight = 6f;
            button.style.paddingLeft = 10f;
            button.style.paddingRight = 10f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            button.style.backgroundColor = backgroundColor;
            button.style.unityBackgroundImageTintColor = backgroundColor;
            button.style.borderTopColor = BorderColor;
            button.style.borderBottomColor = BorderColor;
            button.style.borderLeftColor = BorderColor;
            button.style.borderRightColor = BorderColor;
            button.style.borderTopWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderTopLeftRadius = 4f;
            button.style.borderTopRightRadius = 4f;
            button.style.borderBottomLeftRadius = 4f;
            button.style.borderBottomRightRadius = 4f;
            button.style.color = TextColor;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        private static void OpenFrameworkMenu()
        {
            VidaFramework.OpenWindow();
        }
    }
}
#endif
