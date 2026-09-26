# Changelog

## 2026-09-26 · Instructor edits and HKUST Guangzhou cover

- Merge the instructor's edits from the saved v2 presentation, including the reordered module/water slides, removed text, revised prompts and picture placement. Align page and section numbers with that order.
- Restore the existing HKUST (Guangzhou) logo on the opening slide and use `PROMPT` for all remaining prompt labels.
- Embed the new 72-second recording after the ornamental-pool fix, refresh the current screenshots, and update the Chinese notes to source revision `468100c`.
- Render all 15 slides, verify the retained manual edits, 21 links and embedded video, and preserve the native algorithm table.

## 2026-09-26 · Ornamental pool water

- Replace the solid slab beneath each small pool with a recessed floor and four side walls. Keep the reflection plane unchanged. The water now has 10.5 cm of modeled depth, with at least 8.1 cm remaining at the lowest permitted wave height.
- Replace the broad sine-stripe foam pattern with subtle, irregular contact foam near edges. Reflective water, flowing currents and the main island's spillways remain active.
- Rebuild all 40 pool-bearing module variants across three LODs. Preserve existing mesh file IDs and prefab references when rebaking legacy meshes whose main asset lacks a `LOD0` suffix.
- Verify 600 basin-interior samples, 128 module prefabs and 384 LOD meshes. The standalone build has zero errors and warnings. All 20 streamed-garden checks pass with zero runtime errors.
- Refresh the 72-second, 1080p scene tour and current screenshots after the water fix; retain the existing camera route and project audio.

Same-view images and reports: `Sky_City_Project/Captures/SkyCityWorld/WaterReview/`.

## 2026-09-26 · Refreshed classroom presentation

- Publish the 15-slide English presentation, Chinese teaching notes and official tool links under `Teaching/`, with download links in the repository README.
- Embed the corrected 72-second scene tour and refresh the cover, video poster, remote-garden screenshot and recap image. Preserve the other slide layouts, historical comparison images and clickable tool links.
- Update speaker notes to describe the plant-clearance iteration and reference source revision `1c34b5f`.

## 2026-09-26 · Planting and architecture clearance

- Refresh the 72-second, 1080p scene tour and current project screenshots after the clearance fixes. The tour retains its 30 fps video, original audio and scripted runtime camera route.
- Correct the pool-side loggia roof: its cornice previously reached 6.48 m and cut through the main terrace's flower beds at 6.19 m. The rebuilt cornice now ends at 6.06 m, below the 6.0985 m paving. Rebuild all three architectural LODs, the editable Blender model and the world scene's baked lighting.
- Leave more depth clearance between the main recursive tree crown and the upper arcade, including wind movement.
- Orient streamed recursive crowns toward the module's open space. Seed-randomized yaw could rotate the asymmetric crown into arcade columns; plant seeds and growth behavior remain deterministic.
- Add an opt-in geometry audit of fully grown plant mesh edges against actual architecture collision meshes at four maximum-breeze samples. The reproduced remote case had intersections on three trees before the orientation fix; the same eight near-LOD plant meshes pass afterward. The ten authored-island plant meshes also pass. These are specific geometric checks, not a claim that every possible world seed and wind instant has been exhaustively tested.

Validation: the repaired standalone build has zero errors and warnings. All 20 streamed-garden regression checks pass with zero runtime errors; 1,307 of 1,307 sampled frames rendered at 1600 x 900 (RTX 3090), P95 16.668 ms, P99 16.739 ms, maximum 17.861 ms. See `Sky_City_Project/Captures/SkyCityWorld/ClippingReview/` for the same-view screenshots, geometry reports and `RuntimeAfter.json`.

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
