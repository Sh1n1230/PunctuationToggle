namespace PunctuationToggle;

/// <summary>
/// 「ログイン時に起動」で HKCU の Run キーに登録するコマンド。
/// </summary>
/// <remarks>
/// アプリのフォルダーを移動したり別の場所に展開し直したりすると、登録済みのコマンドは古い場所の exe を指したままになる。
/// その状態を「登録済み」と表示しないよう、登録されているコマンドが今動いている exe を起動するものかを確かめる。
/// </remarks>
public static class LaunchAtLoginCommand
{
    /// <summary>実行ファイルのパスから、Run キーに登録するコマンドを作る（空白を含むパスに備えて引用符で囲む）。</summary>
    public static string For(string executablePath) => $"\"{executablePath}\"";

    /// <summary>登録されているコマンドが、指定した実行ファイルを起動するものか。</summary>
    /// <remarks>Windows のパスは大文字と小文字を区別しないため、区別せずに比べる。</remarks>
    public static bool Launches(string command, string executablePath) =>
        string.Equals(Unquote(command.Trim()), executablePath, StringComparison.OrdinalIgnoreCase);

    private static string Unquote(string command) =>
        command.Length >= 2 && command[0] == '"' && command[^1] == '"' ? command[1..^1] : command;
}
