// 標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。
//
// Mono（mcs）と .NET SDK（dotnet/PunctuationConverter.csproj）の両方でビルドするため、
// Mono の C# コンパイラーが対応している構文だけを使っている。
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
        "  -h, --help     この使い方を表示する\n";

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
        else if (argument == "-h" || argument == "--help")
        {
            WriteUtf8(Console.OpenStandardOutput(), Usage);
            return ExitSuccess;
        }
        else
        {
            WriteUtf8(Console.OpenStandardError(), "不正な引数です: " + string.Join(" ", arguments) + "\n\n" + Usage);
            return ExitUsageError;
        }

        try
        {
            Convert(Console.OpenStandardInput(), Console.OpenStandardOutput(), sources, targets);
            return ExitSuccess;
        }
        catch (IOException exception)
        {
            WriteUtf8(Console.OpenStandardError(), "変換に失敗しました: " + exception.Message + "\n");
            return ExitFailure;
        }
    }

    // 入力を一定の文字数ずつ読み、変換して書き出す。置換する文字はどれも1文字なので、区切る位置は問わない。
    private static void Convert(Stream input, Stream output, string sources, string targets)
    {
        // BOM を付け足したり取り除いたりしないよう、BOM なしの UTF-8 として読み書きする
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using (var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: BufferSize))
        using (var writer = new StreamWriter(output, encoding, BufferSize))
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

    private static void WriteUtf8(Stream stream, string text)
    {
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
