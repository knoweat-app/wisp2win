# Local diarization experiment notes

Wisp2Win v0.4.0 ships plain Whisper transcription for imported audio files and TXT export. Speaker diarization is not part of the Whisper-only feature set and should be treated as a separate experimental track.

## Constraints

- Keep live dictation and imported-file transcription usable without Python, cloud APIs, or extra model accounts.
- Do not label diarization as a guaranteed Whisper capability.
- Prefer local processing and make any large runtime/model dependency opt-in.

## Candidate approaches

1. `whisper.cpp` tinydiarize
   - Pros: local, close to the existing Whisper model ecosystem.
   - Cons: limited scope, model-dependent, not full multi-speaker diarization.
   - Best fit: experimental "speaker turn hints" after the core app can invoke `whisper.cpp` directly.

2. `pyannote.audio`
   - Pros: established diarization quality and speaker segmentation pipeline.
   - Cons: Python stack, separate model downloads, Hugging Face access requirements for common pretrained models.
   - Best fit: optional developer/advanced-user experiment, not default Wisp2Win behavior.

3. NVIDIA NeMo diarization
   - Pros: mature local diarization toolkit.
   - Cons: heavy runtime, packaging burden for a small desktop dictation app.
   - Best fit: research only unless the app gains a plugin-style optional runtime.

## Next step

Prototype diarization outside the main app against a few local meeting-style WAV files, then only wire it into Wisp2Win if the runtime can stay optional and the UI can clearly label the output as experimental.
