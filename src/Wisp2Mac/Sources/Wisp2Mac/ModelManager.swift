import Foundation

final class ModelManager {
    func path(for model: ModelProfile) -> URL {
        AppPaths.modelsDirectory.appendingPathComponent(model.fileName)
    }

    func isInstalled(_ model: ModelProfile) -> Bool {
        let url = path(for: model)
        guard FileManager.default.fileExists(atPath: url.path) else { return false }
        // Reject truncated downloads: file must be at least 80% of expected size
        let attrs = try? FileManager.default.attributesOfItem(atPath: url.path)
        let bytes = (attrs?[.size] as? Int64) ?? 0
        let minBytes = model.approxBytes * 8 / 10
        if bytes < minBytes {
            AppLog.warn("model", "\(model.id) file too small (\(bytes) < \(minBytes)), treating as missing")
            return false
        }
        return true
    }

    func ensureInstalled(_ model: ModelProfile, progress: @escaping (Double) -> Void) async throws {
        try AppPaths.ensure()
        if isInstalled(model) {
            progress(1)
            return
        }

        // Remove any partial download left from a previous attempt
        let destination = path(for: model)
        if FileManager.default.fileExists(atPath: destination.path) {
            try? FileManager.default.removeItem(at: destination)
        }

        AppLog.info("model", "Downloading \(model.id) from \(model.url)")

        let delegate = ProgressDelegate(expectedBytes: model.approxBytes, onProgress: progress)
        let session  = URLSession(configuration: .default, delegate: delegate, delegateQueue: nil)
        defer { session.invalidateAndCancel() }

        let (tempURL, response) = try await session.download(from: model.url)
        guard let http = response as? HTTPURLResponse, 200..<300 ~= http.statusCode else {
            throw WispError("Не удалось скачать модель (HTTP \((response as? HTTPURLResponse)?.statusCode ?? 0))")
        }

        try FileManager.default.moveItem(at: tempURL, to: destination)
        progress(1)
        AppLog.info("model", "Installed \(model.id)")
    }
}

// MARK: - Progress delegate

private final class ProgressDelegate: NSObject, URLSessionDownloadDelegate {
    private let expectedBytes: Int64
    private let onProgress: (Double) -> Void

    init(expectedBytes: Int64, onProgress: @escaping (Double) -> Void) {
        self.expectedBytes = expectedBytes
        self.onProgress = onProgress
    }

    func urlSession(_ session: URLSession,
                    downloadTask: URLSessionDownloadTask,
                    didWriteData bytesWritten: Int64,
                    totalBytesWritten: Int64,
                    totalBytesExpectedToWrite: Int64) {
        let total = totalBytesExpectedToWrite > 0 ? totalBytesExpectedToWrite : expectedBytes
        guard total > 0 else { return }
        onProgress(Double(totalBytesWritten) / Double(total))
    }

    func urlSession(_ session: URLSession, downloadTask: URLSessionDownloadTask,
                    didFinishDownloadingTo location: URL) {}
}
