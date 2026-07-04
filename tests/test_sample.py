from __future__ import annotations

import unittest
from pathlib import Path

from dxf_to_svg.compare import compare
from dxf_to_svg.convert import convert_file
from dxf_to_svg.dxf import entity_counts, parse_entities


ROOT = Path(__file__).resolve().parents[1]


class SampleConversionTest(unittest.TestCase):
    def test_parses_sample_entities(self) -> None:
        counts = entity_counts(parse_entities(ROOT / "Nebula Logo.dxf"))
        self.assertEqual(counts["LINE"], 8)
        self.assertEqual(counts["ARC"], 11)

    def test_converts_sample_with_tolerant_visual_match(self) -> None:
        out = ROOT / "test-output.svg"
        try:
            face_count = convert_file(ROOT / "Nebula Logo.dxf", out, "#6b3f22")
            self.assertGreaterEqual(face_count, 1)
            self.assertTrue(out.exists())
            score = compare(out, ROOT / "logo.png", tolerance_radius=3)
            self.assertGreaterEqual(score, 0.95)
        finally:
            out.unlink(missing_ok=True)


if __name__ == "__main__":
    unittest.main()
