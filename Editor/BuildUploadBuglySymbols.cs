using System.IO;
using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace PluginSet.Bugly.Editor
{
    public class BuildUploadBuglySymbols: IBuildProcessorTask
    {
        public void Execute(BuildProcessorContext context)
        {
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
            
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>("Bugly");

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
            Global.CheckAndDeletePath(symbolPath);
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