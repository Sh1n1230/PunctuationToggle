"""Cobertura 形式のカバレッジ（coverlet が出力する）を、ファイルごとの Markdown の表にする。

CI で、Actions の実行結果のサマリーに書き出すために使う。
使い方: python3 cobertura_summary.py <coverage.cobertura.xml>
"""

import sys
from pathlib import Path
from xml.etree import ElementTree


def main(arguments: list[str]) -> int:
    if len(arguments) != 1:
        print("使い方: cobertura_summary.py <coverage.cobertura.xml>", file=sys.stderr)
        return 2

    root = ElementTree.parse(arguments[0]).getroot()
    # 同じファイルのクラス（入れ子のクラスやラムダの生成クラス）をまとめて、ファイルごとの行の数を数える
    covered_lines: dict[str, set[int]] = {}
    all_lines: dict[str, set[int]] = {}
    for class_element in root.iter("class"):
        filename = Path(class_element.get("filename", "")).name
        for line in class_element.iter("line"):
            number = int(line.get("number", "0"))
            all_lines.setdefault(filename, set()).add(number)
            if int(line.get("hits", "0")) > 0:
                covered_lines.setdefault(filename, set()).add(number)

    print("| ファイル | 行 | 実行された行 | 行のカバレッジ |")
    print("|---|---:|---:|---:|")
    total = covered = 0
    for filename in sorted(all_lines):
        line_count = len(all_lines[filename])
        covered_count = len(covered_lines.get(filename, set()))
        total += line_count
        covered += covered_count
        print(
            f"| {filename} | {line_count} | {covered_count} | {percentage(covered_count, line_count)} |"
        )
    print(f"| **合計** | {total} | {covered} | {percentage(covered, total)} |")
    return 0


def percentage(covered: int, total: int) -> str:
    return f"{100 * covered / total:.1f}%" if total else "-"


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
