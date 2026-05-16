import AppKit
import SwiftUI

struct SettingsView: View {
    @ObservedObject var state: AppState

    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            header
            settingsGrid
            transcriptPanel
        }
        .padding(24)
        .background(Color(red: 0.97, green: 0.98, blue: 0.99))
        .frame(minWidth: 520, minHeight: 420)
    }

    // MARK: - Header

    private var header: some View {
        HStack(alignment: .center) {
            VStack(alignment: .leading, spacing: 4) {
                Text("Wisp2Mac")
                    .font(.system(size: 30, weight: .semibold))
                Text(state.status)
                    .foregroundStyle(.secondary)
            }
            Spacer()
            Button(state.isRecording ? "Остановить и вставить" : "Начать диктовку") {
                Task { await state.toggleDictation() }
            }
            .controlSize(.large)
            .disabled(state.isBusy)
        }
    }

    // MARK: - Settings grid

    private var settingsGrid: some View {
        VStack(spacing: 14) {
            HStack(spacing: 16) {
                field("Модель") {
                    Picker("", selection: binding(\.modelId)) {
                        ForEach(ModelProfile.all) { m in
                            Text(m.displayName).tag(m.id)
                        }
                    }
                    .labelsHidden()
                }

                field("Язык") {
                    Picker("", selection: binding(\.language)) {
                        Text("ru").tag("ru")
                        Text("en").tag("en")
                        Text("авто").tag("auto")
                    }
                    .labelsHidden()
                }
            }

            HStack(spacing: 16) {
                field("Способ вставки") {
                    Picker("", selection: binding(\.insertMethod)) {
                        ForEach(InsertMethod.allCases) { method in
                            Text(method.displayName).tag(method)
                        }
                    }
                    .labelsHidden()
                }

                field("Модель: скачать") {
                    HStack {
                        ProgressView(value: state.downloadProgress)
                        Button("Скачать") {
                            Task { await state.ensureModel() }
                        }
                    }
                }
            }

            HStack(spacing: 16) {
                field("Горячая клавиша") {
                    HotkeyRecorderButton(config: binding(\.hotkey))
                }

                VStack(alignment: .leading, spacing: 8) {
                    Text("Опции")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    HStack(spacing: 14) {
                        Toggle("Вставлять текст",    isOn: binding(\.pasteAfterTranscription))
                        Toggle("Улучшать текст",     isOn: binding(\.polishTranscript))
                        Toggle("Индикатор записи",   isOn: binding(\.showRecordingOverlay))
                    }
                    .toggleStyle(.checkbox)
                }
                .frame(maxWidth: .infinity, alignment: .leading)
            }

            HStack {
                Image(systemName: "keyboard")
                    .foregroundStyle(.blue)
                Text("Нажмите кнопку горячей клавиши, затем нажмите нужное сочетание")
                    .foregroundStyle(.secondary)
                    .font(.caption)
                Spacer()
                Button("Логи") {
                    NSWorkspace.shared.open(AppPaths.logsDirectory)
                }
                .controlSize(.small)
            }
            .padding(10)
            .background(Color.blue.opacity(0.07))
            .clipShape(RoundedRectangle(cornerRadius: 8))
        }
        .padding(16)
        .background(.white)
        .clipShape(RoundedRectangle(cornerRadius: 8))
        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.black.opacity(0.10)))
    }

    // MARK: - Transcript panel

    private var transcriptPanel: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Последняя расшифровка")
                .font(.headline)
            TextEditor(text: Binding(
                get: { state.lastTranscript },
                set: { state.lastTranscript = $0 }
            ))
            .font(.body)
            .scrollContentBackground(.hidden)
            .frame(minHeight: 80)
        }
        .padding(16)
        .background(.white)
        .clipShape(RoundedRectangle(cornerRadius: 8))
        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.black.opacity(0.10)))
    }

    // MARK: - Helpers

    private func field<Content: View>(_ title: String, @ViewBuilder content: () -> Content) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(title)
                .font(.caption)
                .foregroundStyle(.secondary)
            content()
                .frame(maxWidth: .infinity, alignment: .leading)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }

    private func binding<V>(_ kp: WritableKeyPath<AppSettings, V>) -> Binding<V> {
        Binding(
            get: { state.settings[keyPath: kp] },
            set: { state.settings[keyPath: kp] = $0; state.saveSettings() }
        )
    }
}

// MARK: - HotkeyRecorderButton

struct HotkeyRecorderButton: View {
    @Binding var config: HotkeyConfig
    @State private var isRecording = false
    @State private var localMonitor: Any?

    var body: some View {
        Button(isRecording ? "Нажмите клавишу…" : config.displayString) {
            if isRecording { stopRecording() } else { startRecording() }
        }
        .buttonStyle(.bordered)
        .foregroundStyle(isRecording ? Color.orange : Color.primary)
        .onDisappear { stopRecording() }
    }

    private func startRecording() {
        isRecording = true
        localMonitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { event in
            // Escape cancels
            if event.keyCode == 53 {
                stopRecording()
                return nil
            }
            let mods = event.modifierFlags.intersection([.command, .shift, .control, .option])
            guard !mods.isEmpty else { return event }
            config = HotkeyConfig(keyCode: event.keyCode, modifierRaw: mods.rawValue)
            stopRecording()
            return nil
        }
    }

    private func stopRecording() {
        isRecording = false
        if let m = localMonitor { NSEvent.removeMonitor(m); localMonitor = nil }
    }
}
