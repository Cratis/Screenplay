#!/usr/bin/env python3
"""Inlines assets/logos/<domain>.svg as the data URI of the `logo image` line of <Domain>/shell/navigation.play.
Studio and Stage take a Screenplay document only (never a file beside it), so the logo travels inside the text.
usage: tools/embed-logos.py   (run after editing an SVG; idempotent)"""
import base64, re, pathlib
root = pathlib.Path(__file__).resolve().parent.parent
for domain in ("Storefront", "Catalog", "Sales", "Purchasing", "Warehouse"):
    svg = (root / "assets" / "logos" / f"{domain.lower()}.svg").read_bytes()
    uri = "data:image/svg+xml;base64," + base64.b64encode(svg).decode()
    nav = root / domain / "shell" / "navigation.play"
    text = nav.read_text()
    new, n = re.subn(r'logo image "[^"]*"', lambda m: f'logo image "{uri}"', text, count=1)
    assert n == 1, f"{nav}: no logo line"
    nav.write_text(new)
    print(f"{domain}: {len(uri)} characters")
