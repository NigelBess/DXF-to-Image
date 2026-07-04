# DXF to SVG

Small dependency-light converter for filling enclosed DXF regions and writing them as SVG paths.

## Usage

```powershell
python -m dxf_to_svg.convert "Nebula Logo.dxf" --fill "#6b3f22" --out output.svg
```

Add boundary strokes while debugging traced faces:

```powershell
python -m dxf_to_svg.convert "Nebula Logo.dxf" --fill "#6b3f22" --out output.svg --debug-strokes
```

Compare the generated SVG mask against the included hand-made PNG reference:

```powershell
python -m dxf_to_svg.compare output.svg logo.png
```

## Supported DXF Entities

The first version reads the `ENTITIES` section and supports:

- `LINE`
- `ARC`
- `CIRCLE`

Arcs and circles are flattened into deterministic short line segments before endpoint snapping and face tracing.

## Tests

The tests use Python's standard `unittest` runner:

```powershell
python -m unittest discover -s tests
```

The visual sample test uses Pillow for mask rasterization and comparison. It does not require CairoSVG, Inkscape, or ImageMagick.

## Limitations

This is intentionally scoped to the included sample and simple closed geometry. It does not yet split crossing segments, parse polylines, or preserve DXF layer styling.
