// 標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。

import { pipeline, Transform } from "node:stream";
import { StringDecoder } from "node:string_decoder";

const USAGE = `使い方: punctuation_converter [オプション] < 入力 > 出力

標準入力の文章の句読点を変換して、標準出力に書き出します。
句読点以外の文字や改行コードは変更しません。入力は UTF-8 にしてください。

オプション:
  （なし）       「、」を「，」に、「。」を「．」に変換する
  -r, --reverse  「，」を「、」に、「．」を「。」に変換する
  -h, --help     この使い方を表示する
`;

const EXIT_SUCCESS = 0;
const EXIT_FAILURE = 1;
const EXIT_USAGE_ERROR = 2;

/** 変換前の文字 → 変換後の文字 */
type Replacements = ReadonlyMap<string, string>;

const TO_COMMA_PERIOD: Replacements = new Map([
  ["、", "，"],
  ["。", "．"],
]);
const TO_TOUTEN_KUTEN: Replacements = new Map([
  ["，", "、"],
  ["．", "。"],
]);

function convert(text: string, replacements: Replacements): string {
  let converted = text;
  for (const [source, target] of replacements) {
    converted = converted.replaceAll(source, target);
  }
  return converted;
}

/** 受け取ったバイト列を UTF-8 として読み、変換して流す。文字の途中で区切られたバイト列は次に持ち越す。 */
function createConverter(replacements: Replacements): Transform {
  const decoder = new StringDecoder("utf8");
  return new Transform({
    transform(chunk: Buffer, _encoding, callback) {
      callback(null, convert(decoder.write(chunk), replacements));
    },
    flush(callback) {
      callback(null, convert(decoder.end(), replacements));
    },
  });
}

function main(argumentList: readonly string[]): void {
  const argument = argumentList.join(" ");
  let replacements: Replacements;

  if (argumentList.length === 0) {
    replacements = TO_COMMA_PERIOD;
  } else if (argument === "-r" || argument === "--reverse") {
    replacements = TO_TOUTEN_KUTEN;
  } else if (argument === "-h" || argument === "--help") {
    process.stdout.write(USAGE);
    process.exitCode = EXIT_SUCCESS;
    return;
  } else {
    process.stderr.write(`不正な引数です: ${argument}\n\n${USAGE}`);
    process.exitCode = EXIT_USAGE_ERROR;
    return;
  }

  pipeline(process.stdin, createConverter(replacements), process.stdout, (error) => {
    if (error) {
      process.stderr.write(`変換に失敗しました: ${error.message}\n`);
      process.exitCode = EXIT_FAILURE;
    }
  });
}

main(process.argv.slice(2));
