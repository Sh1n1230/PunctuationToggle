"""標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。"""

import io
import os
import sys
from typing import TextIO

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
TO_TOUTEN_KUTEN = {
    comma_period: touten_kuten for touten_kuten, comma_period in TO_COMMA_PERIOD.items()
}

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
        try:
            sys.stdout.write(USAGE)
            sys.stdout.flush()
        except OSError as error:
            discard_output(sys.stdout)
            write_error(f"使い方を表示できませんでした: {error}\n")
            return EXIT_FAILURE
        return EXIT_SUCCESS
    else:
        # UTF-8 でない引数（macOS や Linux で渡せる）は、sys.argv にサロゲートとして入っており
        # そのままでは UTF-8 で書き出せないため、ほかの実装と同じく U+FFFD に置き換えて表示する
        displayed = " ".join(
            os.fsencode(argument).decode("utf-8", errors="replace")
            for argument in arguments
        )
        write_error(f"不正な引数です: {displayed}\n\n{USAGE}")
        return EXIT_USAGE_ERROR

    # newline="" で、改行コードを変換せずにそのまま読み書きする
    reader = io.TextIOWrapper(sys.stdin.buffer, encoding="utf-8", newline="")
    writer = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", newline="")
    try:
        while chunk := reader.read(CHUNK_SIZE):
            writer.write(convert(chunk, replacements))
        writer.flush()
    except UnicodeDecodeError:
        write_error("入力を UTF-8 として読み込めませんでした。\n")
        return EXIT_FAILURE
    except OSError as error:
        discard_output(sys.stdout)
        write_error(f"変換に失敗しました: {error}\n")
        return EXIT_FAILURE
    return EXIT_SUCCESS


def discard_output(stream: TextIO) -> None:
    """書き込めなかった出力先を os.devnull に向ける。

    出力先に書き込めない（ディスクがいっぱい、パイプの読み手が先に終了したなど）場合、
    終了時の flush で再び失敗して「Exception ignored ...」と表示され、終了コードも 120 になるため
    (https://docs.python.org/3/library/signal.html#note-on-sigpipe)
    """
    devnull = os.open(os.devnull, os.O_WRONLY)
    os.dup2(devnull, stream.fileno())
    os.close(devnull)


def write_error(message: str) -> None:
    """標準エラー出力に書く。書き込めなくても、終了コードはほかの実装と同じにするため無視する。"""
    try:
        sys.stderr.write(message)
        sys.stderr.flush()
    except OSError:
        discard_output(sys.stderr)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
