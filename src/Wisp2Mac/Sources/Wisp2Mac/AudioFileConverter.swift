import Foundation

final class AudioFileConverter {
    func convertToWhisperWav(sourceURL: URL) throws -> URL {
        guard FileManager.default.fileExists(atPath: sourceURL.path) else {
            throw WispError("Аудиофайл не найден")
        }

        try AppPaths.ensure()
        let outputURL = AppPaths.tempDirectory
            .appendingPathComponent("import-\(Int(Date().timeIntervalSince1970)).wav")

        let process = Process()
        let output = Pipe()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/afconvert")
        process.arguments = [
            sourceURL.path,
            outputURL.path,
            "-f", "WAVE",
            "-d", "LEI16@16000",
            "-c", "1"
        ]
        process.standardOutput = output
        process.standardError = output

        do {
            AppLog.info("import", "Converting audio source=\(sourceURL.path)")
            try process.run()
            process.waitUntilExit()
        } catch {
            try? FileManager.default.removeItem(at: outputURL)
            throw error
        }

        if process.terminationStatus != 0 {
            try? FileManager.default.removeItem(at: outputURL)
            let data = output.fileHandleForReading.readDataToEndOfFile()
            let message = String(data: data, encoding: .utf8) ?? "unknown afconvert error"
            throw WispError("Не удалось подготовить аудиофайл: \(message)")
        }

        AppLog.info("import", "Converted wav=\(outputURL.path)")
        return outputURL
    }
}
