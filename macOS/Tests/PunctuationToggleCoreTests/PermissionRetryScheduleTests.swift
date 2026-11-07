import Testing
@testable import PunctuationToggleCore

@Suite("アクセシビリティの許可を待つ間隔")
struct PermissionRetryScheduleTests {
    @Test("最初の1分は1秒ごと", arguments: [0, 1, 30, 59.9])
    func frequentAtFirst(elapsed: Double) {
        #expect(PermissionRetrySchedule.interval(elapsedSinceWaitStart: elapsed) == 1)
    }

    @Test("1分を過ぎたら10秒ごと", arguments: [60, 60.1, 600, 86_400])
    func slowAfterOneMinute(elapsed: Double) {
        #expect(PermissionRetrySchedule.interval(elapsedSinceWaitStart: elapsed) == 10)
    }
}
