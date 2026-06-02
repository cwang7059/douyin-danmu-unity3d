#!/usr/bin/env python3
"""Remove Sketchfab display meshes (floor/logo/shadow/smoke) from T-55 OBJ exports."""
from __future__ import annotations

import re
import sys
from pathlib import Path

DROP_MATERIALS = {
    "default_material",
    "logo",
    "shadow",
    "smoke",
    "floor",
}

DROP_MTL_NAMES = DROP_MATERIALS


def clean_obj(text: str) -> str:
    lines = text.splitlines()
    out: list[str] = []
    drop = False
    for line in lines:
        stripped = line.strip()
        if stripped.startswith("usemtl "):
            name = stripped[7:].strip().lower()
            drop = name in DROP_MATERIALS
            if drop:
                continue
        if drop and stripped and stripped[0] in "fvg":
            continue
        out.append(line)
    return "\n".join(out) + ("\n" if text.endswith("\n") else "")


def clean_mtl(text: str) -> str:
    blocks = re.split(r"(?=^newmtl )", text, flags=re.MULTILINE)
    kept: list[str] = []
    for block in blocks:
        if not block.strip():
            continue
        first = block.strip().splitlines()[0]
        if first.startswith("newmtl "):
            name = first[7:].strip().lower()
            if name in DROP_MTL_NAMES:
                continue
        kept.append(block)
    return "".join(kept)


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: clean-t55-obj.py <file.obj> [file.mtl]")
        return 1

    obj_path = Path(sys.argv[1])
    obj_text = obj_path.read_text(encoding="utf-8", errors="replace")
    cleaned = clean_obj(obj_text)
    obj_path.write_text(cleaned, encoding="utf-8")
    print(f"[clean-t55] {obj_path.name}: removed display usemtl blocks")

    mtl_path = Path(sys.argv[2]) if len(sys.argv) > 2 else obj_path.with_suffix(".mtl")
    if mtl_path.is_file():
        mtl_text = mtl_path.read_text(encoding="utf-8", errors="replace")
        mtl_path.write_text(clean_mtl(mtl_text), encoding="utf-8")
        print(f"[clean-t55] {mtl_path.name}: trimmed display materials")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
