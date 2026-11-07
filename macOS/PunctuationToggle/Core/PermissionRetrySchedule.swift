/// アクセシビリティの許可を待つ間に、許可されたかを確かめる間隔の決め方。
///
/// 許可した直後にすぐ反映されるよう、待ち始めてしばらくは短い間隔で確かめる。
/// 許可しないまま常駐させた場合に、短い間隔での起床を続けないよう、その後は間隔を延ばす
/// （メニューを開いたときにはすぐ確かめるので、延ばした後も許可はすぐ反映できる）。
nonisolated enum PermissionRetrySchedule {
    /// 待ち始めてからの、短い間隔で確かめる時間（秒）
    static let frequentDuration: Double = 60

    /// `frequentDuration` までの間隔（秒）
    static let frequentInterval: Double = 1

    /// `frequentDuration` を過ぎた後の間隔（秒）
    static let slowInterval: Double = 10

    /// 待ち始めてからの経過時間（秒）から、次に確かめるまでの間隔（秒）を返す
    static func interval(elapsedSinceWaitStart elapsed: Double) -> Double {
        elapsed < frequentDuration ? frequentInterval : slowInterval
    }
}
