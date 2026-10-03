//! 標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。

use std::env;
use std::io::{self, BufRead, BufWriter, Write};
use std::process::ExitCode;

const USAGE: &str = "\
使い方: punctuation_converter [オプション] < 入力 > 出力

標準入力の文章の句読点を変換して、標準出力に書き出します。
句読点以外の文字や改行コードは変更しません。入力は UTF-8 にしてください。

オプション:
  （なし）       「、」を「，」に、「。」を「．」に変換する
  -r, --reverse  「，」を「、」に、「．」を「。」に変換する
  -h, --help     この使い方を表示する
";

const EXIT_USAGE_ERROR: u8 = 2;

/// (変換前, 変換後) の組
type Replacements = [(char, char); 2];

const TO_COMMA_PERIOD: Replacements = [('、', '，'), ('。', '．')];
const TO_TOUTEN_KUTEN: Replacements = [('，', '、'), ('．', '。')];

fn main() -> ExitCode {
    let arguments: Vec<String> = env::args().skip(1).collect();
    let arguments: Vec<&str> = arguments.iter().map(String::as_str).collect();

    let replacements = match arguments.as_slice() {
        [] => &TO_COMMA_PERIOD,
        ["-r" | "--reverse"] => &TO_TOUTEN_KUTEN,
        ["-h" | "--help"] => {
            print!("{USAGE}");
            return ExitCode::SUCCESS;
        }
        _ => {
            eprint!("不正な引数です: {}\n\n{USAGE}", arguments.join(" "));
            return ExitCode::from(EXIT_USAGE_ERROR);
        }
    };

    match convert_stream(io::stdin().lock(), io::stdout().lock(), replacements) {
        Ok(()) => ExitCode::SUCCESS,
        Err(error) => {
            eprintln!("変換に失敗しました: {error}");
            ExitCode::FAILURE
        }
    }
}

/// 入力を1行ずつ（改行文字を含めて）読み、変換して書き出す。
fn convert_stream(
    mut input: impl BufRead,
    output: impl Write,
    replacements: &Replacements,
) -> io::Result<()> {
    let mut writer = BufWriter::new(output);
    let mut line = String::new();

    while input.read_line(&mut line)? > 0 {
        writer.write_all(convert(&line, replacements).as_bytes())?;
        line.clear();
    }
    writer.flush()
}

fn convert(text: &str, replacements: &Replacements) -> String {
    text.chars()
        .map(|character| {
            replacements
                .iter()
                .find(|(source, _)| *source == character)
                .map_or(character, |(_, target)| *target)
        })
        .collect()
}
