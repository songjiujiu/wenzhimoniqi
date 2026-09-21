# Real mosquito recording

- Title: **Mosquito Buzzing**
- Creator: **MarianaRA**
- Source: https://freesound.org/people/MarianaRA/sounds/477794/
- License: **CC0 1.0**, https://creativecommons.org/publicdomain/zero/1.0/
- Downloaded 2026-09-21 from the public high-quality MP3 preview:
  https://cdn.freesound.org/previews/477/477794_3472612-hq.mp3
- Original author describes a real mosquito recorded with a Tascam DR-40
  in Mexico City, with intermittent plastic-bag handling noises.

The retained source is `MarianaRA-MosquitoBuzzing-477794.mp3`, the public
MP3 preview, not the original uncompressed WAV (which requires login).
`Tools/prepare_mosquito_audio.py` selects a low-transient eight-second
excerpt, converts to mono, applies gentle filtering/denoising, and joins
the loop with a 160 ms crossfade. The output is 7.84 seconds, 44.1 kHz,
16-bit PCM at `Assets/Game/Resources/Audio/MosquitoBuzzLoop.wav`.
The exact excerpt and signal measurements are in `MosquitoBuzzLoop-report.json`.
There is no synthesized tone or pitch shift in this mosquito loop.

In-game master volume and the existing mosquito-buzz slider control it.
Pause, menu, no remaining adult mosquitoes, and a zero buzz slider silence
the loop. One or more adults produce audible volume; larger populations
gradually raise it without altering the recording's pitch.
