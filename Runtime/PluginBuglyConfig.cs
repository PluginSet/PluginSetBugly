using PluginSet.Core;
using UnityEngine;

namespace PluginSet.Bugly
{
    [PluginSetConfig("Bugly")]
    public class PluginBuglyConfig: ScriptableObject
    {
        public string AppId;

        public LogSeverity AutoLogSeverity;

        public bool AutoQuitApplication = false;

        public long ReportDelay = 0;
    }
}