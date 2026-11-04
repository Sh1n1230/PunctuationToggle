"""入力待ちの Ctrl+C で、内部のトレースバックなどを表示せずに終了することを確認する。

使い方: python3 tests/test_sigint.py [実行コマンド]
  実行コマンドを省略すると Python 実装を確かめる（例: python3 tests/test_sigint.py bin/punctuation_converter_rust）。
  `tests/run_tests.sh` が、ビルドされているすべての実装に対して実行する。
"""

import os
import shlex
import signal
import subprocess
import sys
import time
import unittest
from pathlib import Path
from threading import Thread, Timer
from typing import IO

DEFAULT_COMMAND = [
    sys.executable,
    str(Path(__file__).resolve().parents[1] / "punctuation_converter.py"),
]

# テストの対象のコマンド。__main__ で引数から設定する
command = DEFAULT_COMMAND

# 起動を待つ時間の上限（秒）。CI の遅いランナーでも足りる長さ
STARTUP_TIMEOUT_SECONDS = 10

# 入力に流すデータ。どの実装の読み込みの単位（64 KiB）や書き出しのバッファより大きくし、
# 実装の内部の大きさに依存せずに、何かしらの出力が出るようにする
INPUT_TEXT = b"a" * (1 << 20)

# Ctrl+C で終了したときに受け付ける終了コード。シグナルによる終了（Popen では負の値）と、
# シグナルを受けてから 128 + シグナル番号で終了する実行環境（.NET など）の両方を、同じ「中断」として扱う
INTERRUPTED_RETURN_CODES = (-signal.SIGINT, 128 + signal.SIGINT)


@unittest.skipUnless(os.name == "posix", "POSIX の SIGINT による終了を確認する")
class SigintTest(unittest.TestCase):
    def test_interrupt_while_waiting_for_input(self) -> None:
        with subprocess.Popen(
            command,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        ) as process:
            assert process.stdin is not None and process.stdout is not None
            timeout = Timer(STARTUP_TIMEOUT_SECONDS * 2, process.kill)
            timeout.start()
            # 入力は閉じずに書き続けられるよう、別のスレッドで書く（パイプが詰まっても止まらない）
            writer = Thread(
                target=write_ignoring_errors,
                args=(process.stdin, INPUT_TEXT),
                daemon=True,
            )
            writer.start()
            try:
                # 出力が出始めてから割り込むことで、起動の途中（シグナルの設定の前）への送信を避ける
                first_byte = process.stdout.read(1)
                self.assertEqual(first_byte, b"a", "出力が出る前に終了しました")
                time.sleep(0.1)
                process.send_signal(signal.SIGINT)
                output, errors = process.communicate(timeout=STARTUP_TIMEOUT_SECONDS)
            except BaseException:
                process.kill()
                process.communicate()
                raise
            finally:
                timeout.cancel()
        self.assertIn(process.returncode, INTERRUPTED_RETURN_CODES)
        # 書き出し済みの変換結果のほかに、メッセージなどが混ざらない
        self.assertEqual(output, b"a" * len(output))
        self.assertEqual(errors, b"")


def write_ignoring_errors(stream: IO[bytes], data: bytes) -> None:
    try:
        stream.write(data)
        stream.flush()
    except (OSError, ValueError):
        # 割り込みで相手が終了すると書き込みに失敗するが、テストの結果とは関係ない
        pass


if __name__ == "__main__":
    if len(sys.argv) > 1:
        command = shlex.split(sys.argv[1])
    unittest.main(argv=sys.argv[:1])
