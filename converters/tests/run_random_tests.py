"""ランダムに作った入力で、各言語の実装が同じ仕様を満たすかを確かめる。

tests/cases/ の固定のケースでは狙いにくい組み合わせ（改行の種類・全角半角・絵文字・BOM の混在、
読み込みの区切りをまたぐ長さ）を、毎回違う入力で確かめる。確かめる性質は次のとおり。

- 出力が、句読点だけを置き換えた期待どおりのバイト列になる（実装どうしも一致する）
- 変換は冪等（変換した結果をもう一度変換しても変わらない）
- 「、。」→「，．」→「、。」と戻すと、逆変換だけをした結果と同じになる
- 成功したときは標準エラー出力が空
- UTF-8 として正しくないバイトを混ぜると、変換でも逆変換でも、終了コード 1 と、最初の正しくないバイトの位置
  （Python の UTF-8 の復号が示す位置）を示すメッセージになる

失敗したときは、再現に使う乱数の種と何件目のケースかを表示し、入力と期待する出力を
tests/cases/ と同じ形（input.txt・to_comma_period.txt・to_touten_kuten.txt）で保存する。
固定のケースにしたい場合は、そのディレクトリーを tests/cases/ に移す。

使い方: python3 tests/run_random_tests.py [実装名...]
  RANDOM_SEED=<0 以上 2**32 未満の整数> で乱数の種を、RANDOM_CASES=<1 以上の整数> でケースの数を指定できる。
  RANDOM_FAILURES_DIR=<ディレクトリー> で失敗した入力の保存先を指定できる（既定は一時ディレクトリー）。
  ビルドされていない実装は SKIP になる。REQUIRE_ALL=1 では SKIP も失敗にする。
"""

import io
import os
import random
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

CONVERTERS_DIR = Path(__file__).resolve().parent.parent

EXIT_SUCCESS = 0
EXIT_FAILURE = 1
EXIT_USAGE_ERROR = 2


def invalid_utf8_message(data: bytes) -> bytes:
    """UTF-8 として正しくない入力のときに、3つの実装が標準エラー出力に書くメッセージ。

    位置は、正しくないバイト列の最初のバイトが何バイト目か（1 から）と、何行目か（LF で数える）。
    """
    try:
        data.decode("utf-8")
    except UnicodeDecodeError as error:
        line = data[: error.start].count(b"\n") + 1
        return (
            "入力を UTF-8 として読み込めませんでした"
            f"（{line} 行目、先頭から {error.start + 1} バイト目）。\n"
        ).encode()
    raise ValueError("UTF-8 として正しい入力です")


# 乱数の種の範囲（0 以上 2**32 未満）
SEED_LIMIT = 2**32

# 1回の実行の時間の上限（秒）。最大 70,000 文字の入力でも、遅い CI のランナーで十分に足りる長さ。
# 実装が止まらなくなっても、テスト全体が止まらないようにする
RUN_TIMEOUT_SECONDS = 30

# 失敗の説明に含める標準エラー出力の長さ
ERROR_EXCERPT_LENGTH = 300

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
    # 縦書き用・小字形の句読点（変換しない）
    "︐",
    "︑",
    "︒",
    "﹐",
    "﹑",
    "﹒",
    # 正しいが扱いを誤りやすい文字: NUL、非文字、最大の符号位置、サロゲートの前後
    "\x00",
    "\ufffe",
    "\uffff",
    "\U0010ffff",
    "\ud7ff",
    "\ue000",
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


@dataclass
class Result:
    status: int | None  # タイムアウトしたときは None
    output: bytes
    errors: bytes

    def describe(self) -> str:
        if self.status is None:
            return f"{RUN_TIMEOUT_SECONDS} 秒でタイムアウトしました"
        excerpt = self.errors[:ERROR_EXCERPT_LENGTH].decode("utf-8", errors="replace")
        return f"終了コード {self.status}、標準エラー出力「{excerpt}」"


def run(command: list[str], arguments: list[str], data: bytes) -> Result:
    try:
        result = subprocess.run(
            command + arguments,
            input=data,
            capture_output=True,
            check=False,
            timeout=RUN_TIMEOUT_SECONDS,
        )
    except subprocess.TimeoutExpired:
        return Result(None, b"", b"")
    return Result(result.returncode, result.stdout, result.stderr)


def succeeded_with(result: Result, expected: bytes) -> bool:
    return (
        result.status == EXIT_SUCCESS
        and result.output == expected
        and not result.errors
    )


@dataclass
class Settings:
    seed: int
    case_count: int
    failures_dir: Path


def save_case(
    settings: Settings, name: str, case_number: int, text_bytes: bytes
) -> Path:
    """失敗した入力を、tests/cases/ にそのまま移せる形で保存し、そのディレクトリーを返す。"""
    case_dir = settings.failures_dir / f"random_{name}_{settings.seed}_{case_number}"
    case_dir.mkdir(parents=True, exist_ok=True)
    (case_dir / "input.txt").write_bytes(text_bytes)
    try:
        text = text_bytes.decode("utf-8")
    except UnicodeDecodeError:
        # 正しくない入力は期待する出力が無い（終了コード 1 になることを確かめるケース）
        return case_dir
    (case_dir / "to_comma_period.txt").write_bytes(
        text.translate(TO_COMMA_PERIOD).encode()
    )
    (case_dir / "to_touten_kuten.txt").write_bytes(
        text.translate(TO_TOUTEN_KUTEN).encode()
    )
    return case_dir


def check_implementation(
    name: str, command: list[str], generator: random.Random, settings: Settings
) -> list[str]:
    """1つの実装を確かめ、失敗の説明の一覧を返す。"""
    failures = []
    for case_number in range(1, settings.case_count + 1):
        text = random_text(generator)
        data = text.encode("utf-8")
        expected_forward = text.translate(TO_COMMA_PERIOD).encode("utf-8")
        expected_reverse = text.translate(TO_TOUTEN_KUTEN).encode("utf-8")
        # 正しい入力のどこかに、正しくないバイト列を差し込む。乱数は確かめる内容によらず
        # 同じ順に引き、失敗しても以降のケースが同じ入力になるようにする
        position = generator.randint(0, len(data))
        invalid = (
            data[:position] + generator.choice(INVALID_SEQUENCES) + data[position:]
        )

        def fail(
            message: str,
            input_bytes: bytes,
            result: Result,
            case_number: int = case_number,
        ) -> None:
            saved = save_case(settings, name, case_number, input_bytes)
            failures.append(
                f"{case_number} 件目: {message}（{result.describe()}）。入力: {saved}"
            )

        result = run(command, [], data)
        if not succeeded_with(result, expected_forward):
            fail("変換の結果が期待と異なります", data, result)
            continue
        result = run(command, ["--reverse"], data)
        if not succeeded_with(result, expected_reverse):
            fail("逆変換の結果が期待と異なります", data, result)
            continue

        result = run(command, [], expected_forward)
        if not succeeded_with(result, expected_forward):
            fail("変換が冪等ではありません", data, result)
        result = run(command, ["--reverse"], expected_forward)
        if not succeeded_with(result, expected_reverse):
            fail("変換して逆変換した結果が、逆変換だけの結果と異なります", data, result)

        # 文字の途中に差し込んでも、元の文字の継続バイトが余るなどして、どこに差し込んでも正しくなくなる
        expected_message = invalid_utf8_message(invalid)
        for arguments in ([], ["--reverse"]):
            result = run(command, arguments, invalid)
            if result.status != EXIT_FAILURE or result.errors != expected_message:
                fail(
                    f"正しくない UTF-8 で、終了コード 1 と「{expected_message.decode().strip()}」を"
                    f"期待しました（引数 {arguments}）",
                    invalid,
                    result,
                )
    return failures


def parse_integer(name: str, minimum: int, limit: int | None) -> int | None:
    """環境変数の整数を読む。範囲外なら説明を表示して None を返す。"""
    value = os.environ[name]
    try:
        number = int(value)
    except ValueError:
        number = None
    if number is None or number < minimum or (limit is not None and number >= limit):
        upper = f"、{limit} 未満" if limit is not None else ""
        print(
            f"{name} には {minimum} 以上{upper}の整数を指定してください: {value}",
            file=sys.stderr,
        )
        return None
    return number


def read_settings() -> Settings | None:
    if "RANDOM_SEED" in os.environ:
        seed = parse_integer("RANDOM_SEED", 0, SEED_LIMIT)
        if seed is None:
            return None
    else:
        seed = random.SystemRandom().randrange(SEED_LIMIT)
    case_count = DEFAULT_CASE_COUNT
    if "RANDOM_CASES" in os.environ:
        parsed = parse_integer("RANDOM_CASES", 1, None)
        if parsed is None:
            return None
        case_count = parsed
    failures_dir = Path(
        os.environ.get("RANDOM_FAILURES_DIR")
        or tempfile.mkdtemp(prefix="punctuation_converter_random_")
    )
    return Settings(seed, case_count, failures_dir)


def main(arguments: list[str]) -> int:
    # Windows では標準出力の既定の文字コードが UTF-8 ではなく（cp1252 など）、日本語を書けないため
    for stream in (sys.stdout, sys.stderr):
        if isinstance(stream, io.TextIOWrapper):
            stream.reconfigure(encoding="utf-8")

    names = arguments or ALL_IMPLEMENTATIONS
    unknown = [name for name in names if name not in ALL_IMPLEMENTATIONS]
    if unknown:
        print(
            f"不明な実装です: {' '.join(unknown)}（{' '.join(ALL_IMPLEMENTATIONS)} のいずれかを指定してください）",
            file=sys.stderr,
        )
        return EXIT_USAGE_ERROR

    settings = read_settings()
    if settings is None:
        return EXIT_USAGE_ERROR
    seed = settings.seed
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
        failures = check_implementation(name, command, random.Random(seed), settings)
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
