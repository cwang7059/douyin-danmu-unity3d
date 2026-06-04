#!/usr/bin/env python3
"""Pteranodon-style winged reptile (JP-inspired silhouette) for Apocalypse King pterosaur units."""
from __future__ import annotations

import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "Resources" / "Monsters" / "Pterosaur"


def box(mn, mx):
    x0, y0, z0 = mn
    x1, y1, z1 = mx
    return [
        (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
        (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1),
    ]


def triangulate(verts):
    faces = [
        (0, 1, 2), (0, 2, 3), (4, 6, 5), (4, 7, 6),
        (0, 4, 5), (0, 5, 1), (2, 6, 7), (2, 7, 3),
        (0, 3, 7), (0, 7, 4), (1, 5, 6), (1, 6, 2),
    ]
    pos, idx = [], []
    for a, b, c in faces:
        base = len(pos)
        pos.extend((verts[a], verts[b], verts[c]))
        idx.extend((base, base + 1, base + 2))
    return pos, idx


# Forward +X, wings on Z, crest points backward (-X). Game bind rotates +X -> +Z.
PARTS = [
    ("Torso", box((-0.12, -0.10, -0.14), (0.28, 0.10, 0.14))),
    ("Neck", box((0.24, -0.06, -0.07), (0.46, 0.06, 0.07))),
    ("Skull", box((0.44, -0.05, -0.06), (0.78, 0.05, 0.06))),
    ("Beak", box((0.76, -0.03, -0.03), (1.02, 0.03, 0.03))),
    ("Crest", box((0.18, 0.02, -0.02), (0.52, 0.28, 0.02))),
    ("Wing_L", box((-0.04, -0.02, -1.05), (0.22, 0.02, -0.12))),
    ("Wing_R", box((-0.04, -0.02, 0.12), (0.22, 0.02, 1.05))),
    ("WingFinger_L", box((0.10, -0.015, -0.92), (0.34, 0.015, -0.18))),
    ("WingFinger_R", box((0.10, -0.015, 0.18), (0.34, 0.015, 0.92))),
    ("Tail", box((-0.62, -0.04, -0.05), (-0.10, 0.04, 0.05))),
    ("Leg_L", box((0.02, -0.14, -0.18), (0.10, -0.02, -0.08))),
    ("Leg_R", box((0.02, -0.14, 0.08), (0.10, -0.02, 0.18))),
]


def pack_glb(gltf: dict, blob: bytes) -> bytes:
    jb = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    jb += b" " * ((4 - len(jb) % 4) % 4)
    blob += b"\x00" * ((4 - len(blob) % 4) % 4)
    total = 12 + 8 + len(jb) + 8 + len(blob)
    out = bytearray()
    out += struct.pack("<4sII", b"glTF", 2, total)
    out += struct.pack("<I4s", len(jb), b"JSON") + jb
    out += struct.pack("<I4s", len(blob), b"BIN\x00") + blob
    return bytes(out)


def build_glb() -> bytes:
    blob = bytearray()
    buffer_views, accessors, meshes, nodes = [], [], [], []
    for name, part in PARTS:
        pos, idx = triangulate(part)
        pb = b"".join(struct.pack("<fff", *p) for p in pos)
        ib = b"".join(struct.pack("<H", i) for i in idx)
        po, io = len(blob), len(blob) + len(pb)
        blob += pb + b"\x00" * ((4 - len(pb) % 4) % 4)
        blob += ib + b"\x00" * ((4 - len(ib) % 4) % 4)
        pv, iv = len(buffer_views), len(buffer_views) + 1
        buffer_views += [
            {"buffer": 0, "byteOffset": po, "byteLength": len(pb), "target": 34962},
            {"buffer": 0, "byteOffset": io, "byteLength": len(ib), "target": 34963},
        ]
        pa, ia = len(accessors), len(accessors) + 1
        accessors += [
            {
                "bufferView": pv,
                "componentType": 5126,
                "count": len(pos),
                "type": "VEC3",
                "min": [min(p[i] for p in pos) for i in range(3)],
                "max": [max(p[i] for p in pos) for i in range(3)],
            },
            {"bufferView": iv, "componentType": 5123, "count": len(idx), "type": "SCALAR"},
        ]
        mi = len(meshes)
        meshes.append({"name": name, "primitives": [{"attributes": {"POSITION": pa}, "indices": ia}]})
        nodes.append({"name": name, "mesh": mi})
    root = len(nodes)
    nodes.append({"name": "Pteranodon", "children": list(range(root))})
    gltf = {
        "asset": {"version": "2.0", "generator": "generate-pteranodon-glb.py"},
        "scene": 0,
        "scenes": [{"nodes": [root]}],
        "nodes": nodes,
        "meshes": meshes,
        "accessors": accessors,
        "bufferViews": buffer_views,
        "buffers": [{"byteLength": len(blob)}],
    }
    return pack_glb(gltf, bytes(blob))


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    data = build_glb()
    for name in ("Pteranodon.glb", "Pterosaur.glb"):
        path = OUT_DIR / name
        path.write_bytes(data)
        print(f"Wrote {path} ({path.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
