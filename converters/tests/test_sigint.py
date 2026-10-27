"""入力待ちの Ctrl+C で、内部のトレースバックを表示しないことを確認する。"""

import os
import signal
import subprocess
import sys
import unittest
from pathlib import Path
from threading import Timer


@unittest.skipUnless(os.name == "posix", "POSIX の SIGINT による終了を確認する")
class SigintTest(unittest.TestCase):
    def test_interrupt_while_waiting_for_input(self):
        converter = Path(__file__).resolve().parents[1] / "punctuation_converter.py"
        with subprocess.Popen(
            [sys.executable, str(converter)],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        ) as process:
            timeout = Timer(5, process.kill)
            timeout.start()
            try:
                # 出力を確認してから割り込むことで、Python の起動途中への送信を避ける。
                text = b"a" * (1 << 16)
                process.stdin.write(text)
                process.stdin.flush()
                self.assertEqual(process.stdout.read(len(text)), text)
                process.send_signal(signal.SIGINT)
                output, errors = process.communicate(timeout=5)
            except BaseException:
                process.kill()
                process.communicate()
                raise
            finally:
                timeout.cancel()
        self.assertEqual(process.returncode, -signal.SIGINT)
        self.assertEqual(output, b"")
        self.assertEqual(errors, b"")


if __name__ == "__main__":
    unittest.main()
