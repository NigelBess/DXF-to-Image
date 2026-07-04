from __future__ import annotations

from html import escape

from .geometry import Bounds, Point, bounds_for_points


def _fmt(value: float) -> str:
    text = f"{value:.6f}".rstrip("0").rstrip(".")
    return text if text else "0"


def _transform(point: Point, bounds: Bounds, padding: float) -> Point:
    x, y = point
    return (bounds.max_x - x + padding, bounds.max_y - y + padding)


def path_data(face: list[Point], bounds: Bounds, padding: float) -> str:
    points = [_transform(point, bounds, padding) for point in face]
    first = points[0]
    commands = [f"M {_fmt(first[0])} {_fmt(first[1])}"]
    commands.extend(f"L {_fmt(x)} {_fmt(y)}" for x, y in points[1:])
    commands.append("Z")
    return " ".join(commands)


def svg_document(
    faces: list[list[Point]],
    fill: str,
    debug_strokes: bool = False,
    padding: float = 0.0,
) -> str:
    if not faces:
        raise ValueError("No faces found to write")
    all_points = [point for face in faces for point in face]
    bounds = bounds_for_points(all_points)
    width = bounds.width + padding * 2
    height = bounds.height + padding * 2
    stroke = ' stroke="#111827" stroke-width="0.15"' if debug_strokes else ' stroke="none"'
    paths = "\n".join(
        f'  <path d="{escape(path_data(face, bounds, padding))}" fill="{escape(fill)}"'
        f' fill-rule="evenodd"{stroke}/>'
        for face in faces
    )
    return (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {_fmt(width)} {_fmt(height)}" '
        f'width="{_fmt(width)}" height="{_fmt(height)}">\n'
        f"{paths}\n"
        "</svg>\n"
    )
