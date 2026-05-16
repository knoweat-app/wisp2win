import AppKit
import Foundation

struct WispError: LocalizedError {
    let message: String
    init(_ message: String) { self.message = message }
    var errorDescription: String? { message }
}

// MARK: - HotkeyConfig

struct HotkeyConfig: Codable, Equatable {
    var keyCode: UInt16 = 49                                              // Space
    var modifierRaw: UInt = NSEvent.ModifierFlags([.control, .shift]).rawValue

    var modifiers: NSEvent.ModifierFlags { NSEvent.ModifierFlags(rawValue: modifierRaw) }

    var displayString: String {
        var parts: [String] = []
        if modifiers.contains(.control) { parts.append("⌃") }
        if modifiers.contains(.option)  { parts.append("⌥") }
        if modifiers.contains(.shift)   { parts.append("⇧") }
        if modifiers.contains(.command) { parts.append("⌘") }
        parts.append(Self.keyName(for: keyCode))
        return parts.joined()
    }

    static func keyName(for keyCode: UInt16) -> String {
        let map: [UInt16: String] = [
            // Special
            49: "Space", 36: "↩", 48: "⇥", 51: "⌫", 53: "Esc",
            // Function
            122: "F1", 120: "F2", 99: "F3", 118: "F4", 96: "F5",
            97: "F6", 98: "F7", 100: "F8", 101: "F9", 109: "F10",
            103: "F11", 111: "F12",
            // Numbers row
            18: "1", 19: "2", 20: "3", 21: "4", 23: "5",
            22: "6", 26: "7", 28: "8", 25: "9", 29: "0",
            27: "-", 24: "=",
            // Letters (US QWERTY keycodes)
            0: "A", 11: "B", 8: "C", 2: "D", 14: "E", 3: "F",
            5: "G", 4: "H", 34: "I", 38: "J", 40: "K", 37: "L",
            46: "M", 45: "N", 31: "O", 35: "P", 12: "Q", 15: "R",
            1: "S", 17: "T", 32: "U", 9: "V", 13: "W", 7: "X",
            16: "Y", 6: "Z",
            // Punctuation
            41: ";", 39: "'", 43: ",", 47: ".", 44: "/",
            42: "\\", 33: "[", 30: "]", 50: "`",
        ]
        return map[keyCode] ?? "(\(keyCode))"
    }
}

// MARK: - ModelProfile

struct ModelProfile: Identifiable, Codable, Hashable {
    let id: String
    let displayName: String
    let fileName: String
    let url: URL
    let approxBytes: Int64

    static let all: [ModelProfile] = [
        .init(id: "tiny",   displayName: "Быстрая",        fileName: "ggml-tiny.bin",
              url: URL(string: "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin")!,
              approxBytes: 78   * 1024 * 1024),
        .init(id: "base",   displayName: "Сбалансированная", fileName: "ggml-base.bin",
              url: URL(string: "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin")!,
              approxBytes: 148  * 1024 * 1024),
        .init(id: "small",  displayName: "Точная",          fileName: "ggml-small.bin",
              url: URL(string: "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin")!,
              approxBytes: 488  * 1024 * 1024),
        .init(id: "medium", displayName: "Тяжёлая",         fileName: "ggml-medium.bin",
              url: URL(string: "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin")!,
              approxBytes: 1533 * 1024 * 1024),
    ]

    static func byId(_ id: String) -> ModelProfile {
        all.first { $0.id == id } ?? all[1]
    }
}

// MARK: - InsertMethod

enum InsertMethod: String, Codable, CaseIterable, Identifiable {
    case auto, paste, typeText
    var id: String { rawValue }
    var displayName: String {
        switch self {
        case .auto:     "Авто"
        case .paste:    "Cmd+V"
        case .typeText: "Печатать текст"
        }
    }
}

// MARK: - AppSettings

struct AppSettings: Codable {
    var modelId              = "base"
    var language             = "ru"
    var hotkey               = HotkeyConfig()
    var insertMethod         = InsertMethod.auto
    var pasteAfterTranscription = true
    var polishTranscript     = true
    var showRecordingOverlay = true
}

// MARK: - TargetApplication

struct TargetApplication {
    let processIdentifier: pid_t
    let bundleIdentifier: String
    let localizedName: String

    var isTerminalLike: Bool {
        let name   = localizedName.lowercased()
        let bundle = bundleIdentifier.lowercased()
        return name.contains("terminal") || name.contains("iterm") || name.contains("warp")
            || name.contains("termius")  || bundle.contains("terminal") || bundle.contains("iterm")
            || bundle.contains("warp")   || bundle.contains("termius")
    }
}
