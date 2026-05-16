import Foundation

enum AppLog {
    static func info(_ area: String, _ message: String) {
        write("INFO", area, message)
    }

    static func error(_ area: String, _ error: Error) {
        write("ERROR", area, error.localizedDescription)
    }

    private static func write(_ level: String, _ area: String, _ message: String) {
        do {
            try AppPaths.ensure()
            let line = "\(Date().formatted(.iso8601)) [\(level)] \(area): \(message)\n"
            if FileManager.default.fileExists(atPath: AppPaths.logURL.path) {
                let handle = try FileHandle(forWritingTo: AppPaths.logURL)
                handle.seekToEndOfFile()
                handle.write(Data(line.utf8))
                handle.closeFile()
            } else {
                try line.write(to: AppPaths.logURL, atomically: true, encoding: .utf8)
            }
        } catch {
            // Logging must never break dictation.
        }
    }
}
