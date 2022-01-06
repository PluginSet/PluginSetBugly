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
        
        [OnBuildAppCompleted]
        public static void OnBuildAppCompleted(BuildProcessorContext context, string targetPath)
        {
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>("Bugly");
            if (!buildParams.Enable)
                return;

            if (!context.ProductMode)
                return;

            if (!buildParams.UploadSymbols)
                return;

            var handler = context.Get<BuildTaskHandler>("handler");
            handler.AddTaskAfter(new BuildUploadBuglySymbols());
        }
    }
}
