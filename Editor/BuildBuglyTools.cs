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
            if (context.BuildTarget != BuildTarget.Android && context.BuildTarget != BuildTarget.iOS)
                return;
                
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>();
            if (!buildParams.Enable)
                return;

            context.Symbols.Add("ENABLE_BUGLY");
            context.AddLinkAssembly("PluginSet.Bugly");

            Global.CopyDependenciesInLib("com.pluginset.bugly");

            var pluginConfig = context.Get<PluginSetConfig>("pluginsConfig");
            var config = pluginConfig.AddConfig<PluginBuglyConfig>("Bugly");
            config.AppId = buildParams.AppId;
            config.AutoLogSeverity = buildParams.AutoLogSeverity;
            config.AutoQuitApplication = buildParams.AutoQuitApplication;
            config.ReportDelay = buildParams.ReportDelay;
        }

        [iOSXCodeProjectModify]
        public static void OniOSXCodeProjectModify(BuildProcessorContext context, PBXProjectManager project)
        {
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>();
            if (!buildParams.Enable)
                return;

            var mainFramework = project.MainFramework;
            var unityFramework = project.UnityFramework;
            project.SetBuildProperty(mainFramework, "GCC_GENERATE_DEBUGGING_SYMBOLS", "YES");
            project.SetBuildProperty(unityFramework, "GCC_GENERATE_DEBUGGING_SYMBOLS", "YES");
            project.AddFrameworkToProject(unityFramework, "libz.dylib", false);
            project.AddFrameworkToProject(unityFramework, "libc++.dylib", false);
            project.AddFrameworkToProject(unityFramework, "JavaScriptCore.framework", true);
        }

        [AndroidProjectModify]
        public static void OnAndroidProjectModify(BuildProcessorContext context, AndroidProjectManager projectManager)
        {
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>();
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

            if (!buildParams.UploadSymbols || !context.ExportProject)
                return;
            
            var pluginPath = Global.GetPackageFullPath("com.pluginset.bugly");
            var toolJar = Path.GetFullPath(Path.Combine(pluginPath, "Editor", "Tools~", "buglyqq-upload-symbol.jar"));
            Global.CopyFile(toolJar, Path.Combine(context.ProjectPath, "buglyqq-upload-symbol.jar"), null);
        }

        [MenuItem("PluginSet/Collect Bugly Symbols")]
        public static void CollectBuglySymbols()
        {
            CollectAndUploadSymbols(BuildProcessorContext.Default());
        }
        
        [BuildCompletedCallback]
        private static void CollectAndUploadSymbols(BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>();
            if (!buildParams.Enable)
                return;
            
            if (!buildParams.UploadSymbols)
                return;

            string savePath = Path.GetFullPath(Path.Combine(context.ProjectPath, "bugly_symbols"));
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
            Global.ExecuteCommand(sevenZip, "x", "-tzip", "-y", path, $"-o{savePath}");
#else
            string symbolPath = Path.GetFullPath(Path.Combine(context.ProjectPath, "unityLibrary", "symbols"));
#endif
#if UNITY_ANDROID
            CopyAndroidUnitySymbols(savePath, context.DebugMode);
#endif
            
            if (!Directory.Exists(symbolPath))
            {
                Debug.LogWarning($"Cannot find symbols file at path : {symbolPath}");
                return;
            }
            
            Global.CopyFilesTo(savePath, symbolPath, "*.so", SearchOption.AllDirectories);
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
                , "-inputSymbol", savePath
                , "-version", PluginUtil.GetVersionString(PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode)
#if UNITY_IOS
                , "-bundleid", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS)
                , "-platform", "IOS"
#else
                , "-bundleid", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)
                , "-platform", "Android"
#endif
            );
        }

        private static void CopyAndroidUnitySymbols(string targetPath, bool debug)
        {
#if UNITY_EDITOR_WIN
            var unitySymbols = Path.Combine(EditorApplication.applicationContentsPath
#else
            var unitySymbols = Path.Combine(EditorApplication.applicationPath, ".."
#endif
                , "PlaybackEngines", "AndroidPlayer",
                "Variations", "il2cpp", debug ? "Development" : "Release", "Symbols");
            Global.CopyFilesTo(targetPath, unitySymbols, "*.sym.so");
        }
        
        private static void CopyAppleAppSymbolsInApp(string targetPath, string appPath)
        {
            // TODO
            Global.CopyFilesTo(targetPath, Path.Combine(appPath, "dSYMs"), "*.dSYM");
        }
    }
}
