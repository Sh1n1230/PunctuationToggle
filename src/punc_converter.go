package main

import (
	"bufio"
	"os"
	"strings"
)

func main() {
	reader := bufio.NewReaderSize(os.Stdin, 1<<20)
	writer := bufio.NewWriterSize(os.Stdout, 1<<20)
	defer writer.Flush()

	for {
		line, err := reader.ReadString('\n')
		if len(line) > 0 {
			converted := strings.ReplaceAll(line, "、", "，")
			converted = strings.ReplaceAll(converted, "。", "．")
			writer.WriteString(converted)
		}
		if err != nil {
			break
		}
	}
}