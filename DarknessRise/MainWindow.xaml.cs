using System.Windows;

namespace DarknessRise;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "🚀 Local Social Miner 使用说明\n\n" +
            "1. 在搜索设置中输入关键词\n" +
            "2. 设置要抓取的最大推文数\n" +
            "3. 点击\"开始抓取\"按钮\n" +
            "4. 浏览器会自动打开并开始抓取\n" +
            "5. 抓取的数据会实时显示在右侧\n" +
            "6. 可以导出数据为JSON或CSV格式\n\n" +
            "💡 提示: 请先在浏览器中登录X账号以获取更好的抓取效果",
            "使用说明",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "🚀 Local Social Miner v1.0\n\n" +
            "一个本地化的社交媒体数据抓取工具\n" +
            "使用 C# + WPF + Python + Playwright 构建\n\n" +
            "© 2024 Local Social Miner",
            "关于",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
