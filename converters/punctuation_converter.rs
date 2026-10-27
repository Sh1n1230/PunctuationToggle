//! 標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。

use std::borrow::Cow;
use std::env;
use std::ffi::OsString;
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
    // env::args() は UTF-8 でない引数（macOS や Linux で渡せる）で panic するため、args_os() で受け取る。
    // UTF-8 でない部分は U+FFFD に置き換わり、どのオプションにも一致しないので、不正な引数として扱われる
    let arguments: Vec<OsString> = env::args_os().skip(1).collect();
    let arguments: Vec<Cow<str>> = arguments
        .iter()
        .map(|argument| argument.to_string_lossy())
        .collect();
    let arguments: Vec<&str> = arguments.iter().map(AsRef::as_ref).collect();

    let replacements = match arguments.as_slice() {
        [] => &TO_COMMA_PERIOD,
        ["-r" | "--reverse"] => &TO_TOUTEN_KUTEN,
        ["-h" | "--help"] => {
            // print! は書き込めないと panic するため、結果を見て終了コードを決める
            let mut stdout = io::stdout().lock();
            return match stdout
                .write_all(USAGE.as_bytes())
                .and_then(|()| stdout.flush())
            {
                Ok(()) => ExitCode::SUCCESS,
                Err(error) => {
                    write_error(&format!("使い方を表示できませんでした: {error}\n"));
                    ExitCode::FAILURE
                }
            };
        }
        _ => {
            write_error(&format!(
                "不正な引数です: {}\n\n{USAGE}",
                arguments.join(" ")
            ));
            return ExitCode::from(EXIT_USAGE_ERROR);
        }
    };

    match convert_stream(io::stdin().lock(), io::stdout().lock(), replacements) {
        Ok(()) => ExitCode::SUCCESS,
        Err(error) => {
            write_error(&format!("変換に失敗しました: {error}\n"));
            ExitCode::FAILURE
        }
    }
}

/// 標準エラー出力に書く。eprint! は書き込めないと panic するため使わない。
/// 書き込めなくても、終了コードはほかの実装と同じにするため無視する
fn write_error(message: &str) {
    let _ = io::stderr().write_all(message.as_bytes());
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

#[cfg(test)]
mod tests {
    use super::*;

    /// 1回の read で最大 `chunk_length` バイトしか返さない入力（パイプなどを模す）
    struct ShortReads<'a> {
        remaining: &'a [u8],
        chunk_length: usize,
    }

    impl Read for ShortReads<'_> {
        fn read(&mut self, buffer: &mut [u8]) -> io::Result<usize> {
            let length = self
                .remaining
                .len()
                .min(self.chunk_length)
                .min(buffer.len());
            buffer[..length].copy_from_slice(&self.remaining[..length]);
            self.remaining = &self.remaining[length..];
            Ok(length)
        }
    }

    fn convert_bytes(input: impl Read) -> io::Result<Vec<u8>> {
        let mut output = Vec::new();
        convert_stream(input, &mut output, &TO_COMMA_PERIOD)?;
        Ok(output)
    }

    #[test]
    fn 空とascii_だけなら全体が完全な文字() {
        assert_eq!(complete_prefix_len(b""), 0);
        assert_eq!(complete_prefix_len(b"abc"), 3);
    }

    #[test]
    fn 二バイトの文字の途中で終わる() {
        // é = C3 A9
        assert_eq!(complete_prefix_len(b"a\xc3"), 1);
        assert_eq!(complete_prefix_len(b"a\xc3\xa9"), 3);
    }

    #[test]
    fn 三バイトの文字の途中で終わる() {
        // 、 = E3 80 81
        assert_eq!(complete_prefix_len(b"a\xe3"), 1);
        assert_eq!(complete_prefix_len(b"a\xe3\x80"), 1);
        assert_eq!(complete_prefix_len(b"a\xe3\x80\x81"), 4);
    }

    #[test]
    fn 四バイトの文字の途中で終わる() {
        // 😀 = F0 9F 98 80
        assert_eq!(complete_prefix_len(b"a\xf0"), 1);
        assert_eq!(complete_prefix_len(b"a\xf0\x9f"), 1);
        assert_eq!(complete_prefix_len(b"a\xf0\x9f\x98"), 1);
        assert_eq!(complete_prefix_len(b"a\xf0\x9f\x98\x80"), 5);
    }

    #[test]
    fn 一バイトずつしか読めなくても一度に読んだ場合と同じ結果になる() {
        let input = "😀、𠮷。a，b．é\r\n".repeat(3);
        let expected = convert_bytes(input.as_bytes()).unwrap();
        for chunk_length in 1..=4 {
            let reader = ShortReads {
                remaining: input.as_bytes(),
                chunk_length,
            };
            assert_eq!(
                convert_bytes(reader).unwrap(),
                expected,
                "chunk_length = {chunk_length}"
            );
        }
        assert_eq!(expected, "😀，𠮷．a，b．é\r\n".repeat(3).as_bytes());
    }

    #[test]
    fn 末尾で文字が切れたまま終わると不正なデータ() {
        let error = convert_bytes(&b"a\xe3\x80"[..]).unwrap_err();
        assert_eq!(error.kind(), ErrorKind::InvalidData);
    }
}
