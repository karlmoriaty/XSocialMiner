# XSocialMiner - 社交数据采集与分析工具

一款面向独立开发者和研究者的本地化社交数据采集与可视化工具。通过 Python + Playwright 爬虫采集 X (Twitter) 数据，WPF 前端提供实时监控、数据浏览和趋势分析能力。

## 功能特性

- **数据采集调度**: 通过 Python 爬虫采集 X (Twitter) 数据，支持关键词搜索和用户主页抓取
- **实时监控**: WPF 前端展示采集状态、任务进度、错误日志
- **数据浏览**: SQLite 本地存储，支持 Tweet 按关键词/时间/用户查询
- **趋势分析**: 内置统计图表，查看话题热度变化
- **多任务管理**: 支持同时配置多个采集任务（关键词、用户列表）

## 技术栈

- **C# 12** + **WPF** + **.NET 8.0**
- **Entity Framework Core + SQLite**（嵌入式数据库）
- **CommunityToolkit.Mvvm**（MVVM 框架）
- **Python 3.12** + **Playwright** + **browser-cookie3**（爬虫端）

## 项目结构

```
XSocialMiner/
├── DarknessRise/                         # WPF 前端项目
│   ├── DarknessRise.csproj
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── ViewModels/MainWindowViewModel.cs
│   ├── Models/                           # Tweet / ScrapeTask / Configuration
│   ├── Data/AppDbContext.cs              # SQLite 数据上下文
│   └── Services/
│       ├── DatabaseService.cs
│       └── PythonScraperService.cs       # 与 Python 爬虫端的桥接
├── python/
│   ├── requirements.txt
│   └── x_miner/
│       ├── __init__.py
│       ├── cli.py                        # CLI 入口
│       ├── scraper.py                    # Playwright 爬虫核心
│       └── utils.py                      # Cookie 加载、输出格式化
└── README.md
```

## 快速开始

### 1. 安装 Python 依赖

```bash
cd python
pip install -r requirements.txt
playwright install chromium
```

### 2. 运行 WPF 前端

```bash
dotnet run --project DarknessRise/DarknessRise.csproj
```

首次运行会自动创建 SQLite 数据库文件（位于 `%LocalApplicationData%\LocalSocialMiner\lsm.db`）。

### 3. 独立运行 Python 爬虫（可选）

```bash
# 按关键词搜索
python python/x_miner/cli.py --keyword "AI" --max-tweets 20

# 按 URL 抓取（用户主页、搜索结果、话题页等）
python python/x_miner/cli.py --url "https://x.com/elonmusk" --max-tweets 10
python python/x_miner/cli.py --url "https://x.com/search?q=python&f=live" --max-tweets 30
```

## Python 输出协议

WPF 端通过 `PythonScraperService` 启动爬虫进程，约定如下：

- **脚本路径**: `python/x_miner/cli.py`（相对于 WPF 可执行文件的相对路径）
- **Python 定位**: 优先用 `PYTHON_PATH` 环境变量 → 系统 PATH 中的 `python` → 常见安装路径回退
- **日志**: Python 日志输出到 **stderr**，WPF 实时显示
- **数据**: 推文数据以 `TWEETS:` 前缀 + JSON 数组输出到 **stdout**，WPF 实时解析入库

输出格式：

```
TWEETS:[{"author_name":"...","author_handle":"...","content":"...","timestamp":"2024-01-01T00:00:00+00:00","likes":10,"retweets":2,"replies":1,"bookmarks":0,"url":"https://x.com/...","scraped_at":"2024-01-01T12:00:00+00:00"}]
```

## Cookie / 登录

`browser-cookie3` 会尝试从本地 Chrome/Edge/Firefox 加载 X (Twitter) cookie。如果浏览器已登录，爬虫可以采集登录后可见的更多内容；如果未登录，仍可采集公开推文。

## 注意事项

- 仅用于采集公开数据，遵守目标平台的服务条款
- 合理控制抓取频率，避免对目标平台造成压力
