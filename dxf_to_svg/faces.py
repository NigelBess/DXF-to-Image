from __future__ import annotations

import math
from collections import defaultdict

from .geometry import Point, Segment, polygon_area


def _snap_point(point: Point, epsilon: float) -> Point:
    return (round(point[0] / epsilon) * epsilon, round(point[1] / epsilon) * epsilon)


def snapped_segments(segments: list[Segment], epsilon: float = 1e-5) -> list[Segment]:
    snapped: list[Segment] = []
    for start, end in segments:
        a = _snap_point(start, epsilon)
        b = _snap_point(end, epsilon)
        if a != b:
            snapped.append((a, b))
    return snapped


def _cross(a: Point, b: Point) -> float:
    return a[0] * b[1] - a[1] * b[0]


def _subtract(a: Point, b: Point) -> Point:
    return (a[0] - b[0], a[1] - b[1])


def _segment_intersection(a: Point, b: Point, c: Point, d: Point, epsilon: float) -> tuple[float, float] | None:
    r = _subtract(b, a)
    s = _subtract(d, c)
    denominator = _cross(r, s)
    if abs(denominator) <= epsilon:
        return None
    qp = _subtract(c, a)
    t = _cross(qp, s) / denominator
    u = _cross(qp, r) / denominator
    if -epsilon <= t <= 1.0 + epsilon and -epsilon <= u <= 1.0 + epsilon:
        return (min(1.0, max(0.0, t)), min(1.0, max(0.0, u)))
    return None


def split_segments_at_intersections(segments: list[Segment], epsilon: float = 1e-5) -> list[Segment]:
    cuts: list[list[float]] = [[0.0, 1.0] for _ in segments]
    for i, (a, b) in enumerate(segments):
        for j in range(i + 1, len(segments)):
            c, d = segments[j]
            hit = _segment_intersection(a, b, c, d, epsilon)
            if hit is None:
                continue
            t, u = hit
            if epsilon < t < 1.0 - epsilon:
                cuts[i].append(t)
            if epsilon < u < 1.0 - epsilon:
                cuts[j].append(u)

    split: list[Segment] = []
    for (a, b), segment_cuts in zip(segments, cuts):
        unique = sorted(set(round(cut, 10) for cut in segment_cuts))
        for start_t, end_t in zip(unique, unique[1:]):
            if end_t - start_t <= epsilon:
                continue
            start = (a[0] + (b[0] - a[0]) * start_t, a[1] + (b[1] - a[1]) * start_t)
            end = (a[0] + (b[0] - a[0]) * end_t, a[1] + (b[1] - a[1]) * end_t)
            split.append((start, end))
    return split


def _angle(a: Point, b: Point) -> float:
    return math.atan2(b[1] - a[1], b[0] - a[0])


def trace_faces(segments: list[Segment], epsilon: float = 1e-5, min_area: float = 1e-4) -> list[list[Point]]:
    segments = snapped_segments(split_segments_at_intersections(segments, epsilon), epsilon)
    adjacency: dict[Point, set[Point]] = defaultdict(set)
    for a, b in segments:
        adjacency[a].add(b)
        adjacency[b].add(a)

    ordered: dict[Point, list[Point]] = {}
    for vertex, neighbors in adjacency.items():
        ordered[vertex] = sorted(neighbors, key=lambda n: _angle(vertex, n))

    visited: set[tuple[Point, Point]] = set()
    faces: list[list[Point]] = []

    for start in ordered:
        for nxt in ordered[start]:
            edge = (start, nxt)
            if edge in visited:
                continue
            face: list[Point] = []
            current, target = edge
            for _ in range(len(segments) * 4 + 10):
                if (current, target) in visited:
                    break
                visited.add((current, target))
                face.append(current)
                neighbors = ordered[target]
                reverse_index = neighbors.index(current)
                next_index = (reverse_index - 1) % len(neighbors)
                current, target = target, neighbors[next_index]
                if current == start and target == nxt:
                    break
            if len(face) >= 3:
                area = polygon_area(face)
                if abs(area) >= min_area:
                    faces.append(face)

    if not faces:
        return []

    # With this traversal the unbounded exterior is the largest negative face
    # for typical DXF outlines. Keeping positive loops gives filled regions.
    positive = [face for face in faces if polygon_area(face) > min_area]
    if positive:
        return sorted(positive, key=lambda face: abs(polygon_area(face)), reverse=True)

    return sorted(faces, key=lambda face: abs(polygon_area(face)), reverse=True)[1:]
