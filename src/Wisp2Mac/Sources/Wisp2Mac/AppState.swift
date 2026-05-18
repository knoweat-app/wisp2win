import AppKit
import Foundation

@MainActor
final class AppState: ObservableObject {
    @Published var settings: AppSettings
    @Published var status = "Готово"
    @Published var isRecording = false
    @Published var isBusy = false
    @Published var isDownloadingModel = false
    @Published var downloadProgress = 0.0
    @Published var lastTranscript = ""
    @Published var isModelInstalled = false

    let modelManager = ModelManager()
    let overlay = RecordingOverlayController()

    private let settingsStore = SettingsStore()
    private let recorder = AudioRecorder()
    private let audioFileConverter = AudioFileConverter()
    private let transcriber = WhisperTranscriber()
    private let postProcessor = TranscriptPostProcessor()
    private let activeAppTracker = ActiveAppTracker()
    private let insertion = TextInsertionService()
    private var recordingURL: URL?
    private var targetApp: TargetApplication?

    init() {
        settings = settingsStore.load()
        refreshModelInstalled()
    }

    func saveSettings() {
        settingsStore.save(settings)
        refreshModelInstalled()
    }

    func refreshModelInstalled() {
        isModelInstalled = modelManager.isInstalled(ModelProfile.byId(settings.modelId))
    }

    func ensureModel() async {
        if isDownloadingModel {
            return
        }

        let model = ModelProfile.byId(settings.modelId)
        if modelManager.isInstalled(model) {
            downloadProgress = 1
            isModelInstalled = true
            return
        }

        isDownloadingModel = true
        downloadProgress = 0
        status = "Загрузка модели \(model.displayName)"
        defer {
            isDownloadingModel = false
        }

        do {
            try await modelManager.ensureInstalled(model) { progress in
                Task { @MainActor in
                    self.downloadProgress = min(max(progress, 0), 1)
                }
            }
            downloadProgress = 1
            refreshModelInstalled()
            status = "Готово"
        } catch {
            status = error.localizedDescription
            AppLog.error("model", error)
        }
    }

    func toggleDictation() async {
        if isBusy {
            return
        }

        if isRecording {
            await stopAndTranscribe()
        } else {
            startRecording()
        }
    }

    func importAudioFile(_ sourceURL: URL) async {
        if isRecording || isBusy {
            return
        }

        isBusy = true
        status = "Подготовка аудиофайла"
        var wavURL: URL?
        defer {
            if let wavURL {
                try? FileManager.default.removeItem(at: wavURL)
                try? FileManager.default.removeItem(at: wavURL.deletingPathExtension().appendingPathExtension("txt"))
            }
            isBusy = false
        }

        do {
            await ensureModel()
            try ensureSelectedModelInstalled()
            status = "Подготовка аудиофайла"
            wavURL = try audioFileConverter.convertToWhisperWav(sourceURL: sourceURL)
            guard let wavURL else {
                throw WispError("Не удалось подготовить аудиофайл")
            }
            status = "Распознавание файла"

            let text = try await transcribeCurrentSettings(wavURL: wavURL)
            refreshModelInstalled()
            lastTranscript = text
            status = text.isEmpty ? "Речь не обнаружена" : "Файл расшифрован"
        } catch {
            status = error.localizedDescription
            AppLog.error("import", error)
        }
    }

    func exportTranscript(to destinationURL: URL) throws {
        let text = lastTranscript.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !text.isEmpty else {
            throw WispError("Нет текста для экспорта")
        }

        try text.write(to: destinationURL, atomically: true, encoding: .utf8)
        status = "TXT сохранен"
    }

    private func startRecording() {
        do {
            targetApp = activeAppTracker.capture()
            recordingURL = try recorder.start()
            isRecording = true
            status = "Идет запись"
            if settings.showRecordingOverlay {
                overlay.show()
            }
        } catch {
            status = error.localizedDescription
            AppLog.error("recording", error)
        }
    }

    private func stopAndTranscribe() async {
        isRecording = false
        overlay.hide()
        isBusy = true
        status = "Распознавание"

        do {
            guard recordingURL != nil else {
                throw WispError("Нет активной записи")
            }

            let wavURL = try recorder.stop()
            self.recordingURL = nil
            await ensureModel()
            try ensureSelectedModelInstalled()

            let text = try await transcribeCurrentSettings(wavURL: wavURL)

            refreshModelInstalled()
            lastTranscript = text
            if settings.pasteAfterTranscription && !text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
                status = "Вставка"
                try insertion.insert(text, method: resolveInsertMethod(), target: targetApp)
            }

            try? FileManager.default.removeItem(at: wavURL)
            status = text.isEmpty ? "Речь не обнаружена" : "Готово"
        } catch {
            status = error.localizedDescription
            AppLog.error("dictation", error)
        }

        isBusy = false
    }

    private func transcribeCurrentSettings(wavURL: URL) async throws -> String {
        var text = try await transcriber.transcribe(
            wavURL: wavURL,
            modelPath: modelManager.path(for: ModelProfile.byId(settings.modelId)),
            language: settings.language
        )

        if settings.polishTranscript {
            text = postProcessor.polish(text, language: settings.language)
        }

        return text
    }

    private func ensureSelectedModelInstalled() throws {
        let model = ModelProfile.byId(settings.modelId)
        guard modelManager.isInstalled(model) else {
            throw WispError("Модель \(model.displayName) не установлена")
        }
    }

    private func resolveInsertMethod() -> InsertMethod {
        guard settings.insertMethod == .auto else { return settings.insertMethod }
        return .paste
    }
}
