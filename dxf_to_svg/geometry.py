from __future__ import annotations

import math
from dataclasses import dataclass

from .dxf import Arc, Circle, Entity, Line

Point = tuple[float, float]
Segment = tuple[Point, Point]


@dataclass(frozen=True)
class Bounds:
    min_x: float
    min_y: float
    max_x: float
    max_y: float

    @property
    def width(self) -> float:
        return self.max_x - self.min_x

    @property
    def height(self) -> float:
        return self.max_y - self.min_y


def point_on_circle(center: Point, radius: float, angle_degrees: float) -> Point:
    radians = math.radians(angle_degrees)
    return (center[0] + radius * math.cos(radians), center[1] + radius * math.sin(radians))


def flatten_arc(arc: Arc, max_step_degrees: float = 4.0) -> list[Point]:
    start = arc.start_angle
    end = arc.end_angle
    while end <= start:
        end += 360.0
    sweep = end - start
    steps = max(2, int(math.ceil(sweep / max_step_degrees)))
    return [
        point_on_circle(arc.center, arc.radius, start + sweep * index / steps)
        for index in range(steps + 1)
    ]


def flatten_circle(circle: Circle, max_step_degrees: float = 4.0) -> list[Point]:
    steps = max(24, int(math.ceil(360.0 / max_step_degrees)))
    return [
        point_on_circle(circle.center, circle.radius, 360.0 * index / steps)
        for index in range(steps + 1)
    ]


def flatten_entities(entities: list[Entity], max_step_degrees: float = 3.0) -> list[Segment]:
    segments: list[Segment] = []
    for entity in entities:
        if isinstance(entity, Line):
            segments.append((entity.start, entity.end))
        elif isinstance(entity, Arc):
            points = flatten_arc(entity, max_step_degrees)
            segments.extend(zip(points, points[1:]))
        elif isinstance(entity, Circle):
            points = flatten_circle(entity, max_step_degrees)
            segments.extend(zip(points, points[1:]))
    return segments


def polygon_area(points: list[Point]) -> float:
    if len(points) < 3:
        return 0.0
    total = 0.0
    for current, nxt in zip(points, points[1:] + points[:1]):
        total += current[0] * nxt[1] - nxt[0] * current[1]
    return total / 2.0


def bounds_for_points(points: list[Point]) -> Bounds:
    if not points:
        raise ValueError("Cannot compute bounds for empty point set")
    xs = [point[0] for point in points]
    ys = [point[1] for point in points]
    return Bounds(min(xs), min(ys), max(xs), max(ys))


def distance(a: Point, b: Point) -> float:
    return math.hypot(a[0] - b[0], a[1] - b[1])
