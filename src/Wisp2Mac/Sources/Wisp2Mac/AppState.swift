import AppKit
import Foundation

@MainActor
final class AppState: ObservableObject {
    @Published var settings: AppSettings
    @Published var status = "Готово"
    @Published var isRecording = false
    @Published var isBusy = false
    @Published var downloadProgress = 0.0
    @Published var lastTranscript = ""
    @Published var isModelInstalled = false

    let modelManager = ModelManager()
    let overlay = RecordingOverlayController()

    private let settingsStore = SettingsStore()
    private let recorder = AudioRecorder()
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
        let model = ModelProfile.byId(settings.modelId)
        if modelManager.isInstalled(model) {
            downloadProgress = 1
            return
        }

        status = "Загрузка модели \(model.displayName)"
        do {
            try await modelManager.ensureInstalled(model) { progress in
                Task { @MainActor in
                    self.downloadProgress = progress
                }
            }
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

            var text = try await transcriber.transcribe(
                wavURL: wavURL,
                modelPath: modelManager.path(for: ModelProfile.byId(settings.modelId)),
                language: settings.language
            )

            if settings.polishTranscript {
                text = postProcessor.polish(text, language: settings.language)
            }

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

    private func resolveInsertMethod() -> InsertMethod {
        guard settings.insertMethod == .auto else { return settings.insertMethod }
        return .paste
    }
}
