// 標準入力の文章の句読点を「、。」⇔「，．」に変換して、標準出力に書き出す。

#include <array>
#include <cstddef>
#include <cstdio>
#include <iostream>
#include <string>
#include <string_view>
#include <utility>
#include <vector>

namespace {

constexpr std::string_view kUsage =
    "使い方: punctuation_converter [オプション] < 入力 > 出力\n"
    "\n"
    "標準入力の文章の句読点を変換して、標準出力に書き出します。\n"
    "句読点以外の文字や改行コードは変更しません。入力は UTF-8 にしてください。\n"
    "\n"
    "オプション:\n"
    "  （なし）       「、」を「，」に、「。」を「．」に変換する\n"
    "  -r, --reverse  「，」を「、」に、「．」を「。」に変換する\n"
    "  -h, --help     この使い方を表示する\n";

constexpr int kExitSuccess = 0;
constexpr int kExitFailure = 1;
constexpr int kExitUsageError = 2;

// 一度に読み込むバイト数
constexpr std::size_t kBufferSize = 1 << 16;

// {変換前, 変換後} の組（UTF-8 のバイト列）
using Replacements = std::array<std::pair<std::string_view, std::string_view>, 2>;

constexpr Replacements kToCommaPeriod = {{{"、", "，"}, {"。", "．"}}};
constexpr Replacements kToToutenKuten = {{{"，", "、"}, {"．", "。"}}};

void replaceAll(std::string& text, std::string_view from, std::string_view to) {
    for (auto position = text.find(from); position != std::string::npos;
         position = text.find(from, position + to.size())) {
        text.replace(position, from.size(), to);
    }
}

bool writeConverted(std::string& text, std::FILE* output, const Replacements& replacements) {
    for (const auto& [from, to] : replacements) {
        replaceAll(text, from, to);
    }
    return std::fwrite(text.data(), 1, text.size(), output) == text.size();
}

// 入力を一定のバイト数ずつ読み、最後の改行までを変換して書き出す。
// 置換する文字は改行をまたがない（UTF-8 の複数バイト文字は改行のバイトを含まない）ので、
// 改行の直後で区切れば文字の途中で切れることはない。
// （iostream は実装によって1文字ずつ読むため遅いので、C の標準入出力を使う）
bool convertStream(std::FILE* input, std::FILE* output, const Replacements& replacements) {
    std::vector<char> buffer(kBufferSize);
    std::string pending;  // まだ改行が来ていない行

    std::size_t bytesRead = 0;
    while ((bytesRead = std::fread(buffer.data(), 1, buffer.size(), input)) > 0) {
        const std::string_view chunk(buffer.data(), bytesRead);
        const auto lastNewline = chunk.rfind('\n');
        if (lastNewline == std::string_view::npos) {
            pending.append(chunk);
            continue;
        }
        pending.append(chunk.substr(0, lastNewline + 1));
        if (!writeConverted(pending, output, replacements)) {
            return false;
        }
        pending.assign(chunk.substr(lastNewline + 1));
    }

    return std::ferror(input) == 0 && writeConverted(pending, output, replacements) &&
           std::fflush(output) == 0;
}

}  // namespace

int main(int argc, char* argv[]) {
    const std::string_view argument = argc == 2 ? argv[1] : "";
    const Replacements* replacements = nullptr;

    if (argc == 1) {
        replacements = &kToCommaPeriod;
    } else if (argc == 2 && (argument == "-r" || argument == "--reverse")) {
        replacements = &kToToutenKuten;
    } else if (argc == 2 && (argument == "-h" || argument == "--help")) {
        std::cout << kUsage;
        return kExitSuccess;
    } else {
        std::cerr << "不正な引数です:";
        for (int index = 1; index < argc; ++index) {
            std::cerr << ' ' << argv[index];
        }
        std::cerr << "\n\n" << kUsage;
        return kExitUsageError;
    }

    if (!convertStream(stdin, stdout, *replacements)) {
        std::cerr << "変換に失敗しました。\n";
        return kExitFailure;
    }
    return kExitSuccess;
}
