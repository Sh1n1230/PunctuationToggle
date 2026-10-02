# 句読点変換の各言語の実装をビルドし、テストやベンチマークを実行する。
# 生成物はすべて bin/ に出力する。

BIN_DIR := bin

# .NET（Native AOT）のランタイム識別子。実行中の OS と CPU から決める。
UNAME_S := $(shell uname -s)
UNAME_M := $(shell uname -m)
ifeq ($(UNAME_S),Darwin)
  DOTNET_OS := osx
else
  DOTNET_OS := linux
endif
ifneq ($(filter arm64 aarch64,$(UNAME_M)),)
  DOTNET_ARCH := arm64
else
  DOTNET_ARCH := x64
endif
DOTNET_RID ?= $(DOTNET_OS)-$(DOTNET_ARCH)

.PHONY: all cpp csharp-mono csharp-dotnet go java rust typescript test benchmark clean

# csharp-dotnet は .NET SDK が無い環境でも `make` が通るよう、all に含めない
all: cpp csharp-mono go java rust typescript

$(BIN_DIR):
	mkdir -p $(BIN_DIR)

cpp: | $(BIN_DIR)
	c++ -std=c++17 -O3 -Wall -Wextra -o $(BIN_DIR)/punctuation_converter_cpp punctuation_converter.cpp

csharp-mono: | $(BIN_DIR)
	mcs -optimize+ -out:$(BIN_DIR)/PunctuationConverter.exe PunctuationConverter.cs

csharp-dotnet: | $(BIN_DIR)
	dotnet publish dotnet/PunctuationConverter.csproj -c Release -r $(DOTNET_RID) -p:PublishDir=$(CURDIR)/$(BIN_DIR)/dotnet/ --nologo

go: | $(BIN_DIR)
	go vet punctuation_converter.go
	go build -o $(BIN_DIR)/punctuation_converter_go punctuation_converter.go

java: | $(BIN_DIR)
	javac -Xlint:all -Werror -d $(BIN_DIR) PunctuationConverter.java

rust: | $(BIN_DIR)
	rustc --edition 2021 -O -o $(BIN_DIR)/punctuation_converter_rust punctuation_converter.rs

typescript: | $(BIN_DIR)
	npx --no-install tsc -p tsconfig.json

test:
	tests/run_tests.sh

benchmark:
	benchmark/run_benchmark.sh

clean:
	rm -rf $(BIN_DIR) dotnet/bin dotnet/obj
