"""ランダムに作った入力で、各言語の実装が同じ仕様を満たすかを確かめる。

tests/cases/ の固定のケースでは狙いにくい組み合わせ（改行の種類・全角半角・絵文字・BOM の混在、
読み込みの区切りをまたぐ長さ）を、毎回違う入力で確かめる。確かめる性質は次のとおり。

- 出力が、句読点だけを置き換えた期待どおりのバイト列になる（実装どうしも一致する）
- 変換は冪等（変換した結果をもう一度変換しても変わらない）
- 「、。」→「，．」→「、。」と戻すと、逆変換だけをした結果と同じになる
- UTF-8 として正しくないバイトを混ぜると、終了コード 1 になる

失敗したときは、再現に使う乱数の種と入力を表示する。固定のケースにしたい場合は、
その入力を tests/cases/ に追加する。

使い方: python3 tests/run_random_tests.py [実装名...]
  RANDOM_SEED=<整数> で乱数の種を、RANDOM_CASES=<件数> でケースの数を指定できる。
  ビルドされていない実装は SKIP になる。REQUIRE_ALL=1 では SKIP も失敗にする。
"""

import os
import random
import subprocess
import sys
from pathlib import Path

CONVERTERS_DIR = Path(__file__).resolve().parent.parent

EXIT_SUCCESS = 0
EXIT_FAILURE = 1

DEFAULT_CASE_COUNT = 100

# 入力に使う部品。句読点の両方の組、似た文字（全角の英数字、半角の「,.」、句読点に似た記号）、
# 改行の3種類、2〜4バイトの文字、BOM を混ぜる
FRAGMENTS = [
    "、",
    "。",
    "，",
    "．",
    ",",
    ".",
    "｡",
    "､",
    "a",
    "Ａ",
    "あ",
    "é",
    "😀",
    "𠮷",
    "\n",
    "\r\n",
    "\r",
    "﻿",
    " ",
    "\t",
]

# 長い入力の文字数の上限。実装は 64 KiB ずつ読むので、区切りをまたぐ長さも作る
MAXIMUM_LENGTH = 70_000

# UTF-8 として正しくないバイト列
INVALID_SEQUENCES = [
    b"\xff",
    b"\x80",
    b"\xc0\xaf",
    b"\xed\xa0\x80",
    b"\xf4\x90\x80\x80",
]

TO_COMMA_PERIOD = str.maketrans({"、": "，", "。": "．"})
TO_TOUTEN_KUTEN = str.maketrans({"，": "、", "．": "。"})


def executable(path: Path) -> list[str] | None:
    """実行ファイルがあれば、それを実行するコマンド。Windows では .exe の付いたファイルを探す。"""
    if os.name == "nt":
        path = path.with_name(path.name + ".exe")
    return [str(path)] if path.is_file() and os.access(path, os.X_OK) else None


def command_for(name: str) -> list[str] | None:
    """実装名から実行コマンドを求める。実行できない場合は None。tests/run_tests.sh の command_for と同じ。"""
    if name == "csharp-dotnet":
        return executable(CONVERTERS_DIR / "bin" / "dotnet" / "PunctuationConverter")
    if name == "python":
        path = CONVERTERS_DIR / "punctuation_converter.py"
        return [sys.executable, str(path)] if path.is_file() else None
    if name == "rust":
        return executable(CONVERTERS_DIR / "bin" / "punctuation_converter_rust")
    return None


ALL_IMPLEMENTATIONS = ["csharp-dotnet", "python", "rust"]


def random_text(generator: random.Random) -> str:
    # 短い入力を多めに、ときどき読み込みの区切りをまたぐ長い入力を作る
    length = generator.choice(
        [generator.randint(0, 50), generator.randint(0, MAXIMUM_LENGTH)]
    )
    return "".join(generator.choices(FRAGMENTS, k=length))


def run(command: list[str], arguments: list[str], data: bytes) -> tuple[int, bytes]:
    result = subprocess.run(
        command + arguments, input=data, capture_output=True, check=False
    )
    return result.returncode, result.stdout


def check_implementation(command: list[str], generator: random.Random) -> list[str]:
    """1つの実装を確かめ、失敗の説明の一覧を返す。"""
    failures = []
    for _ in range(case_count()):
        text = random_text(generator)
        data = text.encode("utf-8")
        expected_forward = text.translate(TO_COMMA_PERIOD).encode("utf-8")
        expected_reverse = text.translate(TO_TOUTEN_KUTEN).encode("utf-8")

        status, forward = run(command, [], data)
        if status != EXIT_SUCCESS or forward != expected_forward:
            failures.append(
                f"変換の結果が期待と異なります（終了コード {status}）: {data!r:.200}"
            )
            continue
        status, reverse = run(command, ["--reverse"], data)
        if status != EXIT_SUCCESS or reverse != expected_reverse:
            failures.append(
                f"逆変換の結果が期待と異なります（終了コード {status}）: {data!r:.200}"
            )
            continue

        _, twice = run(command, [], forward)
        if twice != forward:
            failures.append(f"変換が冪等ではありません: {data!r:.200}")
        _, round_trip = run(command, ["--reverse"], forward)
        if round_trip != expected_reverse:
            failures.append(
                f"変換して逆変換した結果が、逆変換だけの結果と異なります: {data!r:.200}"
            )

        # 正しい入力のどこかに、正しくないバイト列を差し込む
        position = generator.randint(0, len(data))
        invalid = (
            data[:position] + generator.choice(INVALID_SEQUENCES) + data[position:]
        )
        # 文字の途中に差し込んでも、元の文字の継続バイトが余るなどして、どこに差し込んでも正しくなくなる
        status, _ = run(command, [], invalid)
        if status != EXIT_FAILURE:
            failures.append(
                f"正しくない UTF-8 で終了コード {status} でした（1 を期待）: {invalid!r:.200}"
            )
    return failures


def case_count() -> int:
    return int(os.environ.get("RANDOM_CASES", DEFAULT_CASE_COUNT))


def main(arguments: list[str]) -> int:
    names = arguments or ALL_IMPLEMENTATIONS
    unknown = [name for name in names if name not in ALL_IMPLEMENTATIONS]
    if unknown:
        print(
            f"不明な実装です: {' '.join(unknown)}（{' '.join(ALL_IMPLEMENTATIONS)} のいずれかを指定してください）",
            file=sys.stderr,
        )
        return 2

    seed = int(os.environ.get("RANDOM_SEED", random.SystemRandom().randrange(2**32)))
    print(f"乱数の種: {seed}（RANDOM_SEED={seed} で同じ入力を再現できます）")

    failure_count = 0
    skip_count = 0
    for name in names:
        command = command_for(name)
        if command is None:
            print(f"SKIP  {name}（ビルドされていないか、実行環境がありません）")
            skip_count += 1
            continue
        # 実装ごとに同じ入力の列を使い、実装どうしの結果の違いも分かるようにする
        failures = check_implementation(command, random.Random(seed))
        for failure in failures[:10]:
            print(f"  FAIL: {failure}")
        print(f"{'FAIL' if failures else 'PASS'}  {name}")
        failure_count += len(failures)

    print()
    print(f"失敗: {failure_count} 件、スキップ: {skip_count} 件")
    require_all = os.environ.get("REQUIRE_ALL") == "1"
    return 1 if failure_count or (require_all and skip_count) else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
