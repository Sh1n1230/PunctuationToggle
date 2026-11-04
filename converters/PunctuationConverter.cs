// SPDX-License-Identifier: MPL-2.0
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.

// 標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。
using System;
using System.IO;
using System.Text;

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

    // 一度に読み込む文字数
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
            Convert(OpenOrNull(Console.OpenStandardInput), OpenOrNull(Console.OpenStandardOutput), sources, targets);
            return ExitSuccess;
        }
        catch (DecoderFallbackException)
        {
            // ほかの実装と同じく、UTF-8 として正しくない入力は置き換えずにエラーにする
            WriteError("入力を UTF-8 として読み込めませんでした。\n");
            return ExitFailure;
        }
        catch (IOException exception)
        {
            WriteError("変換に失敗しました: " + exception.Message + "\n");
            return ExitFailure;
        }
    }

    // 入力を一定の文字数ずつ読み、変換して書き出す。置換する文字はどれも1文字なので、区切る位置は問わない。
    private static void Convert(Stream input, Stream output, string sources, string targets)
    {
        // BOM を付け足したり取り除いたりしないよう、BOM なしの UTF-8 として読み書きする。
        // 読み込みでは、正しくないバイト列を U+FFFD に置き換えて出力を黙って変えないよう、例外にする
        var inputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        var outputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using (var reader = new StreamReader(input, inputEncoding, detectEncodingFromByteOrderMarks: false, bufferSize: BufferSize))
        using (var writer = new StreamWriter(output, outputEncoding, BufferSize))
        {
            var buffer = new char[BufferSize];
            int length;
            while ((length = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var index = 0; index < length; index++)
                {
                    var sourceIndex = sources.IndexOf(buffer[index]);
                    if (sourceIndex >= 0)
                    {
                        buffer[index] = targets[sourceIndex];
                    }
                }
                writer.Write(buffer, 0, length);
            }
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
