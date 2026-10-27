# 句読点変換の各言語の実装をビルドし、テストやベンチマークを実行する。
# 生成物はすべて bin/ に出力する。

BIN_DIR := bin

# .NET（Native AOT）のランタイム識別子。実行中の OS と CPU から決める。
UNAME_S := $(shell uname -s)
UNAME_M := $(shell uname -m)
ifeq ($(UNAME_S),Darwin)
  DOTNET_OS := osx
else ifneq ($(filter MINGW% MSYS% CYGWIN%,$(UNAME_S)),)
  # Windows の Git Bash などでは uname -s が MINGW64_NT-10.0-26200 のようになる
  DOTNET_OS := win
else
  DOTNET_OS := linux
endif
ifneq ($(filter arm64 aarch64,$(UNAME_M)),)
  DOTNET_ARCH := arm64
else
  DOTNET_ARCH := x64
endif
DOTNET_RID ?= $(DOTNET_OS)-$(DOTNET_ARCH)

.PHONY: all rust rust-test csharp-dotnet test test-random benchmark clean

# csharp-dotnet は .NET SDK が無い環境でも `make` が通るよう、all に含めない
all: rust

$(BIN_DIR):
	mkdir -p $(BIN_DIR)

csharp-dotnet: | $(BIN_DIR)
	dotnet publish dotnet/PunctuationConverter.csproj -c Release -r $(DOTNET_RID) -p:PublishDir=$(CURDIR)/$(BIN_DIR)/dotnet/ --nologo

rust: | $(BIN_DIR)
	rustc --edition 2021 -O -o $(BIN_DIR)/punctuation_converter_rust punctuation_converter.rs

# Rust 実装の内部の関数（UTF-8 の文字の区切りの処理など）の単体テスト
rust-test: | $(BIN_DIR)
	rustc --edition 2021 --test -o $(BIN_DIR)/punctuation_converter_rust_test punctuation_converter.rs
	$(BIN_DIR)/punctuation_converter_rust_test

test:
	tests/run_tests.sh

# ランダムに作った入力で、各実装が同じ仕様を満たすかを確かめる（RANDOM_SEED で入力を再現できる）
test-random:
	python3 tests/run_random_tests.py

benchmark:
	benchmark/run_benchmark.sh

clean:
	rm -rf $(BIN_DIR) dotnet/bin dotnet/obj
