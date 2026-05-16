import SwiftUI

struct SettingsView: View {
    @ObservedObject var state: AppState

    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            header
            settingsGrid
            statusPanel
            transcriptPanel
        }
        .padding(24)
        .background(Color(red: 0.97, green: 0.98, blue: 0.99))
    }

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

    private var settingsGrid: some View {
        VStack(spacing: 14) {
            HStack(spacing: 16) {
                field("Модель") {
                    Picker("", selection: binding(\.modelId)) {
                        ForEach(ModelProfile.all) { model in
                            Text(model.displayName).tag(model.id)
                        }
                    }
                    .labelsHidden()
                }

                field("Язык") {
                    Picker("", selection: binding(\.language)) {
                        Text("ru").tag("ru")
                        Text("en").tag("en")
                        Text("auto").tag("auto")
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

                VStack(alignment: .leading, spacing: 8) {
                    Text("Модель")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    HStack {
                        ProgressView(value: state.downloadProgress)
                        Button("Скачать") {
                            Task { await state.ensureModel() }
                        }
                    }
                }
            }

            HStack(spacing: 18) {
                Toggle("Вставлять в активное окно", isOn: binding(\.pasteAfterTranscription))
                Toggle("Улучшать текст", isOn: binding(\.polishTranscript))
                Toggle("Индикатор записи", isOn: binding(\.showRecordingOverlay))
            }
            .toggleStyle(.checkbox)
        }
        .padding(16)
        .background(.white)
        .clipShape(RoundedRectangle(cornerRadius: 8))
        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.black.opacity(0.10)))
    }

    private var statusPanel: some View {
        HStack {
            Text("Горячая клавиша: Ctrl+Shift+Space")
                .foregroundStyle(Color.blue)
            Spacer()
            Button("Логи") {
                NSWorkspace.shared.open(AppPaths.logsDirectory)
            }
        }
        .padding(12)
        .background(Color.blue.opacity(0.08))
        .clipShape(RoundedRectangle(cornerRadius: 8))
        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.blue.opacity(0.18)))
    }

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
        }
        .padding(16)
        .background(.white)
        .clipShape(RoundedRectangle(cornerRadius: 8))
        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.black.opacity(0.10)))
    }

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

    private func binding<Value>(_ keyPath: WritableKeyPath<AppSettings, Value>) -> Binding<Value> {
        Binding(
            get: { state.settings[keyPath: keyPath] },
            set: {
                state.settings[keyPath: keyPath] = $0
                state.saveSettings()
            }
        )
    }
}
