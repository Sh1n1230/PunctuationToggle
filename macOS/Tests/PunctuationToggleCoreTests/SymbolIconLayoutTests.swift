import Testing
@testable import PunctuationToggleCore

@Suite("メニューバーの画像に描く句読点の大きさ")
struct SymbolIconLayoutTests {
    private typealias OutlineBounds = SymbolIconLayout.OutlineBounds

    /// メニューバーのフォント（ヒラギノ角ゴシック W4、13 ポイント）で測った、4つの組み合わせの輪郭の範囲
    nonisolated private static let references: [OutlineBounds] = [
        OutlineBounds(width: 8.86, bottom: -0.84, top: 2.90),  // 、。
        OutlineBounds(width: 7.26, bottom: -1.42, top: 2.90),  // ，。
        OutlineBounds(width: 6.34, bottom: -0.95, top: 2.51),  // 、．
        OutlineBounds(width: 4.21, bottom: -1.42, top: 1.55),  // ，．
    ]

    nonisolated private static let available = 16.0

    /// 計算の誤差の分の許容範囲
    nonisolated private static let tolerance = 1e-9

    @Test("句読点の組み合わせが違っても、描いた文字の範囲の面積がそろう", arguments: references)
    private func sameArea(outline: OutlineBounds) {
        let placement = SymbolIconLayout.placement(of: outline, references: Self.references, available: Self.available)
        let largest = Self.references[0]
        let largestPlacement = SymbolIconLayout.placement(
            of: largest, references: Self.references, available: Self.available)

        let area = outline.area * placement.scale * placement.scale
        let largestArea = largest.area * largestPlacement.scale * largestPlacement.scale
        #expect(abs(area - largestArea) < Self.tolerance)
    }

    @Test("字形の小さい組み合わせほど、大きい倍率で描く")
    func smallerOutlineIsDrawnLarger() {
        let scales = Self.references.map { outline in
            SymbolIconLayout.placement(of: outline, references: Self.references, available: Self.available).scale
        }
        #expect(scales == scales.sorted())
        #expect(scales[0] < scales[3])
    }

    @Test("最も広い組み合わせは、描ける幅いっぱいに描く")
    func largestFillsAvailableWidth() {
        let placement = SymbolIconLayout.placement(
            of: Self.references[0], references: Self.references, available: Self.available)
        #expect(abs(Self.references[0].width * placement.scale - Self.available) < Self.tolerance)
    }

    @Test("どの組み合わせも、描ける範囲に収まる", arguments: references)
    private func fitsInAvailable(outline: OutlineBounds) {
        let placement = SymbolIconLayout.placement(of: outline, references: Self.references, available: Self.available)
        #expect(outline.width * placement.scale <= Self.available + Self.tolerance)
        #expect((outline.top - placement.centerY) * placement.scale <= Self.available / 2 + Self.tolerance)
        #expect((placement.centerY - outline.bottom) * placement.scale <= Self.available / 2 + Self.tolerance)
    }

    @Test("縦の位置は、すべての組み合わせを重ねた範囲の中央でそろえる", arguments: references)
    private func commonVerticalCenter(outline: OutlineBounds) {
        let placement = SymbolIconLayout.placement(of: outline, references: Self.references, available: Self.available)
        #expect(abs(placement.centerY - (2.90 + -1.42) / 2) < Self.tolerance)
    }

    @Test("面積をそろえると収まらない文字は、収まる大きさに縮める")
    func oversizedOutlineIsShrunkToFit() {
        // 「？」のように、細くて背の高い文字
        let tall = OutlineBounds(width: 4, bottom: 0, top: 10)
        let placement = SymbolIconLayout.placement(of: tall, references: Self.references, available: Self.available)
        let top = 10.0
        let bottom = -1.42
        #expect(abs(placement.scale - Self.available / (top - bottom)) < Self.tolerance)
        #expect(abs(placement.centerY - (top + bottom) / 2) < Self.tolerance)
    }

    @Test("そろえる相手が無いときは、描ける範囲いっぱいに描く")
    func withoutReferences() {
        let outline = OutlineBounds(width: 8, bottom: -1, top: 3)
        let placement = SymbolIconLayout.placement(of: outline, references: [], available: Self.available)
        #expect(placement == SymbolIconLayout.Placement(scale: 2, centerY: 1))
    }
}
