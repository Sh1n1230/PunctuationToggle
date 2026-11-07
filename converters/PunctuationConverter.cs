// SPDX-License-Identifier: MPL-2.0
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.

// 標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Unicode;

internal static class PunctuationConverter
{
    private const string Usage =
        "使い方: punctuation_converter [オプション] < 入力 > 出力\n" +
        "\n" +
        "標準入力の文章の句読点を変換して、標準出力に書き出します。\n" +
        "句読点以外の文字や改行コードは変更しません。入力は UTF-8 にしてください。\n" +
        "\n" +
        "オプション:\n" +
        "  （なし）       「、」を「，」に、「。」を「．」に変換する\n" +
        "  -r, --reverse  「，」を「、」に、「．」を「。」に変換する\n" +
        "  -h, --help     この使い方を表示する\n" +
        "  --version      版を表示する\n" +
        "\n" +
        "終了コード:\n" +
        "  0  成功\n" +
        "  1  読み書きに失敗した、または入力が UTF-8 として正しくない\n" +
        "  2  不正な引数\n";

    // 版。リポジトリのアプリと同じ番号（Windows/Directory.Build.props の Version）にする。
    // リリースのワークフローが一致を確かめる
    private const string Version = "1.3.0";

    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;
    private const int ExitUsageError = 2;

    // 一度に読み込むバイト数
    private const int BufferSize = 1 << 16;

    // 変換前の文字と変換後の文字を、同じ位置に並べたもの
    private const string CommaPeriod = "，．";
    private const string ToutenKuten = "、。";

    private static int Main(string[] arguments)
    {
        string sources;
        string targets;
        var argument = arguments.Length == 1 ? arguments[0] : "";

        if (arguments.Length == 0)
        {
            sources = ToutenKuten;
            targets = CommaPeriod;
        }
        else if (argument == "-r" || argument == "--reverse")
        {
            sources = CommaPeriod;
            targets = ToutenKuten;
        }
        else if (argument == "-h" || argument == "--help" || argument == "--version")
        {
            var text = argument == "--version" ? "punctuation_converter " + Version + "\n" : Usage;
            try
            {
                WriteUtf8(OpenOrNull(Console.OpenStandardOutput), text);
                return ExitSuccess;
            }
            catch (IOException exception)
            {
                WriteError("表示できませんでした: " + exception.Message + "\n");
                return ExitFailure;
            }
        }
        else
        {
            WriteError("不正な引数です: " + string.Join(" ", arguments) + "\n\n" + Usage);
            return ExitUsageError;
        }

        try
        {
            if (Convert(OpenOrNull(Console.OpenStandardInput), OpenOrNull(Console.OpenStandardOutput), sources, targets)
                is { } invalid)
            {
                // ほかの実装と同じく、UTF-8 として正しくない入力は置き換えずにエラーにし、位置を示す
                WriteError(
                    "入力を UTF-8 として読み込めませんでした（" + invalid.Line + " 行目、先頭から " +
                    invalid.BytePosition + " バイト目）。\n");
                return ExitFailure;
            }
            return ExitSuccess;
        }
        catch (UnauthorizedAccessException)
        {
            // 標準出力が閉じていると、.NET が標準入力を開いたときに空いた番号（1）を使うため、
            // 標準出力を開けても書き込みが EBADF で失敗する。Rust の標準ライブラリと同じく、
            // 閉じた出力先には書けたものとして扱う
            return ExitSuccess;
        }
        catch (IOException exception)
        {
            WriteError("変換に失敗しました: " + exception.Message + "\n");
            return ExitFailure;
        }
    }

    // 入力が UTF-8 として正しくない位置。3つの実装で同じ数え方にする。
    // BytePosition: 正しくないバイト列の最初のバイトが、入力の先頭から何バイト目か（1 から数える）
    // Line: そのバイトが何行目か（1 から数える）。行は LF（CRLF の LF を含む）で区切り、CR だけの改行は数えない
    private readonly record struct InvalidUtf8(long BytePosition, long Line);

    // 入力を一定のバイト数ずつ読み、変換して書き出す。置換する文字はどれも1文字なので、区切る位置は問わない。
    // 正しくない UTF-8 の位置を示せるよう、バイト列として読み、自分で復号する。
    // 入力が UTF-8 として正しくなければ、その位置を返す（それまでに読んだ分は書き出している）。
    private static InvalidUtf8? Convert(Stream input, Stream output, string sources, string targets)
    {
        // BOM を付け足したり取り除いたりしないよう、BOM なしの UTF-8 として書き出す（読み込みでも BOM は U+FEFF のまま扱う）
        var outputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using var writer = new StreamWriter(output, outputEncoding, BufferSize);
        var bytes = new byte[BufferSize];
        // UTF-8 の1バイトは UTF-16 の1文字以下になるので、同じ大きさで足りる
        var characters = new char[BufferSize];
        // 前回の読み込みの末尾にあった、途中で切れた文字のバイト数（バッファの先頭に移してある）
        var carried = 0;
        // 正しいと確かめたバイト数と、その中の LF の数
        long checkedBytes = 0;
        long lineFeeds = 0;

        while (true)
        {
            var read = input.Read(bytes, carried, bytes.Length - carried);
            var filled = carried + read;
            var isEnd = read == 0;
            // 正しくないバイト列を U+FFFD に置き換えて出力を黙って変えないよう、置き換えずに止める。
            // 入力の終わりでなければ、末尾で切れた文字は NeedMoreData として残し、続きを読んでから復号する
            var status = Utf8.ToUtf16(
                bytes.AsSpan(0, filled), characters, out var bytesRead, out var charactersWritten,
                replaceInvalidSequences: false, isFinalBlock: isEnd);
            var lineFeedsRead = bytes.AsSpan(0, bytesRead).Count((byte)'\n');
            if (status == OperationStatus.InvalidData)
            {
                return new InvalidUtf8(checkedBytes + bytesRead + 1, lineFeeds + lineFeedsRead + 1);
            }
            checkedBytes += bytesRead;
            lineFeeds += lineFeedsRead;

            for (var index = 0; index < charactersWritten; index++)
            {
                var sourceIndex = sources.IndexOf(characters[index]);
                if (sourceIndex >= 0)
                {
                    characters[index] = targets[sourceIndex];
                }
            }
            writer.Write(characters, 0, charactersWritten);

            if (isEnd)
            {
                return null;
            }
            carried = filled - bytesRead;
            bytes.AsSpan(bytesRead, carried).CopyTo(bytes);
        }
    }

    // シェルの >&- などで標準入出力が閉じていると、.NET は開くときに例外を投げる。
    // ほかの実装（Rust の標準ライブラリ）と同じく、閉じた出力先には書けたものとして、閉じた入力は空として扱う
    private static Stream OpenOrNull(Func<Stream> open)
    {
        try
        {
            return open();
        }
        catch (UnauthorizedAccessException)
        {
            return Stream.Null;
        }
        catch (IOException)
        {
            return Stream.Null;
        }
    }

    // 標準エラー出力に書く。書き込めなくても、終了コードはほかの実装と同じにするため無視する
    private static void WriteError(string text)
    {
        try
        {
            WriteUtf8(OpenOrNull(Console.OpenStandardError), text);
        }
        catch (IOException)
        {
        }
    }

    private static void WriteUtf8(Stream stream, string text)
    {
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
