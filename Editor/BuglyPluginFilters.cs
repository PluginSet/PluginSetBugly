using PluginSet.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace PluginSet.Bugly.Editor
{
    [InitializeOnLoad]
    public static class BuglyPluginFilters
    {
        private static bool FilterPlugins(string s, BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildBuglyParams>("Bugly");
            if (!buildParams.Enable)
            {
                Debug.Log("Filter lib file :::::::  " + s);
            }
                
            return !buildParams.Enable;
        }

        
        static BuglyPluginFilters()
        {
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins", FilterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/iOS", FilterPlugins);
            
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android", FilterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android/libs", FilterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android/libs/arm64-v8a", FilterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android/libs/armeabi-v7a", FilterPlugins);
            PluginFilter.RegisterFilter("com.pluginset.bugly/Plugins/Android/libs/x86", FilterPlugins);
        }
    }
}