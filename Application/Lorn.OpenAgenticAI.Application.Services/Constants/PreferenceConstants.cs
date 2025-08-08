namespace Lorn.OpenAgenticAI.Application.Services.Constants;

/// <summary>
/// 偏好设置常量定义
/// </summary>
public static class PreferenceConstants
{
    /// <summary>
    /// 界面偏好设置分类
    /// </summary>
    public static class UI
    {
        public const string CATEGORY = "UI";

        /// <summary>主题设置</summary>
        public const string THEME = "Theme";

        /// <summary>字体大小</summary>
        public const string FONT_SIZE = "FontSize";

        /// <summary>布局方式</summary>
        public const string LAYOUT = "Layout";

        /// <summary>是否显示侧边栏</summary>
        public const string SHOW_SIDEBAR = "ShowSidebar";

        /// <summary>是否显示工具栏</summary>
        public const string SHOW_TOOLBAR = "ShowToolbar";

        /// <summary>是否显示状态栏</summary>
        public const string SHOW_STATUSBAR = "ShowStatusbar";

        /// <summary>窗口透明度</summary>
        public const string WINDOW_OPACITY = "WindowOpacity";

        /// <summary>动画效果启用</summary>
        public const string ENABLE_ANIMATIONS = "EnableAnimations";

        /// <summary>界面缩放比例</summary>
        public const string SCALE_FACTOR = "ScaleFactor";

        /// <summary>颜色方案</summary>
        public const string COLOR_SCHEME = "ColorScheme";

        /// <summary>默认值</summary>
        public static class Defaults
        {
            public const string THEME = "Auto";
            public const int FONT_SIZE = 14;
            public const string LAYOUT = "Standard";
            public const bool SHOW_SIDEBAR = true;
            public const bool SHOW_TOOLBAR = true;
            public const bool SHOW_STATUSBAR = true;
            public const double WINDOW_OPACITY = 1.0;
            public const bool ENABLE_ANIMATIONS = true;
            public const double SCALE_FACTOR = 1.0;
            public const string COLOR_SCHEME = "Default";
        }

        /// <summary>可选值</summary>
        public static class Options
        {
            public static readonly string[] THEMES = { "Light", "Dark", "Auto" };
            public static readonly int[] FONT_SIZES = { 10, 12, 14, 16, 18, 20, 24 };
            public static readonly string[] LAYOUTS = { "Compact", "Standard", "Spacious" };
            public static readonly string[] COLOR_SCHEMES = { "Default", "Blue", "Green", "Red", "Purple" };
        }
    }

    /// <summary>
    /// 语言偏好设置分类
    /// </summary>
    public static class Language
    {
        public const string CATEGORY = "Language";

        /// <summary>界面语言</summary>
        public const string UI_LANGUAGE = "UILanguage";

        /// <summary>输入语言</summary>
        public const string INPUT_LANGUAGE = "InputLanguage";

        /// <summary>输出语言</summary>
        public const string OUTPUT_LANGUAGE = "OutputLanguage";

        /// <summary>日期时间格式</summary>
        public const string DATETIME_FORMAT = "DateTimeFormat";

        /// <summary>数字格式</summary>
        public const string NUMBER_FORMAT = "NumberFormat";

        /// <summary>货币格式</summary>
        public const string CURRENCY_FORMAT = "CurrencyFormat";

        /// <summary>时区设置</summary>
        public const string TIMEZONE = "Timezone";

        /// <summary>默认值</summary>
        public static class Defaults
        {
            public const string UI_LANGUAGE = "zh-CN";
            public const string INPUT_LANGUAGE = "zh-CN";
            public const string OUTPUT_LANGUAGE = "zh-CN";
            public const string DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
            public const string NUMBER_FORMAT = "N2";
            public const string CURRENCY_FORMAT = "C2";
            public const string TIMEZONE = "China Standard Time";
        }

        /// <summary>可选值</summary>
        public static class Options
        {
            public static readonly string[] SUPPORTED_LANGUAGES = { "zh-CN", "en-US", "ja-JP", "ko-KR" };
            public static readonly string[] DATETIME_FORMATS = {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss",
                "dd/MM/yyyy HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss"
            };
            public static readonly string[] NUMBER_FORMATS = { "N0", "N1", "N2", "N3", "N4" };
            public static readonly string[] CURRENCY_FORMATS = { "C0", "C1", "C2", "C3" };
        }
    }

    /// <summary>
    /// 操作偏好设置分类
    /// </summary>
    public static class Operation
    {
        public const string CATEGORY = "Operation";

        /// <summary>默认LLM模型</summary>
        public const string DEFAULT_LLM_MODEL = "DefaultLLMModel";

        /// <summary>任务超时时间（秒）</summary>
        public const string TASK_TIMEOUT = "TaskTimeout";

        /// <summary>自动保存间隔（秒）</summary>
        public const string AUTO_SAVE_INTERVAL = "AutoSaveInterval";

        /// <summary>最大并发任务数</summary>
        public const string MAX_CONCURRENT_TASKS = "MaxConcurrentTasks";

        /// <summary>是否启用自动保存</summary>
        public const string ENABLE_AUTO_SAVE = "EnableAutoSave";

        /// <summary>是否启用操作确认</summary>
        public const string ENABLE_CONFIRMATION = "EnableConfirmation";

        /// <summary>是否启用操作日志</summary>
        public const string ENABLE_OPERATION_LOG = "EnableOperationLog";

        /// <summary>默认工作目录</summary>
        public const string DEFAULT_WORK_DIRECTORY = "DefaultWorkDirectory";

        /// <summary>临时文件清理间隔（小时）</summary>
        public const string TEMP_CLEANUP_INTERVAL = "TempCleanupInterval";

        /// <summary>智能提示启用</summary>
        public const string ENABLE_SMART_SUGGESTIONS = "EnableSmartSuggestions";

        /// <summary>响应速度优先级</summary>
        public const string RESPONSE_SPEED_PRIORITY = "ResponseSpeedPriority";

        /// <summary>默认值</summary>
        public static class Defaults
        {
            public const string DEFAULT_LLM_MODEL = "gpt-3.5-turbo";
            public const int TASK_TIMEOUT = 300; // 5分钟
            public const int AUTO_SAVE_INTERVAL = 60; // 1分钟
            public const int MAX_CONCURRENT_TASKS = 5;
            public const bool ENABLE_AUTO_SAVE = true;
            public const bool ENABLE_CONFIRMATION = true;
            public const bool ENABLE_OPERATION_LOG = true;
            public const string DEFAULT_WORK_DIRECTORY = "%UserProfile%\\Documents\\OpenAgenticAI";
            public const int TEMP_CLEANUP_INTERVAL = 24; // 24小时
            public const bool ENABLE_SMART_SUGGESTIONS = true;
            public const string RESPONSE_SPEED_PRIORITY = "Balanced";
        }

        /// <summary>可选值</summary>
        public static class Options
        {
            public static readonly string[] LLM_MODELS = {
                "gpt-3.5-turbo",
                "gpt-4",
                "gpt-4-turbo",
                "claude-3",
                "gemini-pro"
            };
            public static readonly int[] TIMEOUT_OPTIONS = { 30, 60, 120, 300, 600, 1200 }; // 秒
            public static readonly int[] SAVE_INTERVALS = { 30, 60, 120, 300, 600 }; // 秒
            public static readonly int[] CONCURRENT_TASK_OPTIONS = { 1, 3, 5, 10, 20 };
            public static readonly int[] CLEANUP_INTERVALS = { 1, 6, 12, 24, 48, 72 }; // 小时
            public static readonly string[] SPEED_PRIORITIES = { "Speed", "Balanced", "Quality" };
        }
    }

    /// <summary>
    /// 快捷键偏好设置分类
    /// </summary>
    public static class Shortcuts
    {
        public const string CATEGORY = "Shortcuts";

        /// <summary>新建任务</summary>
        public const string NEW_TASK = "NewTask";

        /// <summary>保存当前工作</summary>
        public const string SAVE_WORK = "SaveWork";

        /// <summary>打开工作流</summary>
        public const string OPEN_WORKFLOW = "OpenWorkflow";

        /// <summary>运行任务</summary>
        public const string RUN_TASK = "RunTask";

        /// <summary>停止任务</summary>
        public const string STOP_TASK = "StopTask";

        /// <summary>设置页面</summary>
        public const string OPEN_SETTINGS = "OpenSettings";

        /// <summary>帮助页面</summary>
        public const string OPEN_HELP = "OpenHelp";

        /// <summary>退出应用</summary>
        public const string EXIT_APP = "ExitApp";

        /// <summary>默认值</summary>
        public static class Defaults
        {
            public const string NEW_TASK = "Ctrl+N";
            public const string SAVE_WORK = "Ctrl+S";
            public const string OPEN_WORKFLOW = "Ctrl+O";
            public const string RUN_TASK = "F5";
            public const string STOP_TASK = "Ctrl+Break";
            public const string OPEN_SETTINGS = "Ctrl+,";
            public const string OPEN_HELP = "F1";
            public const string EXIT_APP = "Alt+F4";
        }
    }

    /// <summary>
    /// 收藏和快速访问分类
    /// </summary>
    public static class Favorites
    {
        public const string CATEGORY = "Favorites";

        /// <summary>收藏的工作流</summary>
        public const string WORKFLOWS = "Workflows";

        /// <summary>收藏的Agent</summary>
        public const string AGENTS = "Agents";

        /// <summary>收藏的模板</summary>
        public const string TEMPLATES = "Templates";

        /// <summary>最近使用的项目</summary>
        public const string RECENT_ITEMS = "RecentItems";

        /// <summary>快速访问面板项目</summary>
        public const string QUICK_ACCESS_ITEMS = "QuickAccessItems";

        /// <summary>默认值</summary>
        public static class Defaults
        {
            public const string WORKFLOWS = "[]";
            public const string AGENTS = "[]";
            public const string TEMPLATES = "[]";
            public const string RECENT_ITEMS = "[]";
            public const string QUICK_ACCESS_ITEMS = "[]";
        }
    }
}
