#!/usr/bin/env python3
"""Doxygen input filter for C# positional records.

Doxygen's C# parser (as of 1.17) fails on positional records terminated with a
semicolon — `record X(...) : Base;` — and emits "Found ';' while parsing
initializer list", dropping the type from the docs entirely. This filter
rewrites such declarations on the fly (source files are never modified) into
`class X(...) : Base {}`, which Doxygen indexes fine.

Wire-up (Doxyfile):  FILTER_PATTERNS = *.cs="python scripts/doxygen-csharp-filter.py"
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

# `record Name(...)` or `record Name<T>(...)` optionally followed by a base
# list, terminated by `;`. Parameter lists never contain semicolons in this
# codebase, so a non-greedy scan up to the closing parenthesis is safe.
POSITIONAL_RECORD = re.compile(
    r"\brecord\s+(?P<name>\w+(?:<[^>()]+>)?)\s*\((?P<params>[^;]*?)\)"
    r"(?P<base>\s*:\s*[^;{]+?)?\s*;",
    re.DOTALL,
)


def transform(source: str) -> str:
    # Body-less positional records -> class with an empty body.
    source = POSITIONAL_RECORD.sub(
        lambda m: f"class {m.group('name')}({m.group('params')}){m.group('base') or ''} {{}}",
        source,
    )
    # Remaining records (with bodies) parse once the keyword reads as class.
    return re.sub(r"\brecord\b", "class", source)


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: doxygen-csharp-filter.py <file>", file=sys.stderr)
        return 1
    text = Path(sys.argv[1]).read_text(encoding="utf-8-sig")
    sys.stdout.reconfigure(encoding="utf-8")  # type: ignore[union-attr]
    sys.stdout.write(transform(text))
    return 0


if __name__ == "__main__":
    sys.exit(main())
