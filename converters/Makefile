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

.PHONY: all rust csharp-dotnet test benchmark clean

# csharp-dotnet は .NET SDK が無い環境でも `make` が通るよう、all に含めない
all: rust

$(BIN_DIR):
	mkdir -p $(BIN_DIR)

csharp-dotnet: | $(BIN_DIR)
	dotnet publish dotnet/PunctuationConverter.csproj -c Release -r $(DOTNET_RID) -p:PublishDir=$(CURDIR)/$(BIN_DIR)/dotnet/ --nologo

rust: | $(BIN_DIR)
	rustc --edition 2021 -O -o $(BIN_DIR)/punctuation_converter_rust punctuation_converter.rs

test:
	tests/run_tests.sh

benchmark:
	benchmark/run_benchmark.sh

clean:
	rm -rf $(BIN_DIR) dotnet/bin dotnet/obj
