import Foundation

final class ModelManager {
    func path(for model: ModelProfile) -> URL {
        AppPaths.modelsDirectory.appendingPathComponent(model.fileName)
    }

    func isInstalled(_ model: ModelProfile) -> Bool {
        FileManager.default.fileExists(atPath: path(for: model).path)
    }

    func ensureInstalled(_ model: ModelProfile, progress: @escaping (Double) -> Void) async throws {
        try AppPaths.ensure()
        if isInstalled(model) {
            progress(1)
            return
        }

        AppLog.info("model", "Downloading \(model.id) from \(model.url.absoluteString)")
        let (tempURL, response) = try await URLSession.shared.download(from: model.url)
        guard let http = response as? HTTPURLResponse, 200..<300 ~= http.statusCode else {
            throw WispError("Не удалось скачать модель")
        }

        let destination = path(for: model)
        if FileManager.default.fileExists(atPath: destination.path) {
            try FileManager.default.removeItem(at: destination)
        }

        try FileManager.default.moveItem(at: tempURL, to: destination)
        progress(1)
        AppLog.info("model", "Installed \(model.id)")
    }
}
