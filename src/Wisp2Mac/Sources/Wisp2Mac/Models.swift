import Foundation

struct WispError: LocalizedError {
    let message: String

    init(_ message: String) {
        self.message = message
    }

    var errorDescription: String? { message }
}

struct ModelProfile: Identifiable, Codable, Hashable {
    let id: String
    let displayName: String
    let fileName: String
    let url: URL
    let approxBytes: Int64

    static let all: [ModelProfile] = [
        .init(id: "tiny", displayName: "Быстрая", fileName: "ggml-tiny.bin", url: URL(string: "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin")!, approxBytes: 78 * 1024 * 1024),
        .init(id: "base", displayName: "Сбалансированная", fileName: "ggml-base.bin", url: URL(string: "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin")!, approxBytes: 148 * 1024 * 1024),
        .init(id: "small", displayName: "Точная", fileName: "ggml-small.bin", url: URL(string: "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin")!, approxBytes: 488 * 1024 * 1024),
        .init(id: "medium", displayName: "Тяжелая", fileName: "ggml-medium.bin", url: URL(string: "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin")!, approxBytes: 1533 * 1024 * 1024)
    ]

    static func byId(_ id: String) -> ModelProfile {
        all.first { $0.id == id } ?? all[1]
    }
}

enum InsertMethod: String, Codable, CaseIterable, Identifiable {
    case auto
    case paste
    case typeText

    var id: String { rawValue }

    var displayName: String {
        switch self {
        case .auto: "Авто"
        case .paste: "Cmd+V"
        case .typeText: "Печатать текст"
        }
    }
}

struct AppSettings: Codable {
    var modelId = "base"
    var language = "ru"
    var insertMethod = InsertMethod.auto
    var pasteAfterTranscription = true
    var polishTranscript = true
    var showRecordingOverlay = true
}

struct TargetApplication {
    let processIdentifier: pid_t
    let bundleIdentifier: String
    let localizedName: String

    var isTermius: Bool {
        bundleIdentifier.localizedCaseInsensitiveContains("termius")
            || localizedName.localizedCaseInsensitiveContains("Termius")
    }

    var isTerminalLike: Bool {
        let name = localizedName.lowercased()
        let bundle = bundleIdentifier.lowercased()
        return isTermius
            || name.contains("terminal")
            || name.contains("iterm")
            || name.contains("warp")
            || bundle.contains("terminal")
            || bundle.contains("iterm")
            || bundle.contains("warp")
    }
}
