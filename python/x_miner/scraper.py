"""Playwright-based X (Twitter) scraper.

Scrapes public X profile or search pages and extracts tweet data.
Does not require authentication for public content.
"""

from __future__ import annotations

import re
import sys
from datetime import datetime
from typing import Any

from .utils import emit_tweets, now_iso

try:
    from playwright.sync_api import TimeoutError as PwTimeout
    from playwright.sync_api import sync_playwright
except ImportError:
    print("[error] playwright not installed. Run: pip install playwright && playwright install chromium", file=sys.stderr)
    sys.exit(1)


def _parse_count(text: str) -> int:
    """Parse a count string like '1.2K' or '3.4M' to integer."""
    text = text.strip().replace(",", "")
    if not text:
        return 0
    multiplier = 1
    if text.endswith(("K", "k")):
        multiplier = 1_000
        text = text[:-1]
    elif text.endswith(("M", "m")):
        multiplier = 1_000_000
        text = text[:-1]
    elif text.endswith(("B", "b")):
        multiplier = 1_000_000_000
        text = text[:-1]
    try:
        return int(float(text) * multiplier)
    except ValueError:
        return 0


def _extract_tweets_from_page(page, max_tweets: int) -> list[dict[str, Any]]:
    """Extract tweet cards from the current page using DOM selectors."""
    tweets: list[dict[str, Any]] = []
    seen_ids: set[str] = set()

    page.wait_for_selector('article[data-testid="tweet"]', timeout=15000)

    while len(tweets) < max_tweets:
        cards = page.query_selector_all('article[data-testid="tweet"]')
        for card in cards:
            if len(tweets) >= max_tweets:
                break
            try:
                tweet = _parse_tweet_card(card)
                if not tweet or tweet.get("url") in seen_ids:
                    continue
                seen_ids.add(tweet["url"])
                tweets.append(tweet)
                print(f"[tweet] {len(tweets)}/{max_tweets}: {tweet['content'][:60]}...", file=sys.stderr)
            except Exception as exc:
                print(f"[warn] parse card failed: {exc}", file=sys.stderr)

        if len(tweets) >= max_tweets:
            break

        page.mouse.wheel(0, 3000)
        page.wait_for_timeout(1200)

    return tweets[:max_tweets]


def _parse_tweet_card(card) -> dict[str, Any] | None:
    """Parse a single tweet article element into a tweet dict."""
    link_el = card.query_selector('a[href*="/status/"]')
    if not link_el:
        return None
    href = link_el.get_attribute("href") or ""
    url = f"https://x.com{href.split('?')[0]}"
    author_handle = url.split("/")[3] if "/status/" in url else ""

    author_name = ""
    name_el = card.query_selector('div[data-testid="User-Name"]')
    if name_el:
        spans = name_el.query_selector_all("span")
        if spans:
            author_name = spans[0].inner_text()

    content = ""
    content_el = card.query_selector('div[data-testid="tweetText"]')
    if content_el:
        content = content_el.inner_text()

    timestamp = ""
    time_el = card.query_selector("time")
    if time_el:
        ts = time_el.get_attribute("datetime") or ""
        timestamp = ts.replace("Z", "+00:00")

    def _stat(data_testid: str) -> int:
        el = card.query_selector(f'div[data-testid="{data_testid}"]')
        if not el:
            return 0
        return _parse_count(el.inner_text())

    return {
        "author_name": author_name,
        "author_handle": author_handle,
        "content": content,
        "timestamp": timestamp,
        "likes": _stat("like"),
        "retweets": _stat("retweet"),
        "replies": _stat("reply"),
        "bookmarks": _stat("bookmark"),
        "url": url,
        "scraped_at": now_iso(),
    }


def scrape_keyword(keyword: str, max_tweets: int = 20) -> list[dict[str, Any]]:
    """Scrape tweets matching a keyword on X search."""
    query = keyword.strip().replace(" ", "%20")
    url = f"https://x.com/search?q={query}&src=typed_query&f=live"
    return _scrape_url(url, max_tweets)


def scrape_url(target_url: str, max_tweets: int = 20) -> list[dict[str, Any]]:
    """Scrape tweets from a given X URL (profile, search, or thread)."""
    return _scrape_url(target_url, max_tweets)


def _scrape_url(url: str, max_tweets: int) -> list[dict[str, Any]]:
    """Launch Playwright, navigate to url, extract tweets."""
    print(f"[info] scraping {url} (max={max_tweets})", file=sys.stderr)

    with sync_playwright() as pw:
        browser = pw.chromium.launch(headless=True)
        context = browser.new_context(
            user_agent=(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
                "AppleWebKit/537.36 (KHTML, like Gecko) "
                "Chrome/124.0.0.0 Safari/537.36"
            ),
            viewport={"width": 1280, "height": 800},
        )
        page = context.new_page()

        try:
            page.goto(url, wait_until="domcontentloaded", timeout=30000)
            print(f"[info] page loaded, title={page.title()}", file=sys.stderr)
            tweets = _extract_tweets_from_page(page, max_tweets)
        except PwTimeout:
            print("[warn] page load timeout, trying partial extract", file=sys.stderr)
            tweets = []
        except Exception as exc:
            print(f"[error] scrape failed: {exc}", file=sys.stderr)
            tweets = []
        finally:
            browser.close()

    return tweets


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser()
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--keyword", type=str)
    group.add_argument("--url", type=str)
    parser.add_argument("--max-tweets", type=int, default=20)
    args = parser.parse_args()

    if args.keyword:
        results = scrape_keyword(args.keyword, args.max_tweets)
    else:
        results = scrape_url(args.url, args.max_tweets)

    emit_tweets(results)
