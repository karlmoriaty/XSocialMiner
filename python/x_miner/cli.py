"""Command-line entry point for the X scraper.

Usage:
    python cli.py --keyword "python" --max-tweets 20
    python cli.py --url "https://x.com/elonmusk" --max-tweets 10
    python cli.py --url "https://x.com/search?q=ai&f=live" --max-tweets 30

Output format (parsed by the WPF host):
    Log lines -> stderr
    TWEETS:[{...}, {...}] -> stdout
"""

from __future__ import annotations

import argparse
import sys

from .scraper import scrape_keyword, scrape_url
from .utils import emit_tweets


def main() -> int:
    parser = argparse.ArgumentParser(prog="x_miner")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--keyword", type=str, help="Search keyword")
    group.add_argument("--url", type=str, help="Target X URL (profile, search, or thread)")
    parser.add_argument("--max-tweets", type=int, default=20, help="Max tweets to extract")
    args = parser.parse_args()

    if args.keyword:
        results = scrape_keyword(args.keyword, args.max_tweets)
    else:
        results = scrape_url(args.url, args.max_tweets)

    emit_tweets(results)
    return 0


if __name__ == "__main__":
    sys.exit(main())
