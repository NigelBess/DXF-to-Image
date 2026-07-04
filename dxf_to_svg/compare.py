from __future__ import annotations

import argparse
import re
from pathlib import Path
from xml.etree import ElementTree


def _load_svg_paths(svg_path: str | Path) -> tuple[list[list[tuple[float, float]]], tuple[float, float, float, float], str]:
    root = ElementTree.parse(svg_path).getroot()
    view_box = root.attrib.get("viewBox")
    if not view_box:
        raise ValueError("SVG is missing viewBox")
    min_x, min_y, width, height = [float(part) for part in view_box.split()]
    namespace = "{http://www.w3.org/2000/svg}"
    faces: list[list[tuple[float, float]]] = []
    fill = "#000000"
    for path in root.iter(f"{namespace}path"):
        fill = path.attrib.get("fill", fill)
        numbers = [float(n) for n in re.findall(r"-?\d+(?:\.\d+)?", path.attrib["d"])]
        points = list(zip(numbers[0::2], numbers[1::2]))
        faces.append(points)
    return faces, (min_x, min_y, width, height), fill


Box = tuple[int, int, int, int]


def mask_bounds(mask: set[tuple[int, int]]) -> Box:
    if not mask:
        raise ValueError("Cannot compute bounds for an empty mask")
    xs = [point[0] for point in mask]
    ys = [point[1] for point in mask]
    return (min(xs), min(ys), max(xs), max(ys))


def render_svg_mask(
    svg_path: str | Path,
    size: tuple[int, int],
    target_box: Box | None = None,
) -> set[tuple[int, int]]:
    try:
        from PIL import Image, ImageDraw
    except ImportError as exc:  # pragma: no cover
        raise RuntimeError("Pillow is required for mask comparison") from exc

    faces, (_, _, width, height), _ = _load_svg_paths(svg_path)
    image = Image.new("L", size, 0)
    draw = ImageDraw.Draw(image)
    if target_box:
        left, top, right, bottom = target_box
        sx = (right - left + 1) / width
        sy = (bottom - top + 1) / height
        offset_x = left
        offset_y = top
    else:
        sx = size[0] / width
        sy = size[1] / height
        offset_x = 0
        offset_y = 0
    for face in faces:
        scaled = [(offset_x + x * sx, offset_y + y * sy) for x, y in face]
        draw.polygon(scaled, fill=255)
    return {
        (x, y)
        for y in range(size[1])
        for x in range(size[0])
        if image.getpixel((x, y)) > 0
    }


def reference_mask(png_path: str | Path, background_threshold: int = 245) -> set[tuple[int, int]]:
    try:
        from PIL import Image
    except ImportError as exc:  # pragma: no cover
        raise RuntimeError("Pillow is required for mask comparison") from exc

    with Image.open(png_path) as opened:
        image = opened.convert("RGBA")
        mask: set[tuple[int, int]] = set()
        for y in range(image.height):
            for x in range(image.width):
                r, g, b, a = image.getpixel((x, y))
                if a > 0 and min(r, g, b) < background_threshold:
                    mask.add((x, y))
    return mask


def dilate(mask: set[tuple[int, int]], width: int, height: int, radius: int = 1) -> set[tuple[int, int]]:
    if radius <= 0:
        return mask
    grown: set[tuple[int, int]] = set()
    for x, y in mask:
        for dy in range(-radius, radius + 1):
            for dx in range(-radius, radius + 1):
                nx, ny = x + dx, y + dy
                if 0 <= nx < width and 0 <= ny < height:
                    grown.add((nx, ny))
    return grown


def compare(svg_path: str | Path, png_path: str | Path, tolerance_radius: int = 2) -> float:
    try:
        from PIL import Image
    except ImportError as exc:  # pragma: no cover
        raise RuntimeError("Pillow is required for mask comparison") from exc

    png_mask = reference_mask(png_path)
    with Image.open(png_path) as image:
        size = image.size
    svg_mask = render_svg_mask(svg_path, size, mask_bounds(png_mask))
    svg_wide = dilate(svg_mask, size[0], size[1], tolerance_radius)
    png_wide = dilate(png_mask, size[0], size[1], tolerance_radius)
    intersection = len((svg_mask & png_wide) | (png_mask & svg_wide))
    union = len(svg_mask | png_mask)
    return intersection / union if union else 1.0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Compare a generated SVG against a reference PNG mask.")
    parser.add_argument("svg")
    parser.add_argument("png")
    parser.add_argument("--threshold", type=float, default=0.95)
    parser.add_argument("--tolerance-radius", type=int, default=2)
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    score = compare(args.svg, args.png, args.tolerance_radius)
    print(f"IoU: {score:.4f}")
    return 0 if score >= args.threshold else 1


if __name__ == "__main__":
    raise SystemExit(main())
