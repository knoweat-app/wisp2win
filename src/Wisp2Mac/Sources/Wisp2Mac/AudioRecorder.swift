import AVFoundation
import Foundation

final class AudioRecorder: NSObject {
    private var recorder: AVAudioRecorder?
    private var url: URL?

    func start() throws -> URL {
        try AppPaths.ensure()
        let fileURL = AppPaths.tempDirectory.appendingPathComponent("dictation-\(Int(Date().timeIntervalSince1970)).wav")
        let settings: [String: Any] = [
            AVFormatIDKey: kAudioFormatLinearPCM,
            AVSampleRateKey: 16_000,
            AVNumberOfChannelsKey: 1,
            AVLinearPCMBitDepthKey: 16,
            AVLinearPCMIsFloatKey: false,
            AVLinearPCMIsBigEndianKey: false
        ]

        let recorder = try AVAudioRecorder(url: fileURL, settings: settings)
        recorder.prepareToRecord()
        guard recorder.record() else {
            throw WispError("Не удалось начать запись")
        }

        self.recorder = recorder
        self.url = fileURL
        AppLog.info("recording", "Started \(fileURL.path)")
        return fileURL
    }

    func stop() throws -> URL {
        guard let recorder, let url else {
            throw WispError("Запись не активна")
        }

        recorder.stop()
        self.recorder = nil
        self.url = nil
        AppLog.info("recording", "Stopped \(url.path)")
        return url
    }
}
