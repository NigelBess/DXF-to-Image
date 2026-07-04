# DXF to SVG Converter Plan

## Goal

Build a DXF to SVG converter that finds enclosed regions in a DXF file and emits an SVG that fills those regions with a user-specified color.

The initial acceptance test is the included sample:

- `Nebula Logo.dxf`
- `logo.png`

The PNG is a hand-made reference image of the DXF filled with a solid brown color. It is not expected to match pixel-perfectly, so verification should use a tolerant visual comparison.

## Proposed CLI

```powershell
python -m dxf_to_svg.convert "Nebula Logo.dxf" --fill "#6b3f22" --out output.svg
```

Useful optional flags:

```powershell
python -m dxf_to_svg.convert "Nebula Logo.dxf" --fill "#6b3f22" --out output.svg --debug-strokes
python -m dxf_to_svg.compare output.svg logo.png
```

## Current Sample Observations

The workspace currently contains:

- `Nebula Logo.dxf`
- `logo.png`

The PNG dimensions are `813 x 813`.

A quick scan of the DXF entity records found these relevant entity counts:

- `LINE`: 8
- `ARC`: 11

The file also contains DXF class/table metadata mentioning entities like `LWPOLYLINE` and `HATCH`, but the actual `ENTITIES` section scan showed the sample geometry is primarily `LINE` and `ARC`.

## Implementation Steps

### 1. Project Structure

Create a small Python package:

```text
dxf_to_svg/
  __init__.py
  convert.py
  dxf.py
  geometry.py
  faces.py
  svg.py
  compare.py
tests/
  test_sample.py
README.md
```

Keep the first version dependency-light. Use standard library where practical. Add focused dependencies only when they materially simplify robust geometry or SVG rendering.

### 2. Parse DXF

Read the DXF as group-code/value pairs.

Focus first on the `ENTITIES` section and support:

- `LINE`
- `ARC`
- `CIRCLE`, if inexpensive
- `LWPOLYLINE` / `POLYLINE`, if encountered later

For the sample, `LINE` and `ARC` should be enough.

Important DXF group codes:

For `LINE`:

- `10`, `20`: start x/y
- `11`, `21`: end x/y

For `ARC`:

- `10`, `20`: center x/y
- `40`: radius
- `50`: start angle in degrees
- `51`: end angle in degrees

DXF arcs are counter-clockwise from start angle to end angle. If the end angle is less than the start angle, add 360 degrees.

### 3. Normalize Geometry

Represent the extracted geometry as 2D primitives.

Then convert curves into polylines:

- Preserve `LINE` as a two-point segment.
- Flatten `ARC` to short segments using a tolerance.
- Use enough arc samples to avoid visible faceting in the output.
- Keep the flattening deterministic.

Snap nearly identical endpoints together using an epsilon. This prevents tiny floating point gaps from breaking closed loops.

Initial epsilon ideas:

- absolute coordinate tolerance around `1e-6` to `1e-4`, depending on DXF scale
- make configurable if needed

### 4. Find Enclosed Regions

Build a planar graph from the snapped segments:

1. Create vertices from snapped segment endpoints.
2. Add undirected edges for each segment.
3. Split segments at intersections if the sample requires it.
4. Build directed half-edges.
5. At each vertex, sort outgoing edges by angle.
6. Trace faces by walking directed edges in angular order.
7. Compute signed area for each traced face.
8. Discard the outside face.
9. Discard tiny accidental faces/noise.

SVG can use `fill-rule="evenodd"` if nested loops or holes are present.

For the first sample, a simpler loop extraction may work if the geometry consists of already connected boundary curves. Still, the code should be structured so a true face-tracing implementation can replace or strengthen the first pass.

### 5. Generate SVG

Compute a bounding box from the filled geometry.

DXF uses a Y-up coordinate system; SVG uses Y-down. Convert coordinates by either:

- flipping y values directly, or
- using an SVG transform.

Prefer emitting straightforward path coordinates with a correct `viewBox`.

Output:

- one or more `<path>` elements
- `fill` set to the user-provided color
- `fill-rule="evenodd"` where needed
- no stroke by default

Add `--debug-strokes` to render boundaries for debugging.

### 6. Visual Comparison Acceptance Test

Render the generated SVG to a PNG and compare against `logo.png`.

First check what renderers are locally available:

- browser/Playwright
- Inkscape
- ImageMagick
- CairoSVG

If no renderer is installed, implement the comparison harness so it can use the first available renderer later.

Comparison should be tolerant because the reference PNG was made by hand:

1. Render SVG to the same dimensions as `logo.png`.
2. Convert both images into masks:
   - filled/non-transparent pixels for SVG
   - brown/non-background pixels for reference PNG
3. Optionally apply a small blur/dilation tolerance around edges.
4. Compute a metric such as Intersection-over-Union.
5. Pass if the metric is good enough.

Initial acceptance target:

```text
IoU >= 0.95
```

Adjust only if inspection shows the hand-made PNG reference has larger unavoidable differences.

### 7. Iteration Strategy

After the first implementation:

1. Convert `Nebula Logo.dxf` to `output.svg`.
2. Render `output.svg` to PNG.
3. Compare against `logo.png`.
4. Inspect differences.
5. Fix likely causes:
   - missing loop
   - wrong arc direction
   - coarse arc flattening
   - endpoint snapping too strict or too loose
   - wrong inside/outside classification
   - incorrect y-axis flip
   - incorrect viewBox/padding

### 8. Documentation

Add a `README.md` with:

- installation/setup
- CLI usage
- examples using the included sample
- explanation of supported DXF entities
- verification command
- current limitations

## Notes for Future Agents

- Do not assume exact pixel equality with `logo.png`; it is a rough acceptance reference.
- Start with the included DXF before broadening format support.
- Keep the geometry pipeline modular: parse, flatten, snap, face trace, SVG emit, compare.
- Avoid large refactors until the sample passes visually.
- If adding dependencies, document why they are needed and how to install them.
