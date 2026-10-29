#!/usr/bin/env python3
"""Validate documentation links, required files, and absence of emojis."""

from __future__ import annotations

import re
import sys
import unicodedata
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parent.parent.parent
LOCAL = ROOT / ".local"
LINK_PATTERN = re.compile(r"\[[^\]]*\]\(([^)]+)\)")

REQUIRED_FILES = [
    "README.md",
    "CONTRIBUTING.md",
    "LICENSE",
    "CODE_OF_CONDUCT.md",
    "SECURITY.md",
    "SUPPORT.md",
    "CHANGELOG.md",
    "docs/architecture-overview.md",
    "docs/getting-started.md",
    "docs/security-and-configuration.md",
    "docs/troubleshooting.md",
    "docs/usage-guide.md",
]

EXCLUDED_DIR_PARTS = {
    ".git",
    ".local",
    "node_modules",
    "bin",
    "obj",
    ".nuget",
    "artifacts",
}


def is_decorative_symbol(ch: str) -> bool:
    code = ord(ch)
    # Exclude standard ASCII
    if code < 128:
        return False

    cat = unicodedata.category(ch)
    # 'So' is Symbol, other (covers emojis, dingbats, box-drawing characters)
    if cat == "So":
        return True

    # Standard Unicode emoji and decorative ranges
    if 0x1F000 <= code <= 0x1FAFF:
        return True
    if 0x2600 <= code <= 0x27BF:
        return True
    if 0x2B00 <= code <= 0x2BFF:
        return True
    if 0xFE00 <= code <= 0xFE0F:
        return True

    return False


def relative_links(path: Path):
    text = path.read_text(encoding="utf-8")
    for match in LINK_PATTERN.finditer(text):
        target = match.group(1).strip()
        if target.startswith(("http://", "https://", "mailto:", "urn:", "#", "conversation://")):
            continue
        if " " in target and not target.startswith("<"):
            target = target.split(" ", 1)[0]
        target = unquote(target.split("#", 1)[0])
        if target:
            yield match.group(0), target


def check_required_files() -> list[str]:
    failures: list[str] = []
    for required in REQUIRED_FILES:
        if not (ROOT / required).exists():
            failures.append(f"missing required file: {required}")
    return failures


def get_scan_files(extensions: tuple[str, ...]) -> list[Path]:
    return [
        path
        for path in sorted(ROOT.rglob("*"))
        if path.suffix in extensions and not any(part in EXCLUDED_DIR_PARTS for part in path.parts)
    ]


def check_markdown_links() -> list[str]:
    failures: list[str] = []
    markdown_files = get_scan_files((".md",))

    for path in markdown_files:
        for source, target in relative_links(path):
            resolved = (path.parent / target).resolve()
            rel = path.relative_to(ROOT)
            if resolved.is_relative_to(LOCAL):
                failures.append(f"{rel}: public document links into private workspace {source}")
            elif not resolved.exists():
                failures.append(f"{rel}: broken link {source} -> {target}")

    return failures


def check_no_emojis() -> list[str]:
    failures: list[str] = []
    scan_files = get_scan_files((".md", ".yml", ".yaml"))

    for path in scan_files:
        rel = path.relative_to(ROOT)
        lines = path.read_text(encoding="utf-8").splitlines()
        for line_no, line in enumerate(lines, 1):
            for col_no, ch in enumerate(line, 1):
                if is_decorative_symbol(ch):
                    char_name = unicodedata.name(ch, "UNKNOWN")
                    failures.append(
                        f"{rel}:{line_no}:{col_no}: decorative symbol or emoji {ch!r} "
                        f"(U+{ord(ch):04X}, {char_name})"
                    )

    return failures


def main() -> int:
    failures: list[str] = []

    failures.extend(check_required_files())
    failures.extend(check_markdown_links())
    failures.extend(check_no_emojis())

    if failures:
        print("Documentation validation failed:", file=sys.stderr)
        for failure in failures:
            print(f"  - {failure}", file=sys.stderr)
        return 1

    markdown_count = len(get_scan_files((".md",)))
    yaml_count = len(get_scan_files((".yml", ".yaml")))
    print(
        f"Validation passed: {markdown_count} Markdown and {yaml_count} YAML files checked."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
