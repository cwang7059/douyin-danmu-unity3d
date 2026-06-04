#!/usr/bin/env python3
"""Low-poly winged creature GLB for zombie pterosaur units."""
from __future__ import annotations

import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Resources" / "Monsters" / "Pterosaur" / "Pterosaur.glb"


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


PARTS = [
    ("Body", box((-0.18, -0.12, -0.32), (0.18, 0.12, 0.32))),
    ("Head", box((0.22, -0.08, -0.1), (0.42, 0.1, 0.1))),
    ("Wing_L", box((-0.06, -0.04, -0.72), (0.06, 0.04, -0.08))),
    ("Wing_R", box((-0.06, -0.04, 0.08), (0.06, 0.04, 0.72))),
    ("Tail", box((-0.52, -0.05, -0.06), (-0.22, 0.05, 0.06))),
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


def main() -> None:
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
            {"bufferView": pv, "componentType": 5126, "count": len(pos), "type": "VEC3",
             "min": [min(p[i] for p in pos) for i in range(3)],
             "max": [max(p[i] for p in pos) for i in range(3)]},
            {"bufferView": iv, "componentType": 5123, "count": len(idx), "type": "SCALAR"},
        ]
        mi = len(meshes)
        meshes.append({"name": name, "primitives": [{"attributes": {"POSITION": pa}, "indices": ia}]})
        nodes.append({"name": name, "mesh": mi})
    root = len(nodes)
    nodes.append({"name": "Pterosaur", "children": list(range(root))})
    gltf = {
        "asset": {"version": "2.0", "generator": "generate-pterosaur-glb.py"},
        "scene": 0,
        "scenes": [{"nodes": [root]}],
        "nodes": nodes,
        "meshes": meshes,
        "accessors": accessors,
        "bufferViews": buffer_views,
        "buffers": [{"byteLength": len(blob)}],
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_bytes(pack_glb(gltf, bytes(blob)))
    print(f"Wrote {OUT} ({OUT.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
