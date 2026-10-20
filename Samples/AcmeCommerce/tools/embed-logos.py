#!/usr/bin/env python3
"""Inlines assets/logos/<domain>.svg as a data URI in two places of each domain package, so the logo travels inside the text:
the `logo image` line of <Domain>/shell/navigation.play (the topbar) and the `identity` block of the `ui profile` in <Domain>/shell/shell.play
(the application's own logo, which every blueprint draws in its sidebar, rail or top bar, beside the name, corners and font).
Studio and Stage take a Screenplay document only (never a file beside it), and a data URI cannot 404.
usage: tools/embed-logos.py   (run after editing an SVG or the table below; idempotent)"""
import base64, re, pathlib

root = pathlib.Path(__file__).resolve().parent.parent

# domain: (application name, show the name beside the logo, corners, font)
identities = {
    "Storefront": ("Acme Shop", True, "round", "source-serif-4"),
    "Catalog": ("Acme Catalog", True, "default", "inter"),
    "Sales": ("Acme Sales", True, "sharp", "ibm-plex-sans"),
    "Purchasing": ("Acme Purchasing", False, "default", "ibm-plex-sans"),
    "Warehouse": ("Acme Warehouse", False, "sharp", "inter"),
}

for domain, (name, show_name, corners, font) in identities.items():
    svg = (root / "assets" / "logos" / f"{domain.lower()}.svg").read_bytes()
    uri = "data:image/svg+xml;base64," + base64.b64encode(svg).decode()

    nav = root / domain / "shell" / "navigation.play"
    text = nav.read_text()
    new, n = re.subn(r'logo image "[^"]*"', lambda m: f'logo image "{uri}"', text, count=1)
    assert n == 1, f"{nav}: no logo line"
    nav.write_text(new)

    shell = root / domain / "shell" / "shell.play"
    text = shell.read_text()
    block = f'  identity\n    name "{name}"\n    logo image "{uri}"\n' + ("" if show_name else "    show name false\n") + "\n"
    text = re.sub(r'  identity\n(?:    .*\n)+\n', "", text)
    text, n = re.subn(r'(  appearance\n)', lambda m: block + m.group(1), text, count=1)
    assert n == 1, f"{shell}: no appearance block"
    text = re.sub(r'(\n    (?:corners|font) .*)', "", text)
    text, n = re.subn(r'(    accent "[^"]*")', lambda m: f'{m.group(1)}\n    corners {corners}\n    font {font}', text, count=1)
    assert n == 1, f"{shell}: no accent line"
    shell.write_text(text)
    print(f"{domain}: {len(uri)} characters")
