"""check_markdown_links.py のテスト。使い方: python3 -m unittest discover -s .github/scripts/tests"""

import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_markdown_links


class GithubAnchorTest(unittest.TestCase):
    def test_日本語と英語の見出しのアンカー(self) -> None:
        self.assertEqual(
            check_markdown_links.github_anchor("Windows での使い方"),
            "windows-での使い方",
        )
        self.assertEqual(
            check_markdown_links.github_anchor("1つの実装だけを使う"),
            "1つの実装だけを使う",
        )
        self.assertEqual(
            check_markdown_links.github_anchor("「送信しない」ことの確かめ方"),
            "送信しないことの確かめ方",
        )
        self.assertEqual(
            check_markdown_links.github_anchor("`make test` の説明"), "make-test-の説明"
        )


class CheckTest(unittest.TestCase):
    def test_切れたファイルと見出しを見つけ_正しいリンクは通す(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            (root / "docs").mkdir()
            (root / "docs" / "guide.md").write_text(
                "# 使い方\n\n## 使い方\n", encoding="utf-8"
            )
            readme = root / "README.md"
            readme.write_text(
                "[ok](docs/guide.md#使い方) [ok2](docs/guide.md#使い方-1) [self](#見出し)\n"
                "[ng-file](docs/missing.md) [ng-anchor](docs/guide.md#無い見出し)\n"
                "[ext](https://example.com/#x) `[code](not-a-link.md)`\n\n# 見出し\n",
                encoding="utf-8",
            )
            original = check_markdown_links.REPOSITORY_ROOT
            check_markdown_links.REPOSITORY_ROOT = root
            try:
                problems = check_markdown_links.check(
                    [readme, root / "docs" / "guide.md"]
                )
            finally:
                check_markdown_links.REPOSITORY_ROOT = original
        self.assertEqual(len(problems), 2, problems)
        self.assertIn("docs/missing.md", problems[0])
        self.assertIn("無い見出し", problems[1])


if __name__ == "__main__":
    unittest.main()
