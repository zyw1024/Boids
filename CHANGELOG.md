# Changelog

## 2026-09-26 · Living gardens across the sky city

- Extend recursive courtyard trees and cellular flower beds to streamed WFC districts. Planting slots follow the authored arcade, garden-cloister and terraced-orchard modules, including their rotation, district shape and elevation. Visual review corrected a planter crossing a column foot and moved arcade planting fully onto its deck.
- Share cached flower, cell-overlay and tree meshes. Each loaded district contains at most four living beds and two recursive trees. Create at most one district's botany per frame, render nearby planting and simulate only within 100 metres.
- Preserve cell phases, ages, generation counters and wind-seed sequence when a district unloads. Store unloaded states in temporary session files keyed by district coordinate, world seed and layout fingerprint, then restore them on return. Session files are removed when the world closes.
- Keep garden controls and G planting available beyond the main island. World-generation changes replace the plants together with their district. Right-click still calls the birds.
- Rename the local Unity project to `Sky_City_Project`; preserve asset GUIDs, scene paths and Git history. GitHub remains `Game-Algorithms-Implementation`.
- Add a 72-second, 1920 x 1080 / 30 fps Unity Recorder tour with audio and chapter metadata. It shows the actual running scene through a scripted camera, with final screenshots of the main island, live garden controls and remote planting.

Validation: 10 existing algorithm checks and four placement checks across 35 district layouts pass. The renamed project builds successfully with zero errors and warnings. The final standalone streamed-garden review passes all 20 functional checks, including planting beyond the main island, live controls, exact unload/return restoration, seed replacement, shared meshes and 9 / 49 / 25 resident limits. Runtime errors: 0. On an RTX 3090 at 1600 x 900, all 1,254 sampled frames were actually rendered: P95 22.968 ms, P99 37.165 ms, maximum 70.880 ms. The largest spike occurred in the remote-garden segment; this is not a guarantee of uninterrupted 60 fps. See `Sky_City_Project/Captures/SkyCityWorld/DistrictGardens_Runtime.json` for route-level timings.

The existing 22-check authored-island regression also passes in the renamed build: 1,680 rendered frames, P95 16.673 ms, P99 16.890 ms, maximum 35.900 ms, zero runtime errors. See `BotanyRuntime_SkyCityProject.json` in the same captures folder. This probe exercises the original island; the 20-check probe above covers streamed gardens.

## 2026-09-26 · Main-island garden prototype (`e284d1c`)

- Add a wind tree with three cached recursive depths and animated branch growth.
- Add four planting surfaces with a synchronous four-state, eight-neighbour cellular automaton, G planting, wind seeds and bird visits.
- Add the Esc garden tab and an optional view of the actual cell states.
- Verify 10 algorithm checks and 22 runtime checks. The final standalone build reports zero errors and warnings.

## 2026-09-26 · Interactive world settings

- `95944b4`: WFC parameters, deterministic seeds and incremental regeneration with obsolete-job isolation.
- `b86a912`: First-person exploration, right-click bird gathering and live flock settings.

## Earlier visual milestones

- `65413de`: Layered volumetric clouds and hanging-garden foliage.
- `6ca7740`: Authored Hanging Gardens island and URP reference scene.
- `2e688d3`: Sky City daylight, materials and water reflections.

Screenshots and source assets preserve the earlier versions for classroom comparison. Performance reports describe their own hardware, resolution and test route.
