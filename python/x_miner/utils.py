"""Utilities for loading browser cookies and formatting output."""

from __future__ import annotations

import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def load_cookies_from_browser(domain: str = "x.com") -> list[dict[str, Any]]:
    """Try to load cookies from local Chrome/Edge/Firefox browser profiles.

    Uses browser-cookie3 if available; otherwise returns empty list.
    """
    try:
        import browser_cookie3  # type: ignore

        loaders = [
            ("chrome", browser_cookie3.chrome),
            ("chromium", browser_cookie3.chromium),
            ("edge", browser_cookie3.edge),
            ("firefox", browser_cookie3.firefox),
        ]
        for name, loader in loaders:
            try:
                jar = loader(domain_name=domain)
                cookies: list[dict[str, Any]] = []
                for c in jar:
                    cookies.append(
                        {
                            "name": c.name,
                            "value": c.value,
                            "domain": c.domain,
                            "path": c.path or "/",
                        }
                    )
                if cookies:
                    print(f"[cookie] loaded {len(cookies)} cookies from {name}", file=sys.stderr)
                    return cookies
            except Exception as exc:
                print(f"[cookie] {name} failed: {exc}", file=sys.stderr)
    except ImportError:
        print("[cookie] browser-cookie3 not installed, skipping", file=sys.stderr)
    return []


def emit_tweets(tweets: list[dict[str, Any]]) -> None:
    """Print tweets in the TWEETS:JSON format expected by the WPF host."""
    if tweets:
        print(f"TWEETS:{json.dumps(tweets, ensure_ascii=False)}", flush=True)
    else:
        print("TWEETS:[]", flush=True)
