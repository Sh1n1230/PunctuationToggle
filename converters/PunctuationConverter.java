import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.Reader;
import java.io.Writer;
import java.nio.charset.StandardCharsets;
import java.util.List;

/** 標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。 */
public final class PunctuationConverter {
    private static final String USAGE = """
            使い方: punctuation_converter [オプション] < 入力 > 出力

            標準入力の文章の句読点を変換して、標準出力に書き出します。
            句読点以外の文字や改行コードは変更しません。入力は UTF-8 にしてください。

            オプション:
              （なし）       「、」を「，」に、「。」を「．」に変換する
              -r, --reverse  「，」を「、」に、「．」を「。」に変換する
              -h, --help     この使い方を表示する
            """;

    private static final int EXIT_SUCCESS = 0;
    private static final int EXIT_FAILURE = 1;
    private static final int EXIT_USAGE_ERROR = 2;

    // 一度に読み込む文字数
    private static final int BUFFER_SIZE = 1 << 16;

    /** 変換前の文字と変換後の文字の組。 */
    private record Replacement(char source, char target) {}

    private static final List<Replacement> TO_COMMA_PERIOD =
            List.of(new Replacement('、', '，'), new Replacement('。', '．'));
    private static final List<Replacement> TO_TOUTEN_KUTEN =
            List.of(new Replacement('，', '、'), new Replacement('．', '。'));

    private PunctuationConverter() {}

    public static void main(String[] arguments) {
        System.exit(run(List.of(arguments)));
    }

    private static int run(List<String> arguments) {
        final List<Replacement> replacements;
        if (arguments.isEmpty()) {
            replacements = TO_COMMA_PERIOD;
        } else if (arguments.equals(List.of("-r")) || arguments.equals(List.of("--reverse"))) {
            replacements = TO_TOUTEN_KUTEN;
        } else if (arguments.equals(List.of("-h")) || arguments.equals(List.of("--help"))) {
            System.out.print(USAGE);
            System.out.flush();
            return EXIT_SUCCESS;
        } else {
            System.err.print("不正な引数です: " + String.join(" ", arguments) + "\n\n" + USAGE);
            return EXIT_USAGE_ERROR;
        }

        try {
            convert(
                    new InputStreamReader(System.in, StandardCharsets.UTF_8),
                    new BufferedWriter(new OutputStreamWriter(System.out, StandardCharsets.UTF_8), BUFFER_SIZE),
                    replacements);
            return EXIT_SUCCESS;
        } catch (IOException exception) {
            System.err.println("変換に失敗しました: " + exception.getMessage());
            return EXIT_FAILURE;
        }
    }

    /** 入力を一定の文字数ずつ読み、変換して書き出す。置換する文字はどれも1文字なので、区切る位置は問わない。 */
    private static void convert(Reader reader, Writer writer, List<Replacement> replacements) throws IOException {
        char[] buffer = new char[BUFFER_SIZE];
        int length;
        while ((length = reader.read(buffer)) != -1) {
            for (int index = 0; index < length; index++) {
                buffer[index] = replace(buffer[index], replacements);
            }
            writer.write(buffer, 0, length);
        }
        writer.flush();
    }

    private static char replace(char character, List<Replacement> replacements) {
        for (Replacement replacement : replacements) {
            if (replacement.source() == character) {
                return replacement.target();
            }
        }
        return character;
    }
}
