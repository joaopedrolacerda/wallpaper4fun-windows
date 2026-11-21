// Converte um vídeo para H.264 em 1080p, no máximo 30 fps, sem áudio: o formato que qualquer
// Windows toca com aceleração da placa de vídeo (HEVC precisa de extensão paga no Windows 10).
// Uso (no Mac, com Xcode): swift tools/transcode.swift <entrada> <saída.mp4> <bitrate em Mbps>
import AVFoundation

let args = CommandLine.arguments
let input = URL(fileURLWithPath: args[1])
let output = URL(fileURLWithPath: args[2])
let bitrate = Int(Double(args[3])! * 1_000_000)
let maxFPS: Float = 30
try? FileManager.default.removeItem(at: output)

let asset = AVURLAsset(url: input)
let track = try await asset.loadTracks(withMediaType: .video)[0]
let sourceFPS = try await track.load(.nominalFrameRate)
let frameStep = max(1, Int((sourceFPS / maxFPS).rounded()))  // 60 fps -> usa 1 de cada 2 quadros

let reader = try AVAssetReader(asset: asset)
let readerOutput = AVAssetReaderTrackOutput(track: track, outputSettings: [
    kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_420YpCbCr8BiPlanarVideoRange,
])
reader.add(readerOutput)

let writer = try AVAssetWriter(outputURL: output, fileType: .mp4)
let writerInput = AVAssetWriterInput(mediaType: .video, outputSettings: [
    AVVideoCodecKey: AVVideoCodecType.h264,
    AVVideoWidthKey: 1920,
    AVVideoHeightKey: 1080,
    AVVideoScalingModeKey: AVVideoScalingModeResizeAspectFill,
    AVVideoCompressionPropertiesKey: [
        AVVideoAverageBitRateKey: bitrate,
        AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel,
        AVVideoExpectedSourceFrameRateKey: Int(min(sourceFPS, maxFPS)),
    ],
])
writerInput.expectsMediaDataInRealTime = false
writer.add(writerInput)

reader.startReading()
writer.startWriting()
writer.startSession(atSourceTime: .zero)

let queue = DispatchQueue(label: "transcode")
var frameIndex = 0
await withCheckedContinuation { (done: CheckedContinuation<Void, Never>) in
    writerInput.requestMediaDataWhenReady(on: queue) {
        while writerInput.isReadyForMoreMediaData {
            guard let sample = readerOutput.copyNextSampleBuffer() else {
                writerInput.markAsFinished()
                done.resume()
                return
            }
            if frameIndex % frameStep == 0 { writerInput.append(sample) }
            frameIndex += 1
        }
    }
}
await writer.finishWriting()
print(writer.status == .completed ? "ok: \(output.lastPathComponent)" : "erro: \(String(describing: writer.error))")
