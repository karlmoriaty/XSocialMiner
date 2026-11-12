# XSocialMiner

A local social-data collection and analysis tool for indie developers and
researchers. A Python + Playwright scraper collects data from X (Twitter); a WPF
front end provides job control, live progress, browsing and trend charts.

Everything stays on your machine: tweets go into a local SQLite database, and no
data is sent anywhere else.

## Features

- **Collection jobs** — keyword search and profile/URL scraping driven by the
  Python scraper.
- **Live monitoring** — the WPF host shows job state, progress and scraper errors in
  real time.
- **Browsing** — tweets stored in SQLite, queryable by keyword, time range and
  author.
- **Trend analysis** — built-in charts for topic popularity over time.
- **Multiple tasks** — configure several keywords or user lists at once.

## Stack

- C# 12 / WPF / .NET 8.0
- Entity Framework Core + SQLite
- CommunityToolkit.Mvvm
- Python 3.12 + Playwright + browser-cookie3

## Layout

```
XSocialMiner/
├── DarknessRise/                   WPF front end
│   ├── ViewModels/MainWindowViewModel.cs
│   ├── Models/                     Tweet / ScrapeTask / Configuration
│   ├── Data/AppDbContext.cs        SQLite context
│   └── Services/
│       ├── DatabaseService.cs
│       └── PythonScraperService.cs bridge to the scraper process
└── python/
    ├── requirements.txt
    └── x_miner/
        ├── cli.py                  CLI entry point
        ├── scraper.py              Playwright scraper
        └── utils.py                cookie loading, output formatting
```

## Getting started

### 1. Python dependencies

```bash
cd python
pip install -r requirements.txt
playwright install chromium
```

### 2. Run the WPF app

```bash
dotnet run --project DarknessRise/DarknessRise.csproj
```

The SQLite database is created on first run at
`%LocalApplicationData%\LocalSocialMiner\lsm.db`.

### 3. Run the scraper standalone (optional)

```bash
# keyword search
python python/x_miner/cli.py --keyword "AI" --max-tweets 20

# any X URL: profile, search results, thread
python python/x_miner/cli.py --url "https://x.com/elonmusk" --max-tweets 10
python python/x_miner/cli.py --url "https://x.com/search?q=python&f=live" --max-tweets 30
```

## Host/scraper protocol

`PythonScraperService` launches the scraper as a child process and parses its
output:

- **Script path** — `python/x_miner/cli.py`, resolved relative to the WPF
  executable.
- **Interpreter** — `PYTHON_PATH` first, then `python` on `PATH`, then common
  install locations.
- **Logs** — written to **stderr** and streamed into the UI.
- **Data** — tweets are emitted to **stdout** as a single line prefixed with
  `TWEETS:` followed by a JSON array.

```
TWEETS:[{"author_name":"...","author_handle":"...","content":"...","timestamp":"2024-01-01T00:00:00+00:00","likes":10,"retweets":2,"replies":1,"bookmarks":0,"url":"https://x.com/...","scraped_at":"2024-01-01T12:00:00+00:00"}]
```

## Cookies and login

`browser-cookie3` tries to read existing X cookies from a local Chrome, Edge or
Firefox profile. If you are logged in there, the scraper can reach content that is
only visible to signed-in users; otherwise it still collects public tweets.

## Notes

- Intended for collecting public data only, and for respecting the target
  platform's terms of service.
- Keep request rates low so scraping does not put load on the target.

## License

MIT
