using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.IO;
using DarknessRise.Models;

namespace DarknessRise.Services;

public class PythonScraperService
{
    private Process? _pythonProcess;
    private CancellationTokenSource? _cancellationTokenSource;

    public event EventHandler<string>? OnLogReceived;
    public event EventHandler<List<Tweet>>? OnTweetsScraped;
    public event EventHandler? OnScrapingCompleted;
    public event EventHandler<Exception>? OnError;

    private async Task ExecuteScrapingAsync(string arguments, string description)
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var pythonScriptPath = Path.Combine(
                Path.GetDirectoryName(AppContext.BaseDirectory)!,
                "..", "..", "..", "..",
                "python", "x_miner", "cli.py");

            if (!File.Exists(pythonScriptPath))
            {
                pythonScriptPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "python", "x_miner", "cli.py");
            }

            var pythonPath = Environment.GetEnvironmentVariable("PYTHON_PATH") ?? "python";
            if (pythonPath == "python" && !IsPythonOnPath())
            {
                pythonPath = @"C:\Python312\python.exe";
                if (!File.Exists(pythonPath))
                    pythonPath = @"C:\Users\asus\AppData\Local\Programs\Python\Python312\python.exe";
            }

            OnLogReceived?.Invoke(this, "🔍 正在查找Python脚本...");
            OnLogReceived?.Invoke(this, $"📁 脚本路径: {pythonScriptPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{pythonScriptPath}\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            _pythonProcess = new Process { StartInfo = startInfo };
            _pythonProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OnLogReceived?.Invoke(this, e.Data);
                    TryParseTweets(e.Data);
                }
            };
            _pythonProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OnLogReceived?.Invoke(this, $"⚠️ {e.Data}");
                }
            };
            _pythonProcess.EnableRaisingEvents = true;
            _pythonProcess.Exited += (sender, e) =>
            {
                OnScrapingCompleted?.Invoke(this, EventArgs.Empty);
            };

            OnLogReceived?.Invoke(this, "🚀 启动Python进程...");
            _pythonProcess.Start();
            _pythonProcess.BeginOutputReadLine();
            _pythonProcess.BeginErrorReadLine();

            OnLogReceived?.Invoke(this, $"✅ 开始抓取: {description}");
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
        }
    }

    public async Task StartScrapingAsync(string keyword, int maxTweets)
    {
        await ExecuteScrapingAsync($"--keyword \"{keyword}\" --max-tweets {maxTweets}", 
                                  $"关键词='{keyword}', 数量={maxTweets}");
    }

    public async Task StartScrapingByUrlAsync(string url, int maxTweets)
    {
        await ExecuteScrapingAsync($"--url \"{url}\" --max-tweets {maxTweets}", 
                                  $"网址='{url}', 数量={maxTweets}");
    }

    private void TryParseTweets(string data)
    {
        try
        {
            if (data.StartsWith("TWEETS:"))
            {
                var jsonData = data.Substring("TWEETS:".Length);
                var tweets = JsonSerializer.Deserialize<List<TweetDto>>(jsonData);
                if (tweets != null)
                {
                    var modelTweets = tweets.Select(t => new Tweet
                    {
                        AuthorName = t.author_name,
                        AuthorHandle = t.author_handle,
                        Content = t.content,
                        Timestamp = t.timestamp,
                        Likes = t.likes,
                        Retweets = t.retweets,
                        Replies = t.replies,
                        Bookmarks = t.bookmarks,
                        Url = t.url,
                        ScrapedAt = t.scraped_at
                    }).ToList();

                    OnTweetsScraped?.Invoke(this, modelTweets);
                }
            }
        }
        catch (Exception ex)
        {
            OnLogReceived?.Invoke(this, $"❌ 解析推文失败: {ex.Message}");
        }
    }

    public void StopScraping()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                _pythonProcess.Kill();
                _pythonProcess.WaitForExit(1000);
            }
        }
        catch
        {
            // 忽略停止错误
        }
    }

    private static bool IsPythonOnPath()
    {
        try
        {
            using var check = Process.Start(new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            check?.WaitForExit(2000);
            return check != null && check.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private class TweetDto
    {
        public string author_name { get; set; } = string.Empty;
        public string author_handle { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
        public string timestamp { get; set; } = string.Empty;
        public int likes { get; set; }
        public int retweets { get; set; }
        public int replies { get; set; }
        public int bookmarks { get; set; }
        public string url { get; set; } = string.Empty;
        public string scraped_at { get; set; } = string.Empty;
    }
}
