using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEngine;

namespace PluginSet.Bugly.Editor
{
    [BuildChannelsParams("Bugly", "Bugly相关设置")]
    [VisibleCaseBoolValue("SupportAndroid", true)]
    [VisibleCaseBoolValue("SupportIOS", true)]
    public class BuildBuglyParams: ScriptableObject
    {
        [Tooltip("是否启用Bugly")]
        public bool Enable;
        
        [Tooltip("Bugly提供的App ID")]
        public string AppId;
        
        [Tooltip("Bugly提供的App Key")]
        public string AppKey;

        [Tooltip("自动上传日志等级，选中等级及以上都会自动上传")]
        public LogSeverity AutoLogSeverity = LogSeverity.LogError;
        
        [Tooltip("自动退出应用，慎重开启")]
        public bool AutoQuitApplication = false;

        [Tooltip("上报延时")]
        public long ReportDelay = 0;

        [Tooltip("上传符号文件")]
        public bool UploadSymbols = false;
    }
}