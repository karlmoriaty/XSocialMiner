using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using DarknessRise.Models;
using DarknessRise.Services;
using Microsoft.Win32;

namespace DarknessRise.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "🚀 Local Social Miner";

    [ObservableProperty]
    private string _statusMessage = "就绪 - 等待操作";

    [ObservableProperty]
    private string _statusDetail = "请设置搜索关键词并开始抓取";

    [ObservableProperty]
    private ObservableCollection<Tweet> _tweets = new();

    [ObservableProperty]
    private ObservableCollection<Tweet> _filteredTweets = new();

    [ObservableProperty]
    private ObservableCollection<ScrapeTask> _scrapeTasks = new();

    [ObservableProperty]
    private ObservableCollection<string> _logMessages = new();

    [ObservableProperty]
    private string _searchKeyword = "量子计算";

    [ObservableProperty]
    private string _targetUrl = "";

    [ObservableProperty]
    private bool _isKeywordMode = true;

    [ObservableProperty]
    private bool _isUrlMode = false;

    [ObservableProperty]
    private int _maxTweets = 50;

    private bool _isScraping = false;

    public bool IsScraping
    {
        get => _isScraping;
        set
        {
            if (SetProperty(ref _isScraping, value))
            {
                StartScrapingCommand?.NotifyCanExecuteChanged();
                StopScrapingCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    [ObservableProperty]
    private bool _isHeadlessMode = false;

    [ObservableProperty]
    private double _currentProgress = 0;

    [ObservableProperty]
    private string _progressText = "准备中";

    [ObservableProperty]
    private string _progressPercentage = "0%";

    [ObservableProperty]
    private int _tweetCount = 0;

    [ObservableProperty]
    private int _totalTweets = 0;

    [ObservableProperty]
    private int _taskCount = 0;

    [ObservableProperty]
    private int _totalLikes = 0;

    [ObservableProperty]
    private string _filterText = "";

    private readonly PythonScraperService _scraperService;
    private readonly Dispatcher _dispatcher;
    private int _scrapedCount = 0;

    public MainWindowViewModel()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _scraperService = new PythonScraperService();
        
        _scraperService.OnLogReceived += OnLogReceived;
        _scraperService.OnTweetsScraped += OnTweetsScraped;
        _scraperService.OnScrapingCompleted += OnScrapingCompleted;
        _scraperService.OnError += OnError;
        
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        try
        {
            AddLog("📂 正在加载数据...");
            Tweets = await App.DbService.GetAllTweetsAsync();
            FilteredTweets = new ObservableCollection<Tweet>(Tweets);
            ScrapeTasks = await App.DbService.GetAllTasksAsync();
            UpdateStatistics();
            StatusMessage = "✅ 数据加载完成";
            AddLog("✅ 数据加载完成");
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 加载失败: {ex.Message}";
            AddLog($"❌ 加载失败: {ex.Message}");
        }
    }

    private void OnLogReceived(object? sender, string message)
    {
        _dispatcher.Invoke(() =>
        {
            AddLog(message);
        });
    }

    private async void OnTweetsScraped(object? sender, List<Tweet> newTweets)
    {
        _dispatcher.Invoke(async () =>
        {
            foreach (var tweet in newTweets)
            {
                Tweets.Insert(0, tweet);
                FilteredTweets.Insert(0, tweet);
                await App.DbService.AddTweetAsync(tweet);
                _scrapedCount++;
                
                TweetCount = _scrapedCount;
                UpdateStatistics();
                UpdateProgress();
            }
            AddLog($"📥 已保存 {newTweets.Count} 条推文");
        });
    }

    private void OnScrapingCompleted(object? sender, EventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            IsScraping = false;
            CurrentProgress = 100;
            ProgressPercentage = "100%";
            ProgressText = "抓取完成";
            StatusMessage = "✅ 抓取完成";
            StatusDetail = $"成功抓取 {_scrapedCount} 条推文";
            AddLog("✅ 抓取任务完成");
            UpdateStatistics();
        });
    }

    private void OnError(object? sender, Exception ex)
    {
        _dispatcher.Invoke(() =>
        {
            IsScraping = false;
            CurrentProgress = 0;
            StatusMessage = $"❌ 错误: {ex.Message}";
            StatusDetail = "发生错误，请查看日志";
            AddLog($"❌ 错误: {ex.Message}");
        });
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogMessages.Add($"[{timestamp}] {message}");
        
        if (LogMessages.Count > 100)
        {
            LogMessages.RemoveAt(0);
        }
    }

    private void UpdateProgress()
    {
        var progress = Math.Min((_scrapedCount * 100) / Math.Max(MaxTweets, 1), 99);
        CurrentProgress = progress;
        ProgressPercentage = $"{progress}%";
        ProgressText = $"已抓取 {_scrapedCount}/{MaxTweets}";
    }

    private void UpdateStatistics()
    {
        TotalTweets = Tweets.Count;
        TaskCount = ScrapeTasks.Count;
        TotalLikes = Tweets.Sum(t => t.Likes);
    }

    [RelayCommand(CanExecute = nameof(CanStartScraping))]
    private async Task StartScraping()
    {
        if (IsKeywordMode && string.IsNullOrWhiteSpace(SearchKeyword))
        {
            StatusMessage = "⚠️ 请输入搜索关键词";
            AddLog("⚠️ 请输入搜索关键词");
            return;
        }
        
        if (IsUrlMode && string.IsNullOrWhiteSpace(TargetUrl))
        {
            StatusMessage = "⚠️ 请输入网址";
            AddLog("⚠️ 请输入网址");
            return;
        }
        
        IsScraping = true;
        _scrapedCount = 0;
        CurrentProgress = 0;
        ProgressPercentage = "0%";
        ProgressText = "正在初始化...";
        LogMessages.Clear();
        
        var taskDescription = IsKeywordMode ? $"关键词='{SearchKeyword}'" : $"网址='{TargetUrl}'";
        var task = new ScrapeTask
        {
            Keyword = IsKeywordMode ? SearchKeyword : TargetUrl,
            MaxTweets = MaxTweets,
            Status = "Running",
            CreatedAt = DateTime.Now
        };
        await App.DbService.AddTaskAsync(task);
        TaskCount++;
        
        AddLog($"🚀 开始抓取: {taskDescription}, 数量={MaxTweets}");
        StatusMessage = "🔄 正在抓取...";
        StatusDetail = "正在启动浏览器...";
        
        if (IsKeywordMode)
        {
            await _scraperService.StartScrapingAsync(SearchKeyword, MaxTweets);
        }
        else
        {
            await _scraperService.StartScrapingByUrlAsync(TargetUrl, MaxTweets);
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopScraping))]
    private void StopScraping()
    {
        AddLog("⏹️ 正在停止抓取...");
        _scraperService.StopScraping();
        IsScraping = false;
        StatusMessage = "⏹️ 抓取已停止";
        StatusDetail = "用户停止了抓取任务";
        StartScrapingCommand.NotifyCanExecuteChanged();
        StopScrapingCommand.NotifyCanExecuteChanged();
    }

    private bool CanStartScraping() => !IsScraping;
    private bool CanStopScraping() => IsScraping;

    [RelayCommand]
    private async Task RefreshData()
    {
        try
        {
            AddLog("🔄 正在刷新数据...");
            Tweets = await App.DbService.GetAllTweetsAsync();
            FilteredTweets = new ObservableCollection<Tweet>(Tweets);
            ScrapeTasks = await App.DbService.GetAllTasksAsync();
            UpdateStatistics();
            StatusMessage = "✅ 数据已刷新";
            AddLog("✅ 数据已刷新");
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 刷新失败: {ex.Message}";
            AddLog($"❌ 刷新失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Filter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            FilteredTweets = new ObservableCollection<Tweet>(Tweets);
            AddLog("🔍 筛选已清除");
        }
        else
        {
            var filtered = Tweets.Where(t =>
                t.AuthorName.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                t.AuthorHandle.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                t.Content.Contains(FilterText, StringComparison.OrdinalIgnoreCase)).ToList();
            FilteredTweets = new ObservableCollection<Tweet>(filtered);
            AddLog($"🔍 筛选完成: 找到 {filtered.Count} 条推文");
        }
    }

    [RelayCommand]
    private async Task ClearData()
    {
        var result = System.Windows.MessageBox.Show(
            "确定要清空所有数据吗？此操作不可恢复！",
            "确认清空",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                AddLog("🗑️ 正在清空数据...");
                Tweets.Clear();
                FilteredTweets.Clear();
                await RefreshData();
                StatusMessage = "✅ 数据已清空";
                AddLog("✅ 数据已清空");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 清空失败: {ex.Message}";
                AddLog($"❌ 清空失败: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportJson()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            DefaultExt = "json",
            FileName = $"tweets_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                AddLog($"📄 正在导出到 {saveFileDialog.FileName}...");
                await App.DbService.ExportToJsonAsync(FilteredTweets, saveFileDialog.FileName);
                StatusMessage = $"✅ 已导出 {FilteredTweets.Count} 条推文";
                AddLog($"✅ 已导出 {FilteredTweets.Count} 条推文到 JSON");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 导出失败: {ex.Message}";
                AddLog($"❌ 导出失败: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"tweets_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                AddLog($"📊 正在导出到 {saveFileDialog.FileName}...");
                await App.DbService.ExportToCsvAsync(FilteredTweets, saveFileDialog.FileName);
                StatusMessage = $"✅ 已导出 {FilteredTweets.Count} 条推文";
                AddLog($"✅ 已导出 {FilteredTweets.Count} 条推文到 CSV");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 导出失败: {ex.Message}";
                AddLog($"❌ 导出失败: {ex.Message}");
            }
        }
    }
}
