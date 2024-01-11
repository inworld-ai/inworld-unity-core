/*************************************************************************************************
 * Copyright 2022 Theai, Inc. (DBA Inworld)
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Inworld
{
    [InitializeOnLoad]
    public class Preload : IPreprocessBuildWithReport
    {
        static Preload()
        {
            AssetDatabase.importPackageCompleted += async packageName =>
            {
                if (InworldAI.Initialized)
                    return;
                _AddDebugMacro();
                await DependencyImporter.InstallDependencies();
                VersionChecker.CheckVersionUpdates();
                if (VersionChecker.IsLegacyPackage)
                    VersionChecker.NoticeLegacyPackage();
                InworldAI.Initialized = true;
                if (System.IO.File.Exists("Assets/Inworld/InworldExtraAssets.unitypackage"))
                    AssetDatabase.ImportPackage("Assets/Inworld/InworldExtraAssets.unitypackage", false);
                _SetDefaultUserName();
#if UNITY_EDITOR && VSP
                if (!string.IsNullOrEmpty(InworldAI.User.Account))
                    VSAttribution.SendAttributionEvent("Login Studio", InworldAI.k_CompanyName, InworldAI.User.Account);     
#endif
            };
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.EnteredEditMode:
                    if (InworldAI.IsDebugMode)
                        _AddDebugMacro();
                    else
                        _RemoveDebugMacro();
                    break;
            }
        }
        /// <summary>
        /// Remove all the Inworld logs. those log will not be printed out in the runtime.
        /// Needs to be public to be called outside Unity.
        /// </summary>
        /// <param name="report"></param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (Debug.isDebugBuild || InworldAI.IsDebugMode)
                return;
            _RemoveDebugMacro();
        }
        const string k_NativeJSPath = "Inworld/Editor/Native";
        
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.WebGL) 
                return;
			
            string indexPath = $"{pathToBuiltProject}/index.html";

            if (!File.Exists(indexPath))
            {
                Debug.LogError("Cannot load index file.");
                return;
            }
            string indexData = File.ReadAllText(indexPath);

            string dependencies = "<script src='./InworldMicrophone.js'></script>";

            if (!indexData.Contains(dependencies))
            {
                indexData = indexData.Insert(indexData.IndexOf("</head>"), $"\n{dependencies}\n");
                File.WriteAllText(indexPath, indexData);
            }
            File.Copy($"{Application.dataPath}/{k_NativeJSPath}/InworldMicrophone.txt", $"{pathToBuiltProject}/InworldMicrophone.js", true);
            File.Copy($"{Application.dataPath}/{k_NativeJSPath}/AudioResampler.txt", $"{pathToBuiltProject}/AudioResampler.js", true);
        }
        static void _SetDefaultUserName()
        {
            string userName = CloudProjectSettings.userName;
            InworldAI.User.Name = !string.IsNullOrEmpty(userName) && userName.Split('@').Length > 1 ? userName.Split('@')[0] : userName;
        }
        static void _AddDebugMacro()
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string strSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            if (!strSymbols.Contains("INWORLD_DEBUG"))
                strSymbols = string.IsNullOrEmpty(strSymbols) ? "INWORLD_DEBUG" : strSymbols + ";INWORLD_DEBUG";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, strSymbols);
        }
        static void _RemoveDebugMacro()
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string strSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            strSymbols = strSymbols.Replace(";INWORLD_DEBUG", "").Replace("INWORLD_DEBUG", "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, strSymbols);
        }
        public int callbackOrder { get; }
    }
}
#endif