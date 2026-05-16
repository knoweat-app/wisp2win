import Foundation

final class WhisperTranscriber {
    func transcribe(wavURL: URL, modelPath: URL, language: String) async throws -> String {
        guard FileManager.default.fileExists(atPath: modelPath.path) else {
            throw WispError("Модель не установлена")
        }

        guard let cliURL = Bundle.main.resourceURL?
            .appendingPathComponent("whisper", isDirectory: true)
            .appendingPathComponent("whisper-cli"),
            FileManager.default.fileExists(atPath: cliURL.path) else {
            throw WispError("Whisper engine для macOS еще не встроен в bundle")
        }

        AppLog.info("transcription", "Start wav=\(wavURL.path), model=\(modelPath.lastPathComponent), language=\(language)")
        let process = Process()
        let output = Pipe()
        process.executableURL = cliURL
        process.arguments = [
            "-m", modelPath.path,
            "-f", wavURL.path,
            "-l", normalizeLanguage(language),
            "-otxt",
            "-of", wavURL.deletingPathExtension().path
        ]
        process.standardOutput = output
        process.standardError = output
        try process.run()
        process.waitUntilExit()

        if process.terminationStatus != 0 {
            let data = output.fileHandleForReading.readDataToEndOfFile()
            let message = String(data: data, encoding: .utf8) ?? "unknown whisper error"
            throw WispError("Ошибка Whisper: \(message)")
        }

        let transcriptURL = wavURL.deletingPathExtension().appendingPathExtension("txt")
        let text = try String(contentsOf: transcriptURL, encoding: .utf8)
            .split { $0.isNewline }
            .joined(separator: " ")
            .trimmingCharacters(in: .whitespacesAndNewlines)

        AppLog.info("transcription", "Done chars=\(text.count)")
        return text
    }

    private func normalizeLanguage(_ language: String) -> String {
        switch language.lowercased() {
        case "ru", "russian": "ru"
        case "en", "english": "en"
        default: "auto"
        }
    }
}
