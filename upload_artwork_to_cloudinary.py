#!/usr/bin/env python3
"""
Uploads Ria Mukharjee's artwork photos to Cloudinary from the catalogue proposal.

Unlike upload_illustrations.sh this reads credentials from the environment rather than
hardcoding them, and derives public_id from the artwork slug rather than the source
filename (WhatsApp filenames contain spaces, dots and parentheses).

  CLOUDINARY_CLOUD_NAME / CLOUDINARY_API_KEY / CLOUDINARY_API_SECRET

Usage:
  python3 upload_artwork_to_cloudinary.py --limit 1      # smoke-test a single upload
  python3 upload_artwork_to_cloudinary.py                # upload everything selected
"""
import argparse
import json
import os
import subprocess
import sys
from pathlib import Path

FOLDER = "blackinkpaper_art"
SRC = Path("BlackInkPaperAPIService/wwwroot/assets/blackinkpaper-art")
CATALOG = Path("docs/artwork-catalog-proposal.json")
RESULTS = Path("cloudinary_artwork_results.json")


def selected_uploads():
    """Hero shot first for each artwork, then its alternate finished angles."""
    catalog = json.loads(CATALOG.read_text())
    items = []
    for art in catalog["artworks"]:
        items.append((art["slug"], art["hero_file"], "hero"))
        for n, fname in enumerate(art["alt_files"], start=2):
            items.append((f"{art['slug']}-{n}", fname, "alt"))
    return items


def upload(public_id: str, filename: str, cloud: str, key: str, secret: str) -> dict:
    path = SRC / filename
    if not path.exists():
        raise FileNotFoundError(path)
    proc = subprocess.run(
        ["curl", "-s", "--max-time", "180",
         f"https://{key}:{secret}@api.cloudinary.com/v1_1/{cloud}/image/upload",
         "-X", "POST",
         "-F", f"file=@{path}",
         "-F", f"folder={FOLDER}",
         "-F", f"public_id={public_id}",
         "-F", "resource_type=image"],
        capture_output=True, text=True)
    return json.loads(proc.stdout)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--limit", type=int, default=0, help="upload at most N images")
    args = ap.parse_args()

    cloud = os.environ.get("CLOUDINARY_CLOUD_NAME")
    key = os.environ.get("CLOUDINARY_API_KEY")
    secret = os.environ.get("CLOUDINARY_API_SECRET")
    if not all((cloud, key, secret)):
        sys.exit("Set CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY and CLOUDINARY_API_SECRET.")

    items = selected_uploads()
    if args.limit:
        items = items[: args.limit]

    results, failed = [], 0
    for public_id, filename, role in items:
        res = upload(public_id, filename, cloud, key, secret)
        if "secure_url" in res:
            print(f"  OK  {FOLDER}/{public_id}  ({role})")
            results.append({"public_id_requested": public_id, "role": role,
                            "source_file": filename, **res})
        else:
            failed += 1
            print(f"  FAIL {public_id}: {res.get('error', {}).get('message', res)}")

    RESULTS.write_text(json.dumps(results, indent=1))
    print(f"\n{len(results)} uploaded, {failed} failed -> {RESULTS}")


if __name__ == "__main__":
    main()
