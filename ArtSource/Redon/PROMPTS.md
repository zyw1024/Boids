# Redon 纹理生成提示词

2026-09-23，通过 Codex 内置 imagegen 生成。以下记录实际成功生成、并导入 Unity 的三次提示词；超时且没有返回素材的尝试不作为资产来源。没有使用概念图作为贴图，没有外部参考图片输入，生成结果没有做图像后期修改。

原始 PNG 保存在 `Boids_Proj/Assets/Boids/Art/Redon/Textures/`。Unity 将纹理导入为 1024 × 1024；前两张按线性数据读取，第三张按 sRGB 色彩读取。

## PigmentScumble.png

```text
Use case: stylized-concept. Asset type: grayscale pigment variation texture for an original painterly 3D underwater environment in Unity. Generate a square 1024x1024 full-bleed texture swatch. A close straight-on scan of dry, softly scumbled oil and pastel pigment: broad overlapping broken brush marks, irregular flecks of exposed ground, subtly directional bristle traces, several scales of organic granular pigment. Strictly neutral grayscale, mostly middle gray with soft charcoal and pale gray patches, moderate contrast; no pure white background. Distribute the detail evenly across the image with no central focal point. Smooth tonal fields between clusters of brush marks, sophisticated hand-worked surface, not a uniform noise cloud. No depicted subjects, leaves, fish, water, landscape, canvas border, perspective, text, grid or directional lighting. Flat albedo and mask data, not a photoreal material sphere. All edges should blend for repeating use. This will modulate color and painterly normals on actual 3D meshes; it must contain no scene composition.
```

## DryBrushAtlas.png

```text
Create a square black-and-white game texture: sixteen separate white dry-brush paint strokes evenly arranged in a 4 by 4 grid on a pure black background. All strokes are horizontal, roughly oval, with varied frayed bristles and broken pigment coverage. Every stroke has generous black padding inside its own cell. No text, no visible grid, no shadows, no border. This is a grayscale mask atlas, not a scene illustration.
```

## PetalUnderpainting.png

```text
Square seamless color texture for painterly 3D plant surfaces. A flat scan of an original richly layered oil-and-pastel underpainting, with broad soft scumbled patches of dusty lavender, ultramarine, muted peacock blue, smoky plum, and sparse warm apricot and antique gold. Visible curved dry brush marks, irregular pigment islands, translucent color overlaps, quiet areas between marks. Medium dark overall, selectively luminous warm flecks. Poetic and atmospheric, inspired by Odilon Redon's late color paintings. No flowers, leaves, objects, scenery, border, text, symbols, perspective, baked shadows or specular reflections: only abstract pigment across the entire square, suitable as an albedo texture.
```
