import Foundation
import Testing
@testable import PunctuationToggleCore

@Suite("二重起動の判定")
struct RunningInstanceTests {
    private static let earlier = Date(timeIntervalSinceReferenceDate: 1_000)
    private static let later = Date(timeIntervalSinceReferenceDate: 2_000)

    private static func instance(_ launchDate: Date?, pid: Int32, isTerminated: Bool = false) -> RunningInstance {
        RunningInstance(launchDate: launchDate, processIdentifier: pid, isTerminated: isTerminated)
    }

    @Test("先に起動したものがあれば、それを返す")
    func earlierInstanceExists() {
        let current = Self.instance(Self.later, pid: 200)
        let other = Self.instance(Self.earlier, pid: 100)
        #expect(current.earliestEarlierInstance(among: [current, other]) == other)
    }

    @Test("後から起動したものしか無ければ nil")
    func onlyLaterInstances() {
        let current = Self.instance(Self.earlier, pid: 100)
        #expect(current.earliestEarlierInstance(among: [current, Self.instance(Self.later, pid: 200)]) == nil)
    }

    @Test("自分自身は数えない")
    func ignoresItself() {
        let current = Self.instance(Self.later, pid: 200)
        #expect(current.earliestEarlierInstance(among: [current]) == nil)
    }

    @Test("終了したものは数えない")
    func ignoresTerminated() {
        let current = Self.instance(Self.later, pid: 200)
        #expect(
            current.earliestEarlierInstance(among: [Self.instance(Self.earlier, pid: 100, isTerminated: true)]) == nil)
    }

    @Test("同じ時刻なら、プロセス ID の小さい方を先とみなし、どちらか一方だけが終わる")
    func sameLaunchDate() {
        let first = Self.instance(Self.earlier, pid: 100)
        let second = Self.instance(Self.earlier, pid: 200)
        #expect(second.earliestEarlierInstance(among: [first, second]) == first)
        #expect(first.earliestEarlierInstance(among: [first, second]) == nil)
    }

    @Test("時刻が分からないものは、最も遅く起動したとみなす")
    func unknownLaunchDate() {
        let unknown = Self.instance(nil, pid: 100)
        let known = Self.instance(Self.later, pid: 200)
        #expect(unknown.earliestEarlierInstance(among: [unknown, known]) == known)
        #expect(known.earliestEarlierInstance(among: [unknown, known]) == nil)
    }

    @Test("両方の時刻が分からなければ、プロセス ID で決まり、どちらか一方だけが終わる")
    func bothUnknown() {
        let first = Self.instance(nil, pid: 100)
        let second = Self.instance(nil, pid: 200)
        #expect(second.earliestEarlierInstance(among: [first, second]) == first)
        #expect(first.earliestEarlierInstance(among: [first, second]) == nil)
    }

    @Test("先に起動したものが複数あれば、最も先に起動したものを返す")
    func multipleEarlierInstances() {
        let current = Self.instance(Self.later, pid: 300)
        let earliest = Self.instance(Self.earlier, pid: 200)
        let middle = Self.instance(Date(timeIntervalSinceReferenceDate: 1_500), pid: 100)
        #expect(current.earliestEarlierInstance(among: [middle, earliest, current]) == earliest)
    }
}
