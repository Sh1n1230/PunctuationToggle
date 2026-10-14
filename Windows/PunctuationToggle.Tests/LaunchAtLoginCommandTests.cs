namespace PunctuationToggle.Tests;

public class LaunchAtLoginCommandTests
{
    private const string ExecutablePath = @"C:\Users\user\AppData\Local\Programs\PunctuationToggle\PunctuationToggle.exe";

    [Fact]
    public void 登録するコマンドはパスを引用符で囲む()
    {
        Assert.Equal($"\"{ExecutablePath}\"", LaunchAtLoginCommand.For(ExecutablePath));
    }

    [Fact]
    public void 登録したコマンドは同じ実行ファイルを起動する()
    {
        Assert.True(LaunchAtLoginCommand.Launches(LaunchAtLoginCommand.For(ExecutablePath), ExecutablePath));
    }

    [Theory]
    [InlineData(@"C:\Users\user\AppData\Local\Programs\PunctuationToggle\PunctuationToggle.exe")]
    [InlineData(@" ""C:\Users\user\AppData\Local\Programs\PunctuationToggle\PunctuationToggle.exe"" ")]
    [InlineData(@"""c:\users\USER\appdata\local\programs\punctuationtoggle\PUNCTUATIONTOGGLE.EXE""")]
    public void 引用符の有無や大文字小文字の違いは同じ実行ファイルとみなす(string command)
    {
        Assert.True(LaunchAtLoginCommand.Launches(command, ExecutablePath));
    }

    [Theory]
    [InlineData(@"""C:\Users\user\Downloads\PunctuationToggle-v1.2.0-win-x64\PunctuationToggle.exe""")]
    [InlineData(@"""C:\Users\user\AppData\Local\Programs\PunctuationToggle\PunctuationToggle.exe"" --option")]
    [InlineData("")]
    [InlineData(@"""")]
    public void 別の場所の実行ファイルや別のコマンドは同じとみなさない(string command)
    {
        Assert.False(LaunchAtLoginCommand.Launches(command, ExecutablePath));
    }
}
