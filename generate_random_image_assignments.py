#!/usr/bin/env python3
"""
Reads cloudinary_remaining_results.json and generates SQL to randomly
assign the new uploads as additional ProductImages on existing products.
"""

import json, random, sys
from pathlib import Path

RESULTS_FILE = Path("cloudinary_remaining_results.json")
OUTPUT_FILE  = Path("Infrastructure/Persistence/Seeds/SeedRemainingProductImages.sql")

PRODUCT_IDS = list(range(4, 24))  # ids 4-23 (1-3 were deleted)

# Current max image id is 20 (from the first seed run)
IMAGE_START_ID = 21

random.seed(42)  # reproducible


def main():
    if not RESULTS_FILE.exists():
        print(f"ERROR: {RESULTS_FILE} not found.")
        sys.exit(1)

    with open(RESULTS_FILE) as f:
        uploads = json.load(f)

    if not uploads:
        print("No results found.")
        sys.exit(1)

    print(f"Assigning {len(uploads)} images randomly to products 1-23...")

    # Track how many extra images each product already has so DisplayOrder increments
    display_orders: dict[int, int] = {pid: 2 for pid in PRODUCT_IDS}

    rows = []
    for i, asset in enumerate(uploads):
        pub_id: str  = asset.get("public_id", "")
        url: str     = asset.get("secure_url", "")
        width: int   = asset.get("width", 0)
        height: int  = asset.get("height", 0)
        aspect       = round(width / height, 4) if height else 1.0

        product_id   = random.choice(PRODUCT_IDS)
        image_id     = IMAGE_START_ID + i
        order        = display_orders[product_id]
        display_orders[product_id] += 1

        alt = pub_id.split("/")[-1].replace("_", " ").title()

        rows.append(
            f"({image_id}, {product_id}, '{alt}', FALSE, {order}, "
            f"'{pub_id}', '{url}', {aspect}, {width}, {height}, NULL)"
        )

    lines = [
        "-- Auto-generated: additional product images from remaining Cloudinary uploads",
        "",
        "INSERT INTO ProductImages",
        "(Id, ProductId, AltText, IsPrimary, DisplayOrder, PublicId, BaseUrl, AspectRatio, Width, Height, PlaceholderUrl)",
        "OVERRIDING SYSTEM VALUE",
        "VALUES",
        ",\n".join(rows) + ";",
        "",
        "SELECT setval(pg_get_serial_sequence('productimages', 'id'), COALESCE(MAX(id), 1)) FROM productimages;",
    ]

    OUTPUT_FILE.write_text("\n".join(lines))
    print(f"SQL written to: {OUTPUT_FILE}")
    print(f"  {len(rows)} image rows across {len(set(r.split(',')[1].strip() for r in rows))} products")


if __name__ == "__main__":
    main()
