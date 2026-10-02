#include <iostream>
#include <string>

void replace_all(std::string &s, const std::string &from, const std::string &to)
{
    size_t pos = 0;
    while ((pos = s.find(from, pos)) != std::string::npos)
    {
        s.replace(pos, from.length(), to);
        pos += to.length();
    }
}

int main()
{
    // I/O処理を高速化
    std::ios_base::sync_with_stdio(false);
    std::cin.tie(NULL);

    std::string line;
    while (std::getline(std::cin, line))
    {
        replace_all(line, "、", "，");
        replace_all(line, "。", "．");
        std::cout << line << "\n";
    }
    return 0;
}