"""リポジトリの Markdown の、リポジトリ内へのリンク（ファイルと見出しのアンカー）が切れていないかを確かめる。

使い方: python3 .github/scripts/check_markdown_links.py
  git ls-files の *.md を調べ、切れたリンクがあれば表示して終了コード 1 で終わる。CI の「コーディング規約」ジョブが実行する。
  外部のサイトへのリンク（http・https・mailto）は、サイトの一時的な失敗で CI が失敗しないよう、確かめない。
  外部のツールを使わず標準ライブラリだけで動かし、版の固定を要らなくしている。
"""

import re
import subprocess
import sys
import unicodedata
from pathlib import Path
from urllib.parse import unquote

REPOSITORY_ROOT = Path(__file__).resolve().parents[2]

# [文字](リンク先) と ![画像](リンク先)。リンク先の後ろの "タイトル" は除く
LINK_PATTERN = re.compile(r"!?\[[^\]]*\]\(\s*<?([^)\s>]+)>?(?:\s+\"[^\"]*\")?\s*\)")
HEADING_PATTERN = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")
FENCE_PATTERN = re.compile(r"^\s*(```|~~~)")
EXTERNAL_PREFIXES = ("http://", "https://", "mailto:")


def github_anchor(heading: str) -> str:
    """GitHub が見出しに付けるアンカー。小文字にし、文字・数字・空白・- ・_ 以外を除き、空白を - にする。"""
    text = re.sub(r"`([^`]*)`", r"\1", heading)  # インラインのコード
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)  # リンク
    kept = []
    for character in text.lower():
        category = unicodedata.category(character)
        if category[0] in "LNM" or character in "-_ ":
            kept.append(character)
    return "".join(kept).replace(" ", "-")


def anchors_of(path: Path) -> set[str]:
    anchors: set[str] = set()
    counts: dict[str, int] = {}
    in_fence = False
    for line in path.read_text(encoding="utf-8").splitlines():
        if FENCE_PATTERN.match(line):
            in_fence = not in_fence
            continue
        match = HEADING_PATTERN.match(line)
        if in_fence or not match:
            continue
        anchor = github_anchor(match.group(2))
        # 同じアンカーの見出しが続くと、GitHub は -1・-2 を付ける
        count = counts.get(anchor, 0)
        counts[anchor] = count + 1
        anchors.add(anchor if count == 0 else f"{anchor}-{count}")
    return anchors


def links_of(path: Path) -> list[tuple[int, str]]:
    links: list[tuple[int, str]] = []
    in_fence = False
    for number, line in enumerate(
        path.read_text(encoding="utf-8").splitlines(), start=1
    ):
        if FENCE_PATTERN.match(line):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        # インラインのコードの中はリンクではない
        line = re.sub(r"`[^`]*`", "", line)
        links.extend((number, target) for target in LINK_PATTERN.findall(line))
    return links


def check(markdown_files: list[Path]) -> list[str]:
    problems = []
    anchor_cache: dict[Path, set[str]] = {}
    for markdown in markdown_files:
        for number, target in links_of(markdown):
            if target.startswith(EXTERNAL_PREFIXES):
                continue
            path_part, _, fragment = target.partition("#")
            linked = (
                (markdown.parent / unquote(path_part)).resolve()
                if path_part
                else markdown
            )
            location = f"{markdown.relative_to(REPOSITORY_ROOT)}:{number}"
            if not linked.exists():
                problems.append(f"{location}: リンク先のファイルがありません: {target}")
                continue
            if fragment and linked.suffix == ".md":
                anchors = anchor_cache.setdefault(linked, anchors_of(linked))
                if unquote(fragment).lower() not in anchors:
                    problems.append(
                        f"{location}: リンク先の見出しがありません: {target}"
                    )
    return problems


def main() -> int:
    listed = subprocess.run(
        ["git", "ls-files", "*.md"],
        cwd=REPOSITORY_ROOT,
        capture_output=True,
        text=True,
        check=True,
    ).stdout.split()
    problems = check([REPOSITORY_ROOT / name for name in listed])
    for problem in problems:
        print(problem, file=sys.stderr)
    if problems:
        print(f"切れたリンクが {len(problems)} 件あります。", file=sys.stderr)
        return 1
    print(f"{len(listed)} 個の Markdown のリポジトリ内へのリンクを確かめました。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
