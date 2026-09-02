using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace DualPaneHost;

public partial class MainWindow : Window
{
    private const string ConfigName = "dualpane.json";
    private const string StateName = "dualpane.state.json";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitWebViewsAsync();
        Closing += OnClosing;

        // 恢复窗口状态（尺寸 + 分隔比例）
        RestoreWindowState();
        Loaded += (_, _) => ApplySplitRatio();
    }

    /// <summary>读配置：exe 旁 dualpane.json（URL），或 --left/--right 参数。</summary>
    private static (string Left, string Right) LoadUrls()
    {
        string? left = null, right = null;

        var args = Environment.GetCommandLineArgs();
        for (int i = 1; i < args.Length - 1; i++)
        {
            if (args[i] == "--left") left = args[i + 1];
            if (args[i] == "--right") right = args[i + 1];
        }

        if (left == null || right == null)
        {
            var cfgPath = Path.Combine(AppContext.BaseDirectory, ConfigName);
            if (File.Exists(cfgPath))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(cfgPath));
                    var root = doc.RootElement;
                    if (left == null && root.TryGetProperty("left", out var l)) left = l.GetString();
                    if (right == null && root.TryGetProperty("right", out var r)) right = r.GetString();
                }
                catch { }
            }
        }

        return (
            string.IsNullOrWhiteSpace(left) ? "http://192.168.0.25:3080" : left,
            string.IsNullOrWhiteSpace(right) ? "http://192.168.0.25:7878/webhook/page?name=hist-search" : right
        );
    }

    private async System.Threading.Tasks.Task InitWebViewsAsync()
    {
        var (leftUrl, rightUrl) = LoadUrls();

        // 两个独立 WebView2 环境（各自 userDataFolder → 独立上下文，DSH localStorage 正常）
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DualPaneHost");
        var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(baseDir, "left"));
        var env2 = await CoreWebView2Environment.CreateAsync(null, Path.Combine(baseDir, "right"));

        await wvLeft.EnsureCoreWebView2Async(env);
        await wvRight.EnsureCoreWebView2Async(env2);

        wvLeft.Source = new Uri(leftUrl);
        wvRight.Source = new Uri(rightUrl);
    }

    // ── 窗口状态记忆（尺寸 + 位置 + 分隔比例）──

    private void RestoreWindowState()
    {
        try
        {
            var statePath = Path.Combine(AppContext.BaseDirectory, StateName);
            if (!File.Exists(statePath)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(statePath));
            var r = doc.RootElement;
            if (r.TryGetProperty("width", out var w) && r.TryGetProperty("height", out var h))
            {
                Width = Math.Max(800, w.GetInt32());
                Height = Math.Max(500, h.GetInt32());
            }
            if (r.TryGetProperty("leftRatio", out var lr))
                _leftRatio = Math.Clamp(lr.GetDouble(), 0.15, 0.85);
        }
        catch { }
    }

    private double _leftRatio = 0.66;

    private void ApplySplitRatio()
    {
        colLeft.Width = new GridLength(_leftRatio, GridUnitType.Star);
        colRight.Width = new GridLength(1 - _leftRatio, GridUnitType.Star);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            var state = new
            {
                width = (int)ActualWidth,
                height = (int)ActualHeight,
                left = (int)Left,
                top = (int)Top,
                leftRatio = Math.Round(colLeft.ActualWidth / Math.Max(1, colLeft.ActualWidth + colRight.ActualWidth), 4),
            };
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, StateName),
                JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
