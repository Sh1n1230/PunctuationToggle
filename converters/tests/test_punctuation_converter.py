"""変換コマンドの Python 実装の main を、プロセスを起動せずに確かめる。

使い方: python3 tests/test_punctuation_converter.py（`make test` から実行する）
"""

import io
import sys
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

# 上でパスを足してから読み込む
import punctuation_converter


class FailingOutput(io.StringIO):
    """書き込むと必ず失敗する出力先（ディスクがいっぱいの場合などを模す）。"""

    def write(self, text: str) -> int:
        raise OSError("書き込めません")


class MainTest(unittest.TestCase):
    def run_main(self, arguments: list[str]) -> tuple[int, str, str]:
        output = io.StringIO()
        errors = io.StringIO()
        with (
            mock.patch.object(sys, "stdout", output),
            mock.patch.object(sys, "stderr", errors),
        ):
            status = punctuation_converter.main(arguments)
        return status, output.getvalue(), errors.getvalue()

    def test_使い方を表示して0を返す(self) -> None:
        for option in ("-h", "--help"):
            status, output, errors = self.run_main([option])
            self.assertEqual(status, punctuation_converter.EXIT_SUCCESS)
            self.assertEqual(output, punctuation_converter.USAGE)
            self.assertEqual(errors, "")

    def test_版を表示して0を返す(self) -> None:
        status, output, _ = self.run_main(["--version"])
        self.assertEqual(status, punctuation_converter.EXIT_SUCCESS)
        self.assertEqual(
            output, f"punctuation_converter {punctuation_converter.VERSION}\n"
        )

    def test_不正な引数では2を返し_標準エラー出力に使い方を書く(self) -> None:
        status, output, errors = self.run_main(["--unknown"])
        self.assertEqual(status, punctuation_converter.EXIT_USAGE_ERROR)
        self.assertEqual(output, "")
        self.assertEqual(
            errors, f"不正な引数です: --unknown\n\n{punctuation_converter.USAGE}"
        )

    def test_UTF8_でない引数でも例外にならず2を返す(self) -> None:
        # macOS や Linux では、UTF-8 でない引数はサロゲートとして sys.argv に入る
        # （os.fsdecode は Windows ではサロゲートにせず例外になるため、POSIX と同じ変換を直接行う）
        argument = b"\xff".decode("utf-8", "surrogateescape")
        status, _, errors = self.run_main([argument])
        self.assertEqual(status, punctuation_converter.EXIT_USAGE_ERROR)
        # Windows の os.fsencode はサロゲートを3バイトにするため、U+FFFD の数は OS によって違う
        self.assertRegex(errors, "^不正な引数です: �+\n")

    def test_使い方を書き出せないときは1を返す(self) -> None:
        errors = io.StringIO()
        with (
            mock.patch.object(sys, "stdout", FailingOutput()),
            mock.patch.object(sys, "stderr", errors),
            mock.patch.object(punctuation_converter, "discard_output"),
        ):
            status = punctuation_converter.main(["--help"])
        self.assertEqual(status, punctuation_converter.EXIT_FAILURE)
        self.assertTrue(errors.getvalue().startswith("表示できませんでした: "))

    def test_convert_は句読点だけを両方の向きに置き換える(self) -> None:
        text = "、。，．,.｡､︐︒﹐﹒𠮷😀\r\n"
        self.assertEqual(
            punctuation_converter.convert(text, punctuation_converter.TO_COMMA_PERIOD),
            "，．，．,.｡､︐︒﹐﹒𠮷😀\r\n",
        )
        self.assertEqual(
            punctuation_converter.convert(text, punctuation_converter.TO_TOUTEN_KUTEN),
            "、。、。,.｡､︐︒﹐﹒𠮷😀\r\n",
        )


class ConvertStreamTest(unittest.TestCase):
    def invalid_position(self, data: bytes, chunk_size: int) -> tuple[int, int]:
        writer = io.StringIO()
        with (
            mock.patch.object(punctuation_converter, "CHUNK_SIZE", chunk_size),
            self.assertRaises(punctuation_converter.InvalidUtf8Error) as raised,
        ):
            punctuation_converter.convert_stream(
                io.BytesIO(data), writer, punctuation_converter.TO_COMMA_PERIOD
            )
        return raised.exception.line, raised.exception.byte_position

    def test_正しくないUTF8の位置は読み込みの単位によらない(self) -> None:
        # 3行目の 7 バイト目（CR だけの改行は数えない）
        data = b"a\nb\r\nc\rd\xff"
        for chunk_size in (1, 2, 3, 64):
            with self.subTest(chunk_size=chunk_size):
                self.assertEqual(self.invalid_position(data, chunk_size), (3, 9))

    def test_読み込みの区切りで切れた文字が正しくない場合は_その文字の先頭の位置(
        self,
    ) -> None:
        data = "、".encode()[:2] + b"\xff"
        for chunk_size in (1, 2, 64):
            with self.subTest(chunk_size=chunk_size):
                self.assertEqual(self.invalid_position(data, chunk_size), (1, 1))

    def test_末尾で文字が切れたまま終わると_その文字の先頭の位置(self) -> None:
        self.assertEqual(self.invalid_position(b"a\n\xe3\x80", 64), (2, 3))


if __name__ == "__main__":
    unittest.main()
