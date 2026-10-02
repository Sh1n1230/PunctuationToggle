BIN_DIR := bin
DOTNET_RID := osx-arm64

.PHONY: all clean cpp cs cs-dotnet go java py rs ts

all: cpp cs go java rs ts

$(BIN_DIR):
	mkdir -p $(BIN_DIR)

cpp: $(BIN_DIR)
	g++ -O3 -o $(BIN_DIR)/cpp_bin punc_converter.cpp

cs: $(BIN_DIR)
	mcs -optimize+ -out:$(BIN_DIR)/punc_converter.exe punc_converter.cs

# .NET SDK + Native AOT 版（Mono より現実的な性能が出る、Windows実装の判断材料用）
cs-dotnet: $(BIN_DIR)
	dotnet publish dotnet/PuncConverter.csproj -c Release -r $(DOTNET_RID) --self-contained true -p:PublishAot=true -o $(BIN_DIR)/dotnet-aot

go: $(BIN_DIR)
	go build -o $(BIN_DIR)/go_bin punc_converter.go

java: $(BIN_DIR)
	javac -d $(BIN_DIR) punc_converter.java

rs: $(BIN_DIR)
	rustc -O -o $(BIN_DIR)/rust_bin punc_converter.rs

ts: $(BIN_DIR)
	tsc --target es2021 --module commonjs --types node --outDir $(BIN_DIR) punc_converter.ts

clean:
	rm -rf $(BIN_DIR)
