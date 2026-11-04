"""変換コマンドのベンチマークの結果（hyperfine の JSON）を前回と比べ、Markdown の表を書き出す。

使い方: python3 benchmark_compare.py <今回の JSON> <前回の JSON>
  前回の JSON が無いときは、今回の平均だけを表にする。共有のランナーは性能がばらつくため、
  変化率は目安として表示するだけで、失敗の判定には使わない（終了コードは常に 0）。
"""

import json
import sys
from pathlib import Path

# 秒をミリ秒にする
MILLISECONDS_PER_SECOND = 1000


def load_means(path: Path) -> dict[str, float]:
    """実装の名前から平均の実行時間（秒）への対応を返す。"""
    data = json.loads(path.read_text(encoding="utf-8"))
    return {result["command"]: float(result["mean"]) for result in data["results"]}


def format_table(current: dict[str, float], previous: dict[str, float] | None) -> str:
    lines = ["| 実装 | 今回の平均 | 前回の平均 | 変化 |", "|---|---:|---:|---:|"]
    for name, mean in current.items():
        previous_mean = previous.get(name) if previous is not None else None
        if previous_mean is None or previous_mean == 0:
            lines.append(
                f"| {name} | {mean * MILLISECONDS_PER_SECOND:.2f} ms | - | - |"
            )
            continue
        change = (mean - previous_mean) / previous_mean * 100
        lines.append(
            f"| {name} | {mean * MILLISECONDS_PER_SECOND:.2f} ms"
            f" | {previous_mean * MILLISECONDS_PER_SECOND:.2f} ms | {change:+.1f}% |"
        )
    return "\n".join(lines) + "\n"


def main(arguments: list[str]) -> int:
    if len(arguments) != 2:
        sys.stderr.write("使い方: benchmark_compare.py <今回の JSON> <前回の JSON>\n")
        return 2
    current_path, previous_path = (Path(argument) for argument in arguments)
    previous = load_means(previous_path) if previous_path.is_file() else None
    sys.stdout.write(format_table(load_means(current_path), previous))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
