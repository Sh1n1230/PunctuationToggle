//! 標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。

use std::env;
use std::io::{self, BufWriter, ErrorKind, Read, Write};
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

/// 一度に読み込むバイト数。改行の無い巨大な入力でもメモリを使い切らないよう、行単位ではなく一定の量ずつ読む
const CHUNK_SIZE: usize = 1 << 16;

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

/// 入力を一定のバイト数ずつ読み、変換して書き出す。置換する文字はどれも1文字なので、
/// UTF-8 の文字の途中でさえなければ、どこで区切っても変換結果は同じ。
fn convert_stream(
    mut input: impl Read,
    output: impl Write,
    replacements: &Replacements,
) -> io::Result<()> {
    let mut writer = BufWriter::new(output);
    let mut buffer = vec![0; CHUNK_SIZE];
    // 前回の読み込みの末尾にあった、途中で切れた文字のバイト数（バッファの先頭に移してある）
    let mut carried = 0;

    loop {
        let read = match input.read(&mut buffer[carried..]) {
            Ok(read) => read,
            Err(error) if error.kind() == ErrorKind::Interrupted => continue,
            Err(error) => return Err(error),
        };
        if read == 0 {
            break;
        }
        let filled = carried + read;

        // 末尾で途中までしか読めていない文字は、続きを読んでから変換する
        let valid = complete_prefix_len(&buffer[..filled]);
        let text = std::str::from_utf8(&buffer[..valid]).map_err(|_| invalid_utf8_error())?;
        writer.write_all(convert(text, replacements).as_bytes())?;

        buffer.copy_within(valid..filled, 0);
        carried = filled - valid;
    }

    // 末尾の文字が途中で切れている
    if carried > 0 {
        return Err(invalid_utf8_error());
    }
    writer.flush()
}

/// 末尾で文字が途中で切れている場合に、その文字の手前までのバイト数を返す。
/// 切れていなければ全体の長さを返す（不正なバイト列かどうかは呼び出し側で検証する）。
fn complete_prefix_len(bytes: &[u8]) -> usize {
    // UTF-8 の1文字は最大4バイトなので、末尾から最大4バイト戻って最後の文字の先頭バイトを探す
    for distance_from_end in 1..=bytes.len().min(4) {
        let index = bytes.len() - distance_from_end;
        let byte = bytes[index];
        let is_continuation = byte & 0b1100_0000 == 0b1000_0000;
        if !is_continuation {
            let character_length = match byte {
                0xC0..=0xDF => 2,
                0xE0..=0xEF => 3,
                0xF0..=0xF7 => 4,
                _ => 1,
            };
            return if distance_from_end < character_length {
                index
            } else {
                bytes.len()
            };
        }
    }
    bytes.len()
}

fn invalid_utf8_error() -> io::Error {
    io::Error::new(
        ErrorKind::InvalidData,
        "入力を UTF-8 として読み込めませんでした",
    )
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
