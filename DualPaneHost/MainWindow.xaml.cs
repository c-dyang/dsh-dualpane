using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace DualPaneHost;

public partial class MainWindow : Window
{
    private const string ConfigName = "dualpane.json";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitWebViewsAsync();
    }

    /// <summary>读配置：优先 exe 旁 dualpane.json，缺省用默认两个 URL。</summary>
    private static (string Left, string Right) LoadUrls()
    {
        string? left = null, right = null;

        // 1. 命令行参数 --left / --right
        var args = Environment.GetCommandLineArgs();
        for (int i = 1; i < args.Length - 1; i++)
        {
            if (args[i] == "--left") left = args[i + 1];
            if (args[i] == "--right") right = args[i + 1];
        }

        // 2. exe 同目录 dualpane.json
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
                catch { /* 配置损坏忽略，用默认 */ }
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
        var baseDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DualPaneHost");
        var env = await CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(baseDir, "left"));
        var env2 = await CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(baseDir, "right"));

        await wvLeft.EnsureCoreWebView2Async(env);
        await wvRight.EnsureCoreWebView2Async(env2);

        wvLeft.Source = new Uri(leftUrl);
        wvRight.Source = new Uri(rightUrl);
    }

    // ── 自定义标题栏控制 ──
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void MinBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
}
