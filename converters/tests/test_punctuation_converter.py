"""変換コマンドの Python 実装の main を、プロセスを起動せずに確かめる。

使い方: python3 tests/test_punctuation_converter.py（`make test` から実行する）
"""

import io
import os
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
        argument = os.fsdecode(b"\xff")
        status, _, errors = self.run_main([argument])
        self.assertEqual(status, punctuation_converter.EXIT_USAGE_ERROR)
        self.assertTrue(errors.startswith("不正な引数です: �\n"))

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


if __name__ == "__main__":
    unittest.main()
