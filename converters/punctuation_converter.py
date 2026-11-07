# SPDX-License-Identifier: MPL-2.0
# This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
# If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.

"""標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。"""

import codecs
import io
import os
import signal
import sys
from typing import BinaryIO, TextIO

USAGE = """\
使い方: punctuation_converter [オプション] < 入力 > 出力

標準入力の文章の句読点を変換して、標準出力に書き出します。
句読点以外の文字や改行コードは変更しません。入力は UTF-8 にしてください。

オプション:
  （なし）       「、」を「，」に、「。」を「．」に変換する
  -r, --reverse  「，」を「、」に、「．」を「。」に変換する
  -h, --help     この使い方を表示する
  --version      版を表示する

終了コード:
  0  成功
  1  読み書きに失敗した、または入力が UTF-8 として正しくない
  2  不正な引数
"""

# 版。リポジトリのアプリと同じ番号（Windows/Directory.Build.props の Version）にする。
# リリースのワークフローが一致を確かめる
VERSION = "1.3.0"

EXIT_SUCCESS = 0
EXIT_FAILURE = 1
EXIT_USAGE_ERROR = 2

TO_COMMA_PERIOD = {"、": "，", "。": "．"}
TO_TOUTEN_KUTEN = {
    comma_period: touten_kuten for touten_kuten, comma_period in TO_COMMA_PERIOD.items()
}

# 一度に読み込むバイト数
CHUNK_SIZE = 1 << 16


class InvalidUtf8Error(Exception):
    """入力が UTF-8 として正しくない。位置は3つの実装で同じ数え方にする。

    byte_position: 正しくないバイト列の最初のバイトが、入力の先頭から何バイト目か（1 から数える）
    line: そのバイトが何行目か（1 から数える）。行は LF（CRLF の LF を含む）で区切り、CR だけの改行は数えない
    """

    def __init__(self, byte_position: int, line: int) -> None:
        super().__init__(byte_position, line)
        self.byte_position = byte_position
        self.line = line

    def message(self) -> str:
        return (
            "入力を UTF-8 として読み込めませんでした"
            f"（{self.line} 行目、先頭から {self.byte_position} バイト目）。\n"
        )


def convert(text: str, replacements: dict[str, str]) -> str:
    # str.translate より str.replace を2回呼ぶ方が速い。64 Ki 文字の読み込み1回分で、
    # 合成データ（「、。」の繰り返し）は replace 57 µs・translate 2,871 µs、実文章
    # （benchmark/wikipedia_programming.txt の先頭）は 7.5 µs・1,249 µs（Python 3.13、Linux、timeit）。
    # translate は1文字ずつ表を引くが、replace は置き換える文字を高速に探して、まとめて複写する
    for source, target in replacements.items():
        text = text.replace(source, target)
    return text


def main(arguments: list[str]) -> int:
    # ほかの実装と同じく、Ctrl+C ではトレースバックを表示せずに終了する。
    signal.signal(signal.SIGINT, signal.SIG_DFL)

    # シェルの `>&-` などで標準入出力が閉じていると、Python は sys.stdout などを None にする。
    # Rust の標準ライブラリと同じく、閉じた出力先には書けたものとして扱い、閉じた入力は空として扱う。
    # 閉じた番号から順に os.devnull を開き直すので、開いたファイルはその番号（0・1・2）になる。
    # 終了まで使うため with では閉じない
    if sys.stdin is None:
        sys.stdin = open(os.devnull, encoding="utf-8")  # noqa: SIM115
    if sys.stdout is None:
        sys.stdout = open(os.devnull, "w", encoding="utf-8")  # noqa: SIM115
    if sys.stderr is None:
        sys.stderr = open(os.devnull, "w", encoding="utf-8")  # noqa: SIM115

    # Windows では標準出力・標準エラー出力の既定の文字コードが UTF-8 ではなく（cp932 など）、
    # 改行も CRLF に変わるため、使い方やエラーの表示もほかの実装と同じ UTF-8・LF に揃える
    # （テストなどで io.TextIOWrapper 以外に差し替えられている場合は、そのまま使う）
    for stream in (sys.stdout, sys.stderr):
        if isinstance(stream, io.TextIOWrapper):
            stream.reconfigure(encoding="utf-8", newline="\n")

    if arguments in ([], ["-r"], ["--reverse"]):
        replacements = TO_TOUTEN_KUTEN if arguments else TO_COMMA_PERIOD
    elif arguments in (["-h"], ["--help"], ["--version"]):
        text = (
            USAGE
            if arguments != ["--version"]
            else f"punctuation_converter {VERSION}\n"
        )
        try:
            sys.stdout.write(text)
            sys.stdout.flush()
        except OSError as error:
            discard_output(sys.stdout)
            write_error(f"表示できませんでした: {error}\n")
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

    # newline="" で、改行コードを変換せずにそのまま書き出す
    writer = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", newline="")
    try:
        convert_stream(sys.stdin.buffer, writer, replacements)
        writer.flush()
    except InvalidUtf8Error as error:
        write_error(error.message())
        return EXIT_FAILURE
    except BrokenPipeError:
        # 出力の読み手が先に終了した（`| head` など）ときは、ほかの実装と同じく黙って成功で終える
        discard_output(sys.stdout)
        return EXIT_SUCCESS
    except OSError as error:
        discard_output(sys.stdout)
        write_error(f"変換に失敗しました: {error}\n")
        return EXIT_FAILURE
    return EXIT_SUCCESS


def convert_stream(
    reader: BinaryIO, writer: TextIO, replacements: dict[str, str]
) -> None:
    """入力を一定のバイト数ずつ読み、変換して書き出す。

    正しくない UTF-8 の位置を示せるよう、テキストとしてではなくバイト列として読み、自分で復号する。
    """
    decoder = codecs.getincrementaldecoder("utf-8")()
    # 復号を終えたバイト数と、その中の LF の数
    decoded_bytes = 0
    line_feeds = 0
    while True:
        chunk = reader.read(CHUNK_SIZE)
        # 前回の読み込みの末尾で途中までしか読めていない文字のバイト列（復号器が持っている）
        pending = decoder.getstate()[0]
        try:
            text = decoder.decode(chunk, final=not chunk)
        except UnicodeDecodeError as error:
            # error.start は、持っていたバイト列とこの読み込みをつないだものの中の位置
            valid = (pending + chunk)[: error.start]
            raise InvalidUtf8Error(
                decoded_bytes + error.start + 1, line_feeds + valid.count(b"\n") + 1
            ) from None
        if not chunk:
            return
        complete = (pending + chunk)[
            : len(pending) + len(chunk) - len(decoder.getstate()[0])
        ]
        decoded_bytes += len(complete)
        line_feeds += complete.count(b"\n")
        writer.write(convert(text, replacements))


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
