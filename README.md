# DSH Dual Pane

一个窗口内两列 WebView2 宿主：左 DSH 对话，右历史注入搜索页。可拖分隔条调比例，极简无浏览器工具栏。

## 用法

1. 从 Release 下载 `DualPaneHost.exe` + `dualpane.json`
2. 编辑 `dualpane.json` 指定左右两页 URL（与 exe 同目录）
3. 双击 `DualPaneHost.exe` 打开

## 配置 dualpane.json

```json
{
  "left": "http://192.168.0.25:3080",
  "right": "http://192.168.0.25:7878/webhook/page?name=hist-search"
}
```

也支持命令行：`DualPaneHost.exe --left <url> --right <url>`

## 技术

- WPF (.NET 8) + Microsoft.Web.WebView2
- 两个独立 WebView2 环境（ExclusiveUserDataFolder）→ 各自独立浏览器上下文，DSH 的 localStorage 正常
- GridSplitter 拖拽调左右比例
- 自定义无边框标题栏（可拖动窗口）

## 构建

GitHub Actions 自动编译（push tag v* 或手动触发 workflow_dispatch），产物发布到 Release。
