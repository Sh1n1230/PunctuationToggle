"""標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。"""

import io
import sys

USAGE = """\
使い方: punctuation_converter [オプション] < 入力 > 出力

標準入力の文章の句読点を変換して、標準出力に書き出します。
句読点以外の文字や改行コードは変更しません。入力は UTF-8 にしてください。

オプション:
  （なし）       「、」を「，」に、「。」を「．」に変換する
  -r, --reverse  「，」を「、」に、「．」を「。」に変換する
  -h, --help     この使い方を表示する
"""

EXIT_SUCCESS = 0
EXIT_FAILURE = 1
EXIT_USAGE_ERROR = 2

TO_COMMA_PERIOD = {"、": "，", "。": "．"}
TO_TOUTEN_KUTEN = {comma_period: touten_kuten for touten_kuten, comma_period in TO_COMMA_PERIOD.items()}

# 一度に読み込む文字数
CHUNK_SIZE = 1 << 16


def convert(text: str, replacements: dict[str, str]) -> str:
    for source, target in replacements.items():
        text = text.replace(source, target)
    return text


def main(arguments: list[str]) -> int:
    # Windows では標準出力・標準エラー出力の既定の文字コードが UTF-8 ではなく（cp932 など）、
    # 改行も CRLF に変わるため、使い方やエラーの表示もほかの実装と同じ UTF-8・LF に揃える
    sys.stdout.reconfigure(encoding="utf-8", newline="\n")
    sys.stderr.reconfigure(encoding="utf-8", newline="\n")

    if arguments in ([], ["-r"], ["--reverse"]):
        replacements = TO_TOUTEN_KUTEN if arguments else TO_COMMA_PERIOD
    elif arguments in (["-h"], ["--help"]):
        sys.stdout.write(USAGE)
        return EXIT_SUCCESS
    else:
        sys.stderr.write(f"不正な引数です: {' '.join(arguments)}\n\n{USAGE}")
        return EXIT_USAGE_ERROR

    # newline="" で、改行コードを変換せずにそのまま読み書きする
    reader = io.TextIOWrapper(sys.stdin.buffer, encoding="utf-8", newline="")
    writer = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", newline="")
    try:
        while chunk := reader.read(CHUNK_SIZE):
            writer.write(convert(chunk, replacements))
        writer.flush()
    except UnicodeDecodeError:
        sys.stderr.write("入力を UTF-8 として読み込めませんでした。\n")
        return EXIT_FAILURE
    return EXIT_SUCCESS


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
