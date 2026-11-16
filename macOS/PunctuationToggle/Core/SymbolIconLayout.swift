/// 句読点の2文字をメニューバーの画像に描くときの、倍率と縦の位置の決め方。
///
/// 句読点は組み合わせごとに字形の大きさが違う（「，．」は「、。」より小さい）ため、同じ倍率で描くと、
/// 切り替えたときに大きさが変わって見える（#322）。そのため、文字の輪郭の範囲の面積が、どの組み合わせでも
/// 同じになる倍率で描く（Windows 版の `TrayIconRenderer` と同じ決め方）。
nonisolated enum SymbolIconLayout {
    /// 文字の輪郭を囲む範囲。縦は上向きが正（ベースラインが 0）
    struct OutlineBounds: Equatable, Sendable {
        var width: Double
        var bottom: Double
        var top: Double

        var height: Double { top - bottom }
        var area: Double { width * height }
    }

    /// 輪郭を描くときの倍率と、画像の縦の中心に置く輪郭上の位置
    struct Placement: Equatable, Sendable {
        var scale: Double
        var centerY: Double
    }

    /// - Parameters:
    ///   - outline: 描く文字の輪郭の範囲
    ///   - references: 大きさをそろえる相手（すべての句読点の組み合わせ）の輪郭の範囲
    ///   - available: 画像の中で文字を描ける幅と高さ
    ///
    /// 基準の面積は、すべての組み合わせを同じ倍率で画像に収めたときの、最も広い組み合わせ（「、。」）の面積。
    /// 面積をそろえても収まらない場合は、収まる大きさに縮める。縦の位置は、すべての組み合わせを重ねた範囲の中央を
    /// 画像の中央に置き、句読点の高さをそろえる。
    static func placement(of outline: OutlineBounds, references: [OutlineBounds], available: Double) -> Placement {
        let top = (references.map(\.top) + [outline.top]).max() ?? outline.top
        let bottom = (references.map(\.bottom) + [outline.bottom]).min() ?? outline.bottom
        let fittingScale = min(available / outline.width, available / (top - bottom))
        let centerY = (top + bottom) / 2

        guard
            let referenceWidth = references.map(\.width).max(),
            let referenceTop = references.map(\.top).max(),
            let referenceBottom = references.map(\.bottom).min(),
            let largestReferenceArea = references.map(\.area).max()
        else {
            return Placement(scale: fittingScale, centerY: centerY)
        }

        let commonScale = min(available / referenceWidth, available / (referenceTop - referenceBottom))
        let referenceArea = largestReferenceArea * commonScale * commonScale
        let areaScale = (referenceArea / outline.area).squareRoot()
        return Placement(scale: min(areaScale, fittingScale), centerY: centerY)
    }
}
