# E — The Submerged Garden

An editable Blender environment for the fixed-camera 3D Boids scene. This is an
ongoing art iteration against `ArtSource/Concepts/ArtHistoryStudies/E-symbolist-dream-sea.png`.
The concept image is not used as a backdrop or rendered scene texture.

## Source and import

- `build_atelier.py` authors folded botanical surfaces, overlapping corollas,
  eroded reefs, rooted colonies, branching stems and surface paint marks.
- `E_SubmergedGarden.blend` retains these as named mesh objects. The source was
  authored through Blender Python, not manually sculpted.
- The matching FBX is in `Sky_City_Project/Assets/Boids/Art/Atelier/`.
- Geometric ambient occlusion is stored in vertex alpha; pigment is stored in RGB.
- `AtelierSceneBuilder.Build()` assembles `RedonAtelier.unity` and retains the
  existing animated fish, fixed camera, Boids controller and screen-point feeding.
- The new shaders interpret pigment, geometric shading, depth fog and transparent
  fins. The atmosphere is procedural colored water, with no depicted scenery.

Rebuild the source in a separate Blender process:

```powershell
& 'E:\ProgramFiles\Steam\steamapps\common\Blender\blender.exe' --background --factory-startup --python 'ArtSource\Redon\Atelier\build_atelier.py'
```

The generator clears its factory scene; do not run it in an unsaved working scene.
After FBX import and successful compilation, run **Boids > Atelier > Build Sculpted Garden**.

## Generated material texture provenance

`Textures/SymbolistPigment.png` was generated with the built-in
`image_gen.imagegen` tool on 2026-09-23. It is an abstract material texture, not an
environment plate. Original generation ID: `exec-10cbc7d5-ed09-4521-85aa-67fa098f761f`.

Prompt:

> Create a production-ready square 2048x2048 oil-paint surface texture for a richly modeled three-dimensional Symbolist underwater garden inspired by Odilon Redon. This is a MATERIAL ALBEDO TEXTURE ONLY, not a scene or illustration. Full bleed uniform surface. A sophisticated field of scumbled layered pigments: predominantly slate indigo, dusty lavender, muted peacock blue and grey mauve, occasional restrained apricot and antique gold flecks. Broad translucent oil glazes interwoven with fine dry-brush marks, delicate mineral-like pigment granulation, fractured broken color, soft worn painterly ridges. Lyrical organic brush direction, several scales of marks, no uniform digital noise. Enough local color variation for a closeup to feel like a real painted canvas. Midtone dominant; no pure white or black, no metallic reflections, no lighting gradient or baked shadows, no vignette. No objects, no flowers, no leaves, no landscape, no text, no panels, no border. It will be wrapped over real 3D leaves, petal folds and sculpted stone; it must provide painterly material character without depicting geometry. High visual refinement. Use the requested square aspect and highest useful detail.

`Textures/BotanicalGlaze.png` was generated with `image_gen.imagegen` on
2026-09-24, using concept E as a material/style reference. Generation ID:
`exec-3651acc4-e174-41b1-a6ec-8b68f1f244cf`. It supplies surface pigment and fine
veins on the actual curved leaf meshes. The shader predominantly retains the
authored vertex palette and uses the texture for local pigment variation.

Prompt:

> Use this reference only to study the magnificent violet/lavender botanical surfaces on the LEFT of the image. Create a SQUARE 2048x2048 seamless full-bleed ALBEDO MATERIAL TEXTURE for real modeled folded fan petals. NOT a scene, NOT a landscape, NOT a picture of a leaf, no silhouette or surrounding background. The entire square must be painted botanical material. The texture will map horizontally across petal width, with the stem/root along bottom center and the round outer lip toward the top. A deep muted indigo-mauve base with lyrical fans of subtle dusty lavender and muted teal oil paint glazes; very fine irregular branching veins fan and curve out from the bottom center, like delicate gold threads submerged under layers of translucent violet paint. No geometrically straight lines; no regular grid or symmetry. Veins should be partial, worn, softly obscured and very fine, never a bright literal diagram. Maintain low overall luminance (deep midtones), with selective broken touches of warm antique gold at edges and scattered mineral-like apricot flecks. A sophisticated Redon-inspired oil-painted surface: exquisite scumbled dry brush, layered translucent pigment, dark violet irregular fissures, small lacy marks, several scales of painterly details, subtle color variation over coherent large areas. This is a production surface texture, so no strong baked directional lighting, no cast shadows, no 3D perspective, no frame, no captions. Do not show the fish, water, or any scene elements from the reference. Fill every pixel with the botanical paint surface.

`Textures/ApricotImpasto.png` was generated with `image_gen.imagegen` on
2026-09-24. Generation ID: `exec-e0f339f9-8fb1-44b1-b780-e4925303e2c4`.
The earlier attempt did not return an image; this successful retry is the imported asset.

Prompt:

> Square 2048x2048 flat tileable oil-paint material texture for a three-dimensional giant coral blossom. An abstract non-representational painted surface only. Luminous apricot, pale ochre, dusty peach and mauve with dark violet underpainting visible in irregular broken islands. Dense small scumbled palette-knife marks and translucent oil glazes, finely cracked pigment, sophisticated tactile painterly detail, several scales of organic brush marks. Even diffuse light, no shadows, no highlights from lighting, no vignette. Do not depict flowers, leaves, silhouettes, scenery or objects. No text. Fill the entire image with the material.

## Review standard

Unity camera renders are the visual evidence. Blender viewport previews show the
editable geometry, but do not reproduce the Unity material and atmospheric setup.
Geometry counts and successful compilation do not establish artistic parity with
the reference. Assess silhouettes, focal light, dark foreground masses, depth,
surface variety, and moving fish together.

## Open and review the modeled scene

Open `Sky_City_Project/Assets/Boids/Scenes/RedonAtelier.unity` in Unity and enter Play
mode. Click the central water to feed the school. The original `RedonDream`
scene remains available as the earlier study.

Open `E_SubmergedGarden.blend` to edit the environment. The camera composition,
vertex pigment display, named objects and individual local origins are saved.
The solid viewport hides the old veins and transparent brush quads; the FBX
contains the surface brush geometry for Unity's alpha material.

The current forms include individually shaped, curled fan leaves; six asymmetric
folded membranes per large crown; eroded and terraced reef masses; rooted small
corals; branching veins; hanging roots; and golden inflorescences. Mesh vertex
alpha stores geometric occlusion. These are real surfaces with depth, not
camera-facing environment cards.

For validation, use **Boids > Atelier > Check Reload and Play Mode**. Reports and
actual camera captures are saved in `Sky_City_Project/Captures/Atelier_*`. To regenerate
the motion preview, use **Boids > Atelier > Record Interaction Preview Frames**.
This records 420 camera frames at a target 30 fps, at 1536 by 1024 pixels. The
JSON records elapsed simulation time; this offline recording is not a frame-rate
benchmark. Encode the frames with:

```powershell
ffmpeg -framerate 30 -i 'Sky_City_Project/Captures/AtelierInteractionFrames/frame_%04d.png' -c:v libx264 -crf 18 -pix_fmt yuv420p -movflags +faststart 'Sky_City_Project/Captures/Atelier_Interaction.mp4'
```

The concept remains the art reference. The camera captures show the implemented
interpretation; automated integrity and interaction checks do not measure visual
equivalence to the painting.
