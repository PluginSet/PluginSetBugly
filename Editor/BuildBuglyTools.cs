using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace PluginSet.Bugly.Editor
{
    [BuildTools]
    public static class BuildBuglyTools
    {
        [OnSyncEditorSetting]
        public static void OnSyncEditorSetting(BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>("Bugly");
            if (!buildParams.Enable)
                return;

            context.Symbols.Add("ENABLE_BUGLY");
            context.AddLinkAssembly("PluginSet.Bugly");

            Global.CopyDependenciesInLib("com.pluginset.bugly");

            var pluginConfig = context.Get<PluginSetConfig>("pluginsConfig");
            var config = pluginConfig.Get<PluginBuglyConfig>("Bugly");
            config.AppId = buildParams.AppId;
            config.AutoLogSeverity = buildParams.AutoLogSeverity;
            config.AutoQuitApplication = buildParams.AutoQuitApplication;
            config.ReportDelay = buildParams.ReportDelay;
        }

        [iOSXCodeProjectModify]
        public static void OniOSXCodeProjectModify(BuildProcessorContext context, PBXProjectManager projectManager)
        {
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>("Bugly");
            if (!buildParams.Enable)
                return;

            var project = projectManager.Project;
            project.SetBuildProperty(projectManager.MainFramework, "GCC_GENERATE_DEBUGGING_SYMBOLS", "YES");
            project.SetBuildProperty(projectManager.UnityFramework, "GCC_GENERATE_DEBUGGING_SYMBOLS", "YES");
        }

        [AndroidProjectModify]
        public static void OnAndroidProjectModify(BuildProcessorContext context, AndroidProjectManager projectManager)
        {
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>("Bugly");
            if (!buildParams.Enable)
                return;

            var node = projectManager.ProjectGradle.ROOT.GetOrCreateNode("allprojects/buildscript/dependencies");
            node.AppendContentNode("classpath 'com.tencent.bugly:symtabfileuploader:latest.release'");
            
            Debug.Log(projectManager.LibraryGradle.Print());
            var root = projectManager.LibraryGradle.ROOT;
            const string applyBugly = "apply plugin: 'bugly'";
            root.RemoveContentNode(applyBugly);
            root.InsertChildNode(new GradleContentNode(applyBugly, root), 1);

            var bugly = root.GetOrCreateNode("bugly");
            bugly.AppendContentNode($"appId = \"{buildParams.AppId}\"");
            bugly.AppendContentNode($"appKey = \"{buildParams.AppKey}\"");
            bugly.AppendContentNode($"appVersion = \"{PluginUtil.GetVersionString(PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode)}\"");
        }
        
        [ExportTaskCompleted]
        public static void OnExportProjectCompleted(BuildProcessorContext context)
        {
            if (!context.ExportProject)
                return;
            
            CollectAndUploadSymbols(context);
        }

        [MenuItem("PluginSet/Collect Bugly Symbols")]
        public static void CollectBuglySymbols()
        {
            CollectAndUploadSymbols(BuildProcessorContext.Default());
        }
        
        private static void CollectAndUploadSymbols(BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>("Bugly");
            if (!buildParams.Enable)
                return;
            
            if (!buildParams.UploadSymbols)
                return;

#if !UNITY_2020_1_OR_NEWER
            var isWindows = Application.platform == RuntimePlatform.WindowsEditor;
            var sevenZip = Path.Combine(EditorApplication.applicationContentsPath, "Tools", isWindows ? "7z.exe" : "7za");
            
            string symbolZip = Path.GetFileNameWithoutExtension(context.ProjectPath) + "-" + PlayerSettings.bundleVersion + "-v" + (object) PlayerSettings.Android.bundleVersionCode + ".symbols.zip";
            string path = Path.GetFullPath(Path.Combine(context.ProjectPath, "..", symbolZip));
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Cannot find symbol.zip at path : {path}");
                return;
            }
            string symbolPath = Path.GetFullPath(Path.Combine(context.ProjectPath, "symbols"));
            Global.ExecuteCommand(sevenZip, "x", "-tzip", "-y", path, $"-o{symbolPath}");
            
            File.Delete(path);
#else
            string symbolPath = Path.GetFullPath(Path.Combine(context.ProjectPath, "unityLibrary", "symbols"));
#endif
#if UNITY_ANDROID
            CopyAndroidSymbols(symbolPath, context.ProjectPath);
#endif
            
            if (!Directory.Exists(symbolPath))
            {
                Debug.LogWarning($"Cannot find symbols file at path : {symbolPath}");
                return;
            }
            
            var pluginPath = Global.GetPackageFullPath("com.pluginset.bugly");
            var toolJar = Path.GetFullPath(Path.Combine(pluginPath, "Editor", "Tools~", "buglyqq-upload-symbol.jar"));
            if (!File.Exists(toolJar))
            {
                Debug.LogWarning($"Cannot find bugly tools at path: {toolJar}");
                return;
            }
            
//            if (!context.ProductMode)
//                return;
//            
            Global.ExecuteCommand("java"
                , "-jar", toolJar
                , "-appid", buildParams.AppId
                , "-appkey", buildParams.AppKey
                , "-inputSymbol", symbolPath
                , "-version", PluginUtil.GetVersionString(PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode)
#if UNITY_IOS
                , "-bundleid", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS)
                , "-platform", "IOS"
#else
                , "-bundleid", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)
                , "-platform", "Android"
#endif
            );
            
            Global.CopyFiles(symbolPath, Path.Combine(context.ProjectPath, "symbols-backup"));
//            Global.CheckAndDeletePath(symbolPath);
        }

        private static void CopyAndroidSymbols(string targetPath, string projectPath)
        {
            foreach (var arm in new string[]{"arm64-v8a", "armeabi-v7a"})
            {
                var symbolsPath = Path.Combine(targetPath, arm);
                Global.CopyFilesTo(symbolsPath, Path.Combine(Application.dataPath, "..", "Temp", "StagingArea", "symbols", arm), "*.sym.so");
                Global.CopyFilesTo(symbolsPath, Path.Combine(projectPath, "unityLibrary", "build", $"il2cpp_{arm}_Release", "il2cpp_cache"), "*.sym.so");
                Global.CopyFilesTo(symbolsPath, Path.Combine(projectPath, "unityLibrary", "build", $"il2cpp_{arm}_Release", "il2cpp_cache"), "*.dbg.so");
            }
        }
        
        private static void CopyAppleAppSymbolsInApp(string targetPath, string appPath)
        {
            // TODO
            Global.CopyFilesTo(targetPath, Path.Combine(appPath, "dSYMs"), "*.dSYM");
        }
    }
}
