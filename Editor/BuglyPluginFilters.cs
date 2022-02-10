using PluginSet.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace PluginSet.Bugly.Editor
{
    [InitializeOnLoad]
    public static class BuglyPluginFilters
    {
        static BuglyPluginFilters()
        {
            var filterPlugins = PluginFilter.IsBuildParamsEnable<BuildBuglyParams>();
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins", filterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/iOS", filterPlugins);
            
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android", filterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android/libs", filterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android/libs/arm64-v8a", filterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android/libs/armeabi-v7a", filterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android/libs/x86", filterPlugins);
        }
    }
}