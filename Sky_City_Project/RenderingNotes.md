# Sky City: pearl morning rendering

This revision stays on Unity 2022.3.62f2c1 and URP 14.0.12. It changes rendering in `SkyCityInfinite`, not the WFC vocabulary or streaming limits.

## Rendering changes

- Sky, cloud lighting and water's sky reflection use the scene sun and a shared dawn palette. A `SkyCityDaylight` component binds the palette and refreshes the environment on enable.
- Architecture samples Unity's ambient spherical harmonics instead of a fixed shader gradient. This is environment lighting, **not realtime global illumination**. SSAO now modulates indirect lighting rather than multiplying the final opaque image.
- A separate honed limestone texture adds restrained color variation and surface relief to buildings. The original mineral texture remains on cliffs and drives copper patina; copper has varying roughness.
- High volumetric cloud banks occupy the gaps between districts. Smooth clearings keep the inhabited terraces above the cloud ceiling. Clouds remain animated volumetric raymarches at half resolution, with 112 view samples and 5 light samples.
- The nearest garden's planar reflection is 768 pixels wide, updated at most 30 times per second during steady travel. Water samples the projection matrix used to render that image. Rebasing or changing the reflecting plane forces a refresh. Other elevations retain a sky reflection to avoid projecting the wrong buildings.
- The infinite scene has a separate `PearlMorningGrade` profile. Its scene builder reapplies the same rendering settings through `Boids > Sky City Infinite > Apply pearl morning lighting`.

## Review

Local screenshots are in `Captures/RenderingReview`. Before/after overview and palace images use the same camera positions, FOV and dimensions. Birds and clouds are dynamic, so the captures do not represent identical simulation instants. These images are Unity camera renders, without painted-over scene content.

Validation on 2026-09-25:

- Windows standalone build succeeded, zero errors and zero warnings. All four revised shaders compiled without messages in the editor.
- The visible standalone player rendered all 3,900 measured frames over 65.01 seconds at 1536 x 1024 on an RTX 3090, with a 60 FPS cap. P95 frame time was 16.670 ms; P99 16.726 ms; maximum 17.259 ms. GPU P95 was 6.414 ms.
- Streaming completed 223 chunks and unloaded 198, with 25 peak residents, 3 peak pending jobs, 7 origin shifts, no fallbacks and no errors. Returning to the initial district reproduced its fingerprint. These timings describe this hardware and route, not every device or world seed.
- Five seconds of fixed-step water review produced 150 camera frames and 149 reflection updates. This is a motion check, not a performance benchmark.
- A first hidden-window run produced zero rendered frames and is excluded from rendering-performance claims. Its report remains in the local review folder.
- The measured player report is preserved in `Captures/SkyCityInfinite_RenderingPerformance.json`.

The architecture and foliage meshes are unchanged in this revision. Simplified tree crowns, plain wall silhouettes and repeated architectural motifs still limit how closely the scene matches the concept. Improved lighting is not evidence that the concept's artistic quality has been reached.

## Generated texture provenance

- Asset: `Assets/Boids/Art/SkyCityInfinite/HonedIvoryLimestone.png`
- Created: 2026-09-25, with the built-in imagegen tool; no input/reference image.
- Copied into the Unity project from the tool output. Unity imports it with mipmaps, trilinear filtering, 8x anisotropy and a 1024-pixel texture limit.
- The generated image is used as a surface texture, not as a background or replacement for a Unity render.

Prompt:

> Use case: photorealistic-natural. Asset type: seamless square tileable albedo texture for fine carved ivory limestone architecture in a floating Renaissance garden city. Primary request: flat orthographic scan of a continuous smooth honed limestone surface, warm ivory with very subtle pale beige mineral mottling, tiny pores and a few fine fossil flecks. Quiet refined material for palace columns and balustrades, believable finely crafted architectural stone. Very low contrast, uniform neutral diffuse illumination, no baked shadows or specular highlights. Edge-to-edge stone, seamless tile on all four edges. NO brick pattern, NO grout, NO slabs, NO stripes, NO long veins, NO cracks, NO vignette, NO perspective, NO objects, NO text, NO border. Square 1024 by 1024.
