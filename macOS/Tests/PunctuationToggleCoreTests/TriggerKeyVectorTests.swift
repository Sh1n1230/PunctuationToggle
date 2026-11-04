import Foundation
import Testing
@testable import PunctuationToggleCore

/// Windows 版と共通のテストベクタ（リポジトリのルートの testdata/trigger_key_vectors.json）で、
/// 切り替えキーの判定と読み取りを確かめる。ベクタの書き方は JSON の format を参照。
@Suite("両 OS に共通のテストベクタ")
struct TriggerKeyVectorTests {
    @Test("切り替えキーの判定", arguments: TriggerKeyVectors.shared.detector)
    func detector(scenario: TriggerKeyVectors.Scenario) throws {
        let vectors = TriggerKeyVectors.shared
        let triggerKeyCode = try vectors.keyCode(scenario.trigger ?? "")
        var detector = TriggerKeyDetector(triggerKey: try #require(TriggerKey(keyCode: triggerKeyCode)))

        for (index, step) in scenario.inputs.enumerated() {
            // macOS では修飾キーのリピートは届かない
            guard let input = try vectors.keyboardInput(step) else { continue }
            let decision = detector.handle(input)
            guard let expected = step.expect else { continue }
            #expect(
                decision
                    == InputDecision(
                        shouldToggle: expected.toggle ?? false,
                        shouldSuppress: expected.suppress ?? false
                    ),
                "\(index + 1) 番目の入力（\(step.input)）"
            )
        }
    }

    @Test("設定画面でのキーの読み取り", arguments: TriggerKeyVectors.shared.capture)
    func capture(scenario: TriggerKeyVectors.Scenario) throws {
        let vectors = TriggerKeyVectors.shared
        var capture = TriggerKeyCapture()

        for (index, step) in scenario.inputs.enumerated() {
            guard let input = try vectors.keyboardInput(step) else { continue }
            let result = step.active == false ? capture.handleWhileInactive(input) : capture.handle(input)
            guard let expected = step.expect else { continue }

            let expectedOutcome: TriggerKeyCapture.Outcome
            switch expected.outcome {
            case "waiting":
                expectedOutcome = .waiting
            case "captured":
                expectedOutcome = .captured(try #require(TriggerKey(keyCode: try vectors.keyCode(expected.key ?? ""))))
            case "rejected":
                expectedOutcome = .rejected(keyCode: try vectors.keyCode(expected.key ?? ""))
            case "cancelled":
                expectedOutcome = .cancelled
            default:
                Issue.record("不明な outcome です: \(expected.outcome ?? "（なし）")")
                continue
            }
            #expect(result.outcome == expectedOutcome, "\(index + 1) 番目の入力（\(step.input)）")
            if let suppress = expected.suppress {
                #expect(result.shouldSuppress == suppress, "\(index + 1) 番目の入力（\(step.input)）")
            }
        }
    }
}

/// testdata/trigger_key_vectors.json の内容
struct TriggerKeyVectors: Decodable, Sendable {
    struct KeyCodes: Decodable, Sendable {
        let macOS: UInt16
    }

    struct Expectation: Decodable, Sendable {
        let toggle: Bool?
        let suppress: Bool?
        let outcome: String?
        let key: String?
    }

    struct Step: Decodable, Sendable {
        let input: String
        let key: String?
        let otherModifiersHeld: Bool?
        let isRepeat: Bool?
        let modifiersHeld: Bool?
        let active: Bool?
        let expect: Expectation?
    }

    struct Scenario: Decodable, Sendable, CustomTestStringConvertible {
        let name: String
        let trigger: String?
        let inputs: [Step]

        var testDescription: String { name }
    }

    struct VectorError: Error, CustomStringConvertible {
        let description: String
    }

    let keys: [String: KeyCodes]
    let detector: [Scenario]
    let capture: [Scenario]

    /// テストのソースの場所からたどって読み込む（swift test はリポジトリの中で実行する）
    static let shared: TriggerKeyVectors = {
        let url = URL(fileURLWithPath: #filePath)
            .deletingLastPathComponent()
            .appendingPathComponent("../../../testdata/trigger_key_vectors.json")
            .standardizedFileURL
        do {
            return try JSONDecoder().decode(TriggerKeyVectors.self, from: Data(contentsOf: url))
        } catch {
            fatalError("テストベクタを読み込めませんでした（\(url.path)）: \(error)")
        }
    }()

    func keyCode(_ name: String) throws -> UInt16 {
        guard let keyCodes = keys[name] else {
            throw VectorError(description: "不明なキーです: \(name)")
        }
        return keyCodes.macOS
    }

    /// ベクタの入力を macOS の入力にする。macOS に届かない入力（修飾キーのリピート）では nil。
    func keyboardInput(_ step: Step) throws -> KeyboardInput? {
        switch step.input {
        case "modifierDown", "modifierUp":
            return .modifierChanged(
                keyCode: try keyCode(step.key ?? ""),
                isPressed: step.input == "modifierDown",
                otherModifiersHeld: step.otherModifiersHeld ?? false
            )
        case "modifierRepeat":
            return nil
        case "keyDown":
            return .keyDown(
                keyCode: try keyCode(step.key ?? ""),
                isRepeat: step.isRepeat ?? false,
                modifiersHeld: step.modifiersHeld ?? false
            )
        case "keyUp":
            return .keyUp(keyCode: try keyCode(step.key ?? ""))
        case "mouse":
            return .mouse
        default:
            throw VectorError(description: "不明な入力です: \(step.input)")
        }
    }
}
