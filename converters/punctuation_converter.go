// punctuation_converter は、標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。
package main

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"strings"
)

const usage = `使い方: punctuation_converter [オプション] < 入力 > 出力

標準入力の文章の句読点を変換して、標準出力に書き出します。
句読点以外の文字や改行コードは変更しません。入力は UTF-8 にしてください。

オプション:
  （なし）       「、」を「，」に、「。」を「．」に変換する
  -r, --reverse  「，」を「、」に、「．」を「。」に変換する
  -h, --help     この使い方を表示する
`

const (
	exitSuccess    = 0
	exitFailure    = 1
	exitUsageError = 2
)

const bufferSize = 1 << 16

var (
	toCommaPeriod = strings.NewReplacer("、", "，", "。", "．")
	toToutenKuten = strings.NewReplacer("，", "、", "．", "。")
)

func main() {
	os.Exit(run(os.Args[1:], os.Stdin, os.Stdout, os.Stderr))
}

func run(arguments []string, stdin io.Reader, stdout, stderr io.Writer) int {
	var replacer *strings.Replacer
	switch strings.Join(arguments, " ") {
	case "":
		replacer = toCommaPeriod
	case "-r", "--reverse":
		replacer = toToutenKuten
	case "-h", "--help":
		fmt.Fprint(stdout, usage)
		return exitSuccess
	default:
		fmt.Fprintf(stderr, "不正な引数です: %s\n\n%s", strings.Join(arguments, " "), usage)
		return exitUsageError
	}

	if err := convert(stdin, stdout, replacer); err != nil {
		fmt.Fprintf(stderr, "変換に失敗しました: %v\n", err)
		return exitFailure
	}
	return exitSuccess
}

// convert は、入力を1行ずつ（改行文字を含めて）読み、変換して書き出す。
// 置換する文字は改行をまたがないので、行の途中で区切られることはない。
func convert(input io.Reader, output io.Writer, replacer *strings.Replacer) error {
	reader := bufio.NewReaderSize(input, bufferSize)
	writer := bufio.NewWriterSize(output, bufferSize)

	for {
		line, readErr := reader.ReadString('\n')
		if _, err := replacer.WriteString(writer, line); err != nil {
			return err
		}
		if readErr == io.EOF {
			break
		}
		if readErr != nil {
			return readErr
		}
	}
	return writer.Flush()
}
