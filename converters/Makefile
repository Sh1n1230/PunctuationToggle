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
else ifneq ($(filter x86_64 amd64,$(UNAME_M)),)
  DOTNET_ARCH := x64
endif
# 上の CPU 以外では、make DOTNET_RID=linux-arm csharp-dotnet のように指定する
ifndef DOTNET_RID
  ifdef DOTNET_ARCH
    DOTNET_RID := $(DOTNET_OS)-$(DOTNET_ARCH)
  endif
endif

# Windows の実行ファイルの拡張子。Git Bash などでは、拡張子を省いても .exe を実行できる
ifeq ($(DOTNET_OS),win)
  EXE := .exe
else
  EXE :=
endif

# Python のコマンド名。Windows では python3 が無いことがあるので、make PYTHON=python のように指定する
PYTHON ?= python3

# Rust のエディション。CI の rustfmt・Clippy と CONTRIBUTING のコマンドも同じ値にする
RUST_EDITION := 2024

.PHONY: all rust rust-test csharp-dotnet test test-random benchmark clean clean-benchmark

RUST_BINARY := $(BIN_DIR)/punctuation_converter_rust$(EXE)
RUST_TEST_BINARY := $(BIN_DIR)/punctuation_converter_rust_test$(EXE)
DOTNET_BINARY := $(BIN_DIR)/dotnet/PunctuationConverter$(EXE)

# csharp-dotnet は .NET SDK が無い環境でも `make` が通るよう、all に含めない
all: rust

$(BIN_DIR):
	mkdir -p $(BIN_DIR)

# ソースが変わったときだけビルドし直す（make を続けて実行しても、2回目は何もしない）
csharp-dotnet: $(DOTNET_BINARY)

$(DOTNET_BINARY): PunctuationConverter.cs dotnet/PunctuationConverter.csproj | $(BIN_DIR)
ifndef DOTNET_RID
	$(error 実行中の CPU（$(UNAME_M)）の .NET のランタイム識別子が分かりません。make DOTNET_RID=linux-arm csharp-dotnet のように指定してください)
endif
	dotnet publish dotnet/PunctuationConverter.csproj -c Release -r $(DOTNET_RID) "-p:PublishDir=$(CURDIR)/$(BIN_DIR)/dotnet/" --nologo

rust: $(RUST_BINARY)

$(RUST_BINARY): punctuation_converter.rs | $(BIN_DIR)
	rustc --edition $(RUST_EDITION) -O -o $@ punctuation_converter.rs

# Rust 実装の内部の関数（UTF-8 の文字の区切りの処理など）の単体テスト
rust-test: $(RUST_TEST_BINARY)
	$(RUST_TEST_BINARY)

$(RUST_TEST_BINARY): punctuation_converter.rs | $(BIN_DIR)
	rustc --edition $(RUST_EDITION) --test -o $@ punctuation_converter.rs

# ビルド済みの実装がソースより古ければ作り直してからテストする（ビルドしていない実装は SKIP のまま）
test:
	@if [ -e $(RUST_BINARY) ]; then $(MAKE) --no-print-directory rust; fi
	@if [ -e $(DOTNET_BINARY) ]; then $(MAKE) --no-print-directory csharp-dotnet; fi
	tests/run_tests.sh

# ランダムに作った入力で、各実装が同じ仕様を満たすかを確かめる（RANDOM_SEED で入力を再現できる）
test-random:
	$(PYTHON) tests/run_random_tests.py

benchmark:
	PYTHON=$(PYTHON) benchmark/run_benchmark.sh

# ビルドの生成物と、ベンチマークの合成データ（約30MB）・結果を削除する
clean: clean-benchmark
	rm -rf $(BIN_DIR) dotnet/bin dotnet/obj

# ベンチマークの合成データと結果だけを削除する
clean-benchmark:
	rm -rf benchmark/repeated_text.txt benchmark/results
