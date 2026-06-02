#!/usr/bin/env python3
"""Build a minimal low-poly M14 rifle GLB (fallback when web download is unavailable)."""
from __future__ import annotations

import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "Resources" / "Weapons" / "M14Rifle"
OUT_PATH = OUT_DIR / "M14Rifle.glb"


def box(min_corner, max_corner):
    x0, y0, z0 = min_corner
    x1, y1, z1 = max_corner
    return [
        (x0, y0, z0),
        (x1, y0, z0),
        (x1, y1, z0),
        (x0, y1, z0),
        (x0, y0, z1),
        (x1, y0, z1),
        (x1, y1, z1),
        (x0, y1, z1),
    ]


def triangulate_box(verts):
    faces = [
        (0, 1, 2),
        (0, 2, 3),
        (4, 6, 5),
        (4, 7, 6),
        (0, 4, 5),
        (0, 5, 1),
        (2, 6, 7),
        (2, 7, 3),
        (0, 3, 7),
        (0, 7, 4),
        (1, 5, 6),
        (1, 6, 2),
    ]
    positions = []
    indices = []
    for a, b, c in faces:
        base = len(positions)
        positions.extend((verts[a], verts[b], verts[c]))
        indices.extend((base, base + 1, base + 2))
    return positions, indices


PARTS = [
    ("Receiver", box((-0.03, -0.04, -0.08), (0.03, 0.05, 0.10))),
    ("Barrel", box((-0.018, -0.01, 0.10), (0.018, 0.02, 0.62))),
    ("Stock", box((-0.035, -0.03, -0.34), (0.035, 0.06, -0.08))),
    ("Magazine", box((-0.022, -0.12, -0.02), (0.022, -0.04, 0.08))),
    ("Handguard", box((-0.028, -0.025, 0.08), (0.028, 0.03, 0.28))),
    ("RearSight", box((-0.012, 0.05, -0.02), (0.012, 0.08, 0.02))),
]


def pack_glb(gltf: dict, bin_blob: bytes) -> bytes:
    json_bytes = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    json_pad = (4 - (len(json_bytes) % 4)) % 4
    json_bytes += b" " * json_pad
    bin_pad = (4 - (len(bin_blob) % 4)) % 4
    bin_blob += b"\x00" * bin_pad
    total = 12 + 8 + len(json_bytes) + 8 + len(bin_blob)
    out = bytearray()
    out += struct.pack("<4sII", b"glTF", 2, total)
    out += struct.pack("<I4s", len(json_bytes), b"JSON")
    out += json_bytes
    out += struct.pack("<I4s", len(bin_blob), b"BIN\x00")
    out += bin_blob
    return bytes(out)


def main() -> None:
    bin_blob = bytearray()
    buffer_views = []
    accessors = []
    meshes = []
    nodes = []

    for part_name, part_box in PARTS:
        positions, indices = triangulate_box(part_box)
        pos_bytes = b"".join(struct.pack("<fff", *p) for p in positions)
        idx_bytes = b"".join(struct.pack("<H", i) for i in indices)

        pos_off = len(bin_blob)
        bin_blob += pos_bytes
        while len(bin_blob) % 4:
            bin_blob.append(0)

        idx_off = len(bin_blob)
        bin_blob += idx_bytes
        while len(bin_blob) % 4:
            bin_blob.append(0)

        pos_view = len(buffer_views)
        buffer_views.append(
            {"buffer": 0, "byteOffset": pos_off, "byteLength": len(pos_bytes), "target": 34962}
        )
        idx_view = len(buffer_views)
        buffer_views.append(
            {"buffer": 0, "byteOffset": idx_off, "byteLength": len(idx_bytes), "target": 34963}
        )

        pos_accessor = len(accessors)
        accessors.append(
            {
                "bufferView": pos_view,
                "componentType": 5126,
                "count": len(positions),
                "type": "VEC3",
                "min": [min(p[i] for p in positions) for i in range(3)],
                "max": [max(p[i] for p in positions) for i in range(3)],
            }
        )
        idx_accessor = len(accessors)
        accessors.append(
            {
                "bufferView": idx_view,
                "componentType": 5123,
                "count": len(indices),
                "type": "SCALAR",
            }
        )
        mesh_index = len(meshes)
        meshes.append(
            {
                "name": part_name,
                "primitives": [{"attributes": {"POSITION": pos_accessor}, "indices": idx_accessor}],
            }
        )
        nodes.append({"name": part_name, "mesh": mesh_index})

    root_index = len(nodes)
    nodes.append({"name": "M14Rifle", "children": list(range(root_index))})

    gltf = {
        "asset": {"version": "2.0", "generator": "ApocalypseKing generate-m14-glb.py"},
        "scene": 0,
        "scenes": [{"name": "Scene", "nodes": [root_index]}],
        "nodes": nodes,
        "meshes": meshes,
        "accessors": accessors,
        "bufferViews": buffer_views,
        "buffers": [{"byteLength": len(bin_blob)}],
    }

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_bytes(pack_glb(gltf, bytes(bin_blob)))
    print(f"Wrote {OUT_PATH} ({OUT_PATH.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
