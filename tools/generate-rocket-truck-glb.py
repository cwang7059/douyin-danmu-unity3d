#!/usr/bin/env python3
"""Soviet MLRS truck silhouette (BM-21 / 红警 V3 火箭车) for Apocalypse King rocket trucks."""
from __future__ import annotations

import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "Resources" / "Vehicles" / "RocketTruck"


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


# Forward +X (cab at +X). Launcher rack at -X, elevated. Game bind may yaw -90 if X > Z.
PARTS = [
    ("Chassis", box((-0.92, 0.08, -0.42), (0.72, 0.28, 0.42))),
    ("Cab", box((0.38, 0.22, -0.34), (0.78, 0.52, 0.34))),
    ("CabRoof", box((0.42, 0.50, -0.30), (0.74, 0.58, 0.30))),
    ("Bed", box((-0.88, 0.24, -0.38), (-0.18, 0.30, 0.38))),
    ("LauncherFrame", box((-0.82, 0.30, -0.32), (-0.22, 0.38, 0.32))),
    ("LauncherRack", box((-0.78, 0.34, -0.30), (-0.26, 0.56, 0.30))),
    ("LauncherTop", box((-0.74, 0.52, -0.26), (-0.30, 0.60, 0.26))),
    ("Stabilizer_L", box((-0.48, 0.08, -0.52), (-0.36, 0.22, -0.44))),
    ("Stabilizer_R", box((-0.48, 0.08, 0.44), (-0.36, 0.22, 0.52))),
    ("Bumper", box((0.76, 0.12, -0.40), (0.86, 0.24, 0.40))),
    ("Wheel_FL", box((0.46, 0.02, -0.50), (0.62, 0.18, -0.34))),
    ("Wheel_FR", box((0.46, 0.02, 0.34), (0.62, 0.18, 0.50))),
    ("Wheel_ML", box((-0.08, 0.02, -0.50), (0.08, 0.18, -0.34))),
    ("Wheel_MR", box((-0.08, 0.02, 0.34), (0.08, 0.18, 0.50))),
    ("Wheel_RL", box((-0.62, 0.02, -0.50), (-0.46, 0.18, -0.34))),
    ("Wheel_RR", box((-0.62, 0.02, 0.34), (-0.46, 0.18, 0.50))),
]

# 4x4 rocket tubes on the rear rack (Red Alert MLRS read)
for row in range(4):
    for col in range(4):
        x0 = -0.72 + col * 0.11
        z0 = -0.20 + row * 0.11
        PARTS.append(
            (f"Tube_{row}_{col}", box((x0, 0.36, z0), (x0 + 0.06, 0.58, z0 + 0.06)))
        )


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


def material_index(part_name: str) -> int:
    lower = part_name.lower()
    if "wheel" in lower or "stabilizer" in lower:
        return 2
    if "tube" in lower or "launcher" in lower:
        return 1
    return 0


def build_glb() -> bytes:
    materials = [
        {
            "name": "Hull",
            "pbrMetallicRoughness": {
                "baseColorFactor": [0.32, 0.40, 0.26, 1.0],
                "metallicFactor": 0.05,
                "roughnessFactor": 0.88,
            },
        },
        {
            "name": "Launcher",
            "pbrMetallicRoughness": {
                "baseColorFactor": [0.24, 0.28, 0.20, 1.0],
                "metallicFactor": 0.12,
                "roughnessFactor": 0.82,
            },
        },
        {
            "name": "Rubber",
            "pbrMetallicRoughness": {
                "baseColorFactor": [0.14, 0.14, 0.14, 1.0],
                "metallicFactor": 0.0,
                "roughnessFactor": 0.95,
            },
        },
    ]

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
        meshes.append({
            "name": name,
            "primitives": [{
                "attributes": {"POSITION": pa},
                "indices": ia,
                "material": material_index(name),
            }],
        })
        nodes.append({"name": name, "mesh": mi})
    root = len(nodes)
    nodes.append({"name": "RocketTruck", "children": list(range(root))})
    gltf = {
        "asset": {"version": "2.0", "generator": "generate-rocket-truck-glb.py"},
        "scene": 0,
        "scenes": [{"nodes": [root]}],
        "nodes": nodes,
        "meshes": meshes,
        "materials": materials,
        "accessors": accessors,
        "bufferViews": buffer_views,
        "buffers": [{"byteLength": len(blob)}],
    }
    return pack_glb(gltf, bytes(blob))


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    data = build_glb()
    path = OUT_DIR / "RocketTruck.glb"
    path.write_bytes(data)
    print(f"Wrote {path} ({path.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
