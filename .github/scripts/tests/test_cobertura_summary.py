"""cobertura_summary.py のテスト。使い方: python3 -m unittest discover -s .github/scripts/tests"""

import sys
import unittest
from pathlib import Path
from xml.etree import ElementTree

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import cobertura_summary

REPORT = """<coverage><packages><package><classes>
<class filename="Core/NativeMethods.cs"><lines><line number="1" hits="1"/><line number="2" hits="0"/></lines></class>
<class filename="App/NativeMethods.cs"><lines><line number="1" hits="0"/></lines></class>
<class filename="Core/TriggerKey.cs"><lines><line number="5" hits="2"/></lines></class>
<class filename="Core/TriggerKey.cs"><lines><line number="6" hits="0"/></lines></class>
</classes></package></packages></coverage>"""


class CoberturaSummaryTest(unittest.TestCase):
    def test_別のフォルダーの同じ名前のファイルを別に数える(self) -> None:
        covered, all_lines = cobertura_summary.count_lines(
            ElementTree.fromstring(REPORT)
        )
        self.assertEqual(all_lines["Core/NativeMethods.cs"], {1, 2})
        self.assertEqual(all_lines["App/NativeMethods.cs"], {1})
        self.assertEqual(covered["Core/NativeMethods.cs"], {1})
        self.assertNotIn("App/NativeMethods.cs", covered)

    def test_同じファイルの複数のクラスはまとめる(self) -> None:
        _, all_lines = cobertura_summary.count_lines(ElementTree.fromstring(REPORT))
        self.assertEqual(all_lines["Core/TriggerKey.cs"], {5, 6})

    def test_名前が重なるときだけパスを表示する(self) -> None:
        names = cobertura_summary.display_names(
            ["App/NativeMethods.cs", "Core/NativeMethods.cs", "Core/TriggerKey.cs"]
        )
        self.assertEqual(names["App/NativeMethods.cs"], "App/NativeMethods.cs")
        self.assertEqual(names["Core/TriggerKey.cs"], "TriggerKey.cs")


if __name__ == "__main__":
    unittest.main()
