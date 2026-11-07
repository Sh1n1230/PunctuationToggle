"""Cobertura 形式のカバレッジ（coverlet が出力する）を、ファイルごとの Markdown の表にする。

CI で、Actions の実行結果のサマリーに書き出すために使う。
使い方: python3 cobertura_summary.py <coverage.cobertura.xml>...
  テストのプロジェクトごとのレポートを複数渡すと、レポートごとに、対象のアセンブリの名前の見出しと表を書く。
"""

import sys
from pathlib import Path
from xml.etree import ElementTree


def main(arguments: list[str]) -> int:
    if not arguments:
        print(
            "使い方: cobertura_summary.py <coverage.cobertura.xml>...", file=sys.stderr
        )
        return 2

    for index, report in enumerate(arguments):
        if index > 0:
            print()
        root = ElementTree.parse(report).getroot()
        if len(arguments) > 1:
            print(f"#### {', '.join(package_names(root))}")
            print()
        print_table(root)
    return 0


def package_names(root: ElementTree.Element) -> list[str]:
    """レポートの対象のアセンブリ（coverlet の package）の名前。"""
    return sorted({package.get("name", "") for package in root.iter("package")})


def print_table(root: ElementTree.Element) -> None:
    covered_lines, all_lines = count_lines(root)
    names = display_names(sorted(all_lines))

    print("| ファイル | 行 | 実行された行 | 行のカバレッジ |")
    print("|---|---:|---:|---:|")
    total = covered = 0
    for filename in sorted(all_lines):
        line_count = len(all_lines[filename])
        covered_count = len(covered_lines.get(filename, set()))
        total += line_count
        covered += covered_count
        print(
            f"| {names[filename]} | {line_count} | {covered_count} | {percentage(covered_count, line_count)} |"
        )
    print(f"| **合計** | {total} | {covered} | {percentage(covered, total)} |")


def count_lines(
    root: ElementTree.Element,
) -> tuple[dict[str, set[int]], dict[str, set[int]]]:
    """ファイル（coverlet が書くソースのフォルダーからのパス）ごとに、実行された行とすべての行を返す。

    同じファイルのクラス（入れ子のクラスやラムダの生成クラス）はまとめる。別のフォルダーの同じ名前の
    ファイルは、パスが違うので別のファイルとして数える。
    """
    covered_lines: dict[str, set[int]] = {}
    all_lines: dict[str, set[int]] = {}
    for class_element in root.iter("class"):
        filename = Path(class_element.get("filename", "")).as_posix()
        for line in class_element.iter("line"):
            number = int(line.get("number", "0"))
            all_lines.setdefault(filename, set()).add(number)
            if int(line.get("hits", "0")) > 0:
                covered_lines.setdefault(filename, set()).add(number)
    return covered_lines, all_lines


def display_names(paths: list[str]) -> dict[str, str]:
    """表に出す名前。ふつうはファイル名だけにし、同じ名前のファイルが複数あるときだけパスを出す。"""
    counts: dict[str, int] = {}
    for path in paths:
        name = Path(path).name
        counts[name] = counts.get(name, 0) + 1
    return {
        path: path if counts[Path(path).name] > 1 else Path(path).name for path in paths
    }


def percentage(covered: int, total: int) -> str:
    return f"{100 * covered / total:.1f}%" if total else "-"


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
