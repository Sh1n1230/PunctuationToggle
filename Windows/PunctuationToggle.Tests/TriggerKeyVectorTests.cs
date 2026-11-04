using System.Text.Json;

namespace PunctuationToggle.Tests;

/// <summary>
/// macOS 版と共通のテストベクタ（リポジトリのルートの testdata/trigger_key_vectors.json）で、
/// 切り替えキーの判定と読み取りを確かめる。ベクタの書き方は JSON の format を参照。
/// </summary>
public class TriggerKeyVectorTests
{
    private static readonly JsonElement Vectors = LoadVectors();

    public static TheoryData<string> DetectorScenarios => ScenarioNames("detector");

    public static TheoryData<string> CaptureScenarios => ScenarioNames("capture");

    [Theory]
    [MemberData(nameof(DetectorScenarios))]
    public void 切り替えキーの判定がベクタのとおりになる(string scenarioName)
    {
        var scenario = FindScenario("detector", scenarioName);
        var triggerKey = TriggerKey.FromVirtualKey(VirtualKey(scenario.GetProperty("trigger").GetString()));
        Assert.NotNull(triggerKey);
        var detector = new TriggerKeyDetector(triggerKey);

        var index = 0;
        foreach (var step in scenario.GetProperty("inputs").EnumerateArray())
        {
            index++;
            var decision = detector.Handle(KeyboardInputFor(step));
            if (!step.TryGetProperty("expect", out var expected))
            {
                continue;
            }
            var expectedDecision = new InputDecision(
                ShouldToggle: OptionalBool(expected, "toggle") ?? false,
                ShouldSuppress: OptionalBool(expected, "suppress") ?? false);
            Assert.True(
                expectedDecision == decision,
                $"{index} 番目の入力（{step.GetProperty("input").GetString()}）: 期待 {expectedDecision}、実際 {decision}");
        }
    }

    [Theory]
    [MemberData(nameof(CaptureScenarios))]
    public void 設定画面でのキーの読み取りがベクタのとおりになる(string scenarioName)
    {
        var scenario = FindScenario("capture", scenarioName);
        var capture = new TriggerKeyCapture();

        var index = 0;
        foreach (var step in scenario.GetProperty("inputs").EnumerateArray())
        {
            index++;
            var input = KeyboardInputFor(step);
            var isActive = OptionalBool(step, "active") ?? true;
            var result = isActive ? capture.Handle(input) : capture.HandleWhileInactive(input);
            if (!step.TryGetProperty("expect", out var expected))
            {
                continue;
            }

            var description = $"{index} 番目の入力（{step.GetProperty("input").GetString()}）";
            var expectedOutcome = expected.GetProperty("outcome").GetString() switch
            {
                "waiting" => CaptureOutcome.Waiting,
                "captured" => CaptureOutcome.Captured,
                "rejected" => CaptureOutcome.Rejected,
                "cancelled" => CaptureOutcome.Cancelled,
                var outcome => throw new InvalidDataException($"不明な outcome です: {outcome}"),
            };
            Assert.True(expectedOutcome == result.Outcome, $"{description}: 期待 {expectedOutcome}、実際 {result.Outcome}");
            if (expectedOutcome == CaptureOutcome.Captured)
            {
                Assert.Equal(VirtualKey(expected.GetProperty("key").GetString()), result.CapturedKey?.VirtualKey);
            }
            if (expectedOutcome == CaptureOutcome.Rejected)
            {
                Assert.Equal(VirtualKey(expected.GetProperty("key").GetString()), result.RejectedVirtualKey);
            }
            if (OptionalBool(expected, "suppress") is { } suppress)
            {
                Assert.True(suppress == result.ShouldSuppress, $"{description}: 入力を渡さないか（期待 {suppress}）");
            }
        }
    }

    /// <summary>ベクタの入力を Windows の入力にする。修飾キーも KeyDown・KeyUp で表す。</summary>
    private static KeyboardInput KeyboardInputFor(JsonElement step)
    {
        var kind = step.GetProperty("input").GetString();
        return kind switch
        {
            "modifierDown" => KeyboardInput.KeyDown(
                KeyOf(step), otherModifiersHeld: OptionalBool(step, "otherModifiersHeld") ?? false),
            "modifierRepeat" => KeyboardInput.KeyDown(KeyOf(step), isRepeat: true),
            "modifierUp" or "keyUp" => KeyboardInput.KeyUp(KeyOf(step)),
            "keyDown" => KeyboardInput.KeyDown(
                KeyOf(step),
                isRepeat: OptionalBool(step, "isRepeat") ?? false,
                otherModifiersHeld: OptionalBool(step, "modifiersHeld") ?? false),
            "mouse" => KeyboardInput.Mouse,
            _ => throw new InvalidDataException($"不明な入力です: {kind}"),
        };
    }

    private static int KeyOf(JsonElement step) => VirtualKey(step.GetProperty("key").GetString());

    private static int VirtualKey(string? name) =>
        name is not null && Vectors.GetProperty("keys").TryGetProperty(name, out var keyCodes)
            ? keyCodes.GetProperty("windows").GetInt32()
            : throw new InvalidDataException($"不明なキーです: {name}");

    private static bool? OptionalBool(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) ? value.GetBoolean() : null;

    private static JsonElement FindScenario(string group, string name) =>
        Vectors.GetProperty(group).EnumerateArray().Single(scenario => scenario.GetProperty("name").GetString() == name);

    private static TheoryData<string> ScenarioNames(string group) =>
        new(Vectors.GetProperty(group).EnumerateArray().Select(scenario => scenario.GetProperty("name").GetString()!));

    // テストのプロジェクトがビルド時に出力先へコピーする（PunctuationToggle.Tests.csproj を参照）
    private static JsonElement LoadVectors()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "testdata", "trigger_key_vectors.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }
}
