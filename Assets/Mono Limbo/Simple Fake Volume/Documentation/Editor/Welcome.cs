using UnityEditor;
using UnityEngine;
using System.IO;

namespace MonoLimbo
{
    [InitializeOnLoad]
    public static class SimpleFakeVolumeFog_WelcomeLauncher
    {
        private const string DontShowKey = "SFVF_Welcome_DontShow";

        static SimpleFakeVolumeFog_WelcomeLauncher()
        {
            if (!EditorPrefs.HasKey(DontShowKey))
                EditorApplication.update += OpenWindowOnce;
        }

        private static void OpenWindowOnce()
        {
            EditorApplication.update -= OpenWindowOnce;
            SimpleFakeVolumeFog_WelcomeWindow.ShowWindow();
        }
    }

    public class SimpleFakeVolumeFog_WelcomeWindow : EditorWindow
    {
        private Texture2D banner;
        private bool dontShowAgain;

        private const string publisherUrl = "https://assetstore.unity.com/packages/vfx/shaders/simple-fake-volume-fog-299560"; // Replace with your page
        private const string documentationFolderName = "Mono Limbo/Simple Fake Volume Fog/Documentation";

        public static void ShowWindow()
        {
            var window = GetWindow<SimpleFakeVolumeFog_WelcomeWindow>("Fake Volume Fog");
            window.minSize = new Vector2(450, 520);
        }

        private void OnEnable()
        {
            banner = Resources.Load<Texture2D>("SFVF_Banner");
            // Banner path: Assets/Resources/SFVF_Banner.png
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // Banner
            if (banner != null)
                GUILayout.Label(banner, GUILayout.Height(140));
            else
                GUILayout.Label("Simple Fake Volume Fog", EditorStyles.boldLabel);

            GUILayout.Space(10);

            // Overview
            EditorGUILayout.LabelField("Welcome to Simple Fake Volume Fog!", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A lightweight, UV-free fog shader using world-space and triplanar projection.\n" +
                "Perfect for mobile, VR, stylized games, prototypes, and meshes without UVs.",
                MessageType.Info
            );

            GUILayout.Space(12);

            // Features
            EditorGUILayout.LabelField("Key Features", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "- UV-free world-space fog projection\n" +
                "- Smooth triplanar blending\n" +
                "- Alpha-Only fog overlay mode\n" +
                "- Optimized for mobile & VR\n" +
                "- Works instantly with any mesh",
                MessageType.None
            );

            GUILayout.Space(12);

            // Quick Start
            EditorGUILayout.LabelField("Quick Start", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Create a material using the 'Simple Fake Volume Fog' shader.\n" +
                "2. Adjust Fog Color, Fog Density, and World Scale.\n" +
                "3. Apply to any mesh — no UVs required!",
                MessageType.Info
            );

            GUILayout.Space(12);

            // Documentation
            EditorGUILayout.LabelField("Documentation", EditorStyles.boldLabel);
            if (GUILayout.Button("Open Documentation Folder"))
                OpenDocumentation();

            GUILayout.Space(12);

            // Support
            EditorGUILayout.LabelField("Support", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "For questions, demos, or bug reports, include your Unity version and reproduction steps.\n" +
                "More tools and updates available on our publisher page.",
                MessageType.None
            );

            if (GUILayout.Button("Visit Publisher Page"))
                Application.OpenURL(publisherUrl);

            GUILayout.FlexibleSpace();

            GUILayout.Space(10);

            // Do Not Show Again
            bool newToggle = EditorGUILayout.Toggle("Do not show again", dontShowAgain);
            if (newToggle != dontShowAgain)
            {
                dontShowAgain = newToggle;
                if (dontShowAgain)
                    EditorPrefs.SetInt("SFVF_Welcome_DontShow", 1);
                else
                    EditorPrefs.DeleteKey("SFVF_Welcome_DontShow");
            }
        }

        private void OpenDocumentation()
        {
            string[] allFolders = Directory.GetDirectories(Application.dataPath, "*", SearchOption.AllDirectories);

            foreach (string folder in allFolders)
            {
                if (folder.Replace("\\", "/").EndsWith(documentationFolderName))
                {
                    EditorUtility.RevealInFinder(folder);
                    return;
                }
            }

            EditorUtility.DisplayDialog(
                "Documentation Not Found",
                "Could not locate the documentation folder.\n\nExpected: Assets/" + documentationFolderName,
                "OK"
            );
        }
    }
}