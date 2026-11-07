import Foundation

/// 二重起動の判定に使う、動いている同じアプリのプロセスの情報。
/// `NSRunningApplication` から取り出した値で作る（Core は AppKit を使わないため）。
nonisolated struct RunningInstance: Equatable, Sendable {
    /// 起動した時刻。LaunchServices を通さずに起動した場合（ターミナルから実行ファイルを直接起動したなど）は nil
    var launchDate: Date?
    var processIdentifier: Int32
    var isTerminated: Bool

    /// 起動した順に並べるための値。時刻が分からないものは最も遅く起動したとみなし、同じ時刻ならプロセス ID で決める。
    /// 同時に起動した2つが、互いに相手を先と判断して両方とも終わることのないよう、必ず順序が決まる値にする
    private var launchOrder: (Date, Int32) {
        (launchDate ?? .distantFuture, processIdentifier)
    }

    /// `others` のうち、自分より先に起動して動いているものを返す（複数ある場合は最も先に起動したもの）。
    /// 自分自身（同じプロセス ID）と終了したものは数えない。無ければ nil
    func earliestEarlierInstance(among others: [RunningInstance]) -> RunningInstance? {
        others
            .filter { other in
                other.processIdentifier != processIdentifier && !other.isTerminated && other.launchOrder < launchOrder
            }
            .min { first, second in first.launchOrder < second.launchOrder }
    }
}
