from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class Line:
    start: tuple[float, float]
    end: tuple[float, float]


@dataclass(frozen=True)
class Arc:
    center: tuple[float, float]
    radius: float
    start_angle: float
    end_angle: float


@dataclass(frozen=True)
class Circle:
    center: tuple[float, float]
    radius: float


Entity = Line | Arc | Circle


def read_group_pairs(path: str | Path) -> list[tuple[str, str]]:
    lines = Path(path).read_text(errors="ignore").splitlines()
    pairs: list[tuple[str, str]] = []
    for index in range(0, len(lines) - 1, 2):
        pairs.append((lines[index].strip(), lines[index + 1].strip()))
    return pairs


def _float(values: dict[str, list[str]], code: str, default: float | None = None) -> float:
    found = values.get(code)
    if not found:
        if default is None:
            raise ValueError(f"Missing required DXF group code {code}")
        return default
    return float(found[-1])


def _parse_entity(kind: str, values: dict[str, list[str]]) -> Entity | None:
    if kind == "LINE":
        return Line(
            (_float(values, "10"), _float(values, "20")),
            (_float(values, "11"), _float(values, "21")),
        )
    if kind == "ARC":
        return Arc(
            (_float(values, "10"), _float(values, "20")),
            _float(values, "40"),
            _float(values, "50"),
            _float(values, "51"),
        )
    if kind == "CIRCLE":
        return Circle(
            (_float(values, "10"), _float(values, "20")),
            _float(values, "40"),
        )
    return None


def parse_entities(path: str | Path) -> list[Entity]:
    pairs = read_group_pairs(path)
    entities: list[Entity] = []
    in_entities = False
    pending_kind: str | None = None
    pending_values: dict[str, list[str]] = {}
    supported = {"LINE", "ARC", "CIRCLE"}

    def flush() -> None:
        nonlocal pending_kind, pending_values
        if pending_kind:
            entity = _parse_entity(pending_kind, pending_values)
            if entity is not None:
                entities.append(entity)
        pending_kind = None
        pending_values = {}

    for code, value in pairs:
        if code == "0" and value == "SECTION":
            continue
        if code == "2" and value == "ENTITIES":
            in_entities = True
            continue
        if not in_entities:
            continue
        if code == "0" and value == "ENDSEC":
            flush()
            break
        if code == "0":
            flush()
            if value in supported:
                pending_kind = value
                pending_values = {}
            continue
        if pending_kind:
            pending_values.setdefault(code, []).append(value)

    return entities


def entity_counts(entities: Iterable[Entity]) -> dict[str, int]:
    counts: dict[str, int] = {}
    for entity in entities:
        name = type(entity).__name__.upper()
        counts[name] = counts.get(name, 0) + 1
    return counts
