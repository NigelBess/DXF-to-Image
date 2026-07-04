from __future__ import annotations

import argparse
from pathlib import Path

from .dxf import parse_entities
from .faces import trace_faces
from .geometry import flatten_entities
from .svg import svg_document


def convert_file(
    input_path: str | Path,
    output_path: str | Path,
    fill: str,
    debug_strokes: bool = False,
    epsilon: float = 1e-5,
) -> int:
    entities = parse_entities(input_path)
    segments = flatten_entities(entities)
    faces = trace_faces(segments, epsilon=epsilon)
    svg = svg_document(faces, fill=fill, debug_strokes=debug_strokes)
    Path(output_path).write_text(svg, encoding="utf-8")
    return len(faces)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Convert enclosed DXF regions to a filled SVG.")
    parser.add_argument("input", help="Input DXF file")
    parser.add_argument("--fill", default="#6b3f22", help="Fill color for enclosed regions")
    parser.add_argument("--out", required=True, help="Output SVG file")
    parser.add_argument("--debug-strokes", action="store_true", help="Draw thin strokes around traced faces")
    parser.add_argument("--epsilon", type=float, default=1e-5, help="Endpoint snapping tolerance")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    count = convert_file(args.input, args.out, args.fill, args.debug_strokes, args.epsilon)
    print(f"Wrote {args.out} with {count} filled face(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
