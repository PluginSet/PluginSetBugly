#if UNITY_EDITOR || ENABLE_BUGLY
using System.Collections.Generic;
using PluginSet.Core;
using UnityEngine;

namespace PluginSet.Bugly
{
#if !UNITY_EDITOR
    [PluginRegister]
#endif
    public class PluginBugly : PluginBase, IUserSet
    {
        public override string Name => "Bugly";

        private string _userId;
        private Dictionary<string, object> _userInfo;
        private Dictionary<string, string> _extra;

        private Dictionary<string, string> DefaultExtra
        {
            get
            {
                if (_extra == null)
                {
                    _extra = new Dictionary<string, string>
                    {
                        {"ScreenSolution", $"{Screen.width}x{Screen.height}"},
                        {"deviceModel", SystemInfo.deviceModel},
                        {"deviceName", SystemInfo.deviceName},
                        {"deviceType", SystemInfo.deviceType.ToString()},

                        {"deviceUId", SystemInfo.deviceUniqueIdentifier},
                        {"gDId", $"{SystemInfo.graphicsDeviceID}"},
                        {"gDName", SystemInfo.graphicsDeviceName},
                        {"gDVdr", SystemInfo.graphicsDeviceVendor},
                        {"gDVer", SystemInfo.graphicsDeviceVersion},
                        {"gDVdrID", $"{SystemInfo.graphicsDeviceVendorID}"},

                        {"graphicsMemorySize", $"{SystemInfo.graphicsMemorySize}"},
                        {"systemMemorySize", $"{SystemInfo.systemMemorySize}"},
                        {"UnityVersion", Application.unityVersion},
                    };
                }

                return _extra;
            }
        }


        protected override void Init(PluginSetConfig config)
        {
            var buglyConfig = config.Get<PluginBuglyConfig>("Bugly");

            // Enable the debug log print
            BuglyAgent.ConfigDebugMode(false); // 没什么卵用，就打印一个东西让你去后台查

            // Config default channel, version, user 
            var channel = _managerInstance.GetChannelName();
            var channelId = _managerInstance.GetChannelId();
            var version = PluginUtil.AppVersion;
            var user = SystemInfo.deviceUniqueIdentifier;
            BuglyAgent.ConfigDefault($"{channel}_{channelId}", version, user, buglyConfig.ReportDelay);

            // Config auto report log level, default is LogSeverity.LogError, so the LogError, LogException log will auto report
            BuglyAgent.ConfigAutoReportLogLevel(buglyConfig.AutoLogSeverity);

            // Config auto quit the application make sure only the first one c# exception log will be report, please don't set TRUE if you do not known what are you doing.
            BuglyAgent.ConfigAutoQuitApplication(buglyConfig.AutoQuitApplication);

            // Init the bugly sdk and enable the c# exception handler.
            BuglyAgent.InitWithAppId(buglyConfig.AppId);
            Debug.Log($"BuglyAgent.InitWithAppId with {buglyConfig.AppId}");

            // TODO NOT Required. If you need to report extra data with exception, you can set the extra handler
            BuglyAgent.SetLogCallbackExtrasHandler(LogCallbackExtrasHandler);

            AddEventListener(PluginConstants.NOTIFY_REPORT, delegate(PluginsEventContext context)
            {
                OnPluginCoreReportError(context.Data is ReportError data ? data : default);
            });
        }

        private Dictionary<string, string> LogCallbackExtrasHandler()
        {
            Dictionary<string, string> extras = new Dictionary<string, string>(DefaultExtra);

            extras.Add("ResourceVersion", ResourcesManager.ResourceVersion);

            if (_userInfo != null)
            {
                foreach (var kv in _userInfo)
                {
                    extras.Add($"User_{kv.Key}", kv.Value.ToString());
                }
            }

            return extras;
        }

        private void OnPluginCoreReportError(ReportError error)
        {
            if (error.ThreadName.Equals("MainThread"))
                return;

            BuglyAgent.ReportException(null, error.Condition, error.StackTrack);
        }

        public void SetUserInfo(bool isNewUser, string userId, Dictionary<string, object> pairs = null)
        {
            if (_userId != userId)
                ClearUserInfo();

            _userId = userId;
            BuglyAgent.SetUserId(userId);
            
            if (pairs != null)
            {
                _userInfo = new Dictionary<string, object>(pairs);
            }
        }

        public void ClearUserInfo()
        {
            BuglyAgent.SetUserId(string.Empty);
            _userInfo = null;
        }

        public void FlushUserInfo()
        {
        }
    }
}
#endif
