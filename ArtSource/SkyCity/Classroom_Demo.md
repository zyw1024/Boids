# Sky City classroom Demo

This is the actual Unity scene captured with `com.unity.recorder@4.0.3`, including the original **Garden of Winds** piano, strings and harp score. It is not an AI-generated video.

- [80-second 1080p / 30 fps H.264 + AAC recording](../../Boids_Proj/Captures/SkyCity_Demo_Recorder.mp4)
- [Capture metadata](../../Boids_Proj/Captures/SkyCity_Demo_Recorder.json)
- [Latest standalone performance report](../../Boids_Proj/Captures/SkyCityInfinite_PlayerPerformance.json)

## Reproduce the take

1. Use branch `feat/redon-style-scene`. Open `Boids_Proj` with Unity 2022.3 and wait for package import.
2. Open `Assets/Boids/Scenes/SkyCityInfinite.unity` and enter Play mode.
3. Choose **Boids > Sky City Infinite > Record classroom demo (80 seconds)**.
4. The take waits for 25 resident districts and no pending work, disables camera input temporarily, and records four camera sections. The world, flock, clouds, flags and original score continue running.
5. The MP4 and JSON are saved under the OS temporary folder, `SkyCityClassroom/SkyCity_Demo_Recorder.*`. An ASCII output path avoids the Windows encoder timestamp-cleanup warning encountered with a Chinese project path.
6. The utility restores camera position, orientation, field of view and input afterward. It does not save changes to scene assets. The menu overwrites its own previous take, so copy recordings elsewhere before repeating.

Camera sections: 0–22 s city approach, 22–40 s palace detail, 40–56 s gallery bridge, 56–80 s continuous travel. These are camera cuts and motion in the running engine. Capture uses a fixed 30 fps timeline and is **not a performance benchmark**.

The classroom distribution is transcoded from Recorder's MP4 to H.264 High / yuv420p and AAC, with a front-loaded MP4 index for presentation compatibility. It preserves the 2,400 recorded video frames and recorded soundtrack. The source utility is `Assets/Boids/Editor/SkyCityDemoRecorder.cs`.

## Teaching focus

The 40-minute case study starts with the recorded Demo and repository link, then follows the actual iteration: artistic goals, Blender modeling, a 128-module vocabulary, live rendering and movement, WFC, bounded streaming, repetition, water regression, architectural repair and independent validation.

The editable presentation and Chinese teaching notes are delivered in the course workspace under `output/天空之城_VibeCoding`. Prompt rewrites in the course are explicitly labeled as classroom rewrites. They should not be mistaken for exact historical transcripts.
