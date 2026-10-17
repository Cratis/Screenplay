#!/usr/bin/env python3
"""RENDER VARIANT ONLY. cratis/Stage 3.75 refuses `command reads protected` (STAGE-ESM-044: the renderer does not preserve protected
decision reads yet). This turns every command read into what a client can send:
  - `reads X by y` lines and `require ...` rules (with their message/severity lines) are removed: the rendered backend does not guard;
  - a produced-event mapping `p = Read.prop` becomes a command INPUT of the same name (the table below gives its type);
  - the root `command reads protected` line is removed.
usage: strip-protected-reads.py <variant-root>
Each domain author lists the inputs its commands need in EXTRA_INPUTS (file | command | property | type)."""
import sys, re, glob

EXTRA_INPUTS = [
    # Catalog (the approval carries a snapshot the client has to send in the render variant)
    ('Catalog/model/publication.play', 'ApproveProductForSale', 'sku',      'Sku'),
    ('Catalog/model/publication.play', 'ApproveProductForSale', 'name',     'ProductName'),
    ('Catalog/model/publication.play', 'ApproveProductForSale', 'category', 'String'),
    ('Catalog/model/publication.play', 'ApproveProductForSale', 'details',  'String'),
    ('Catalog/model/publication.play', 'ApproveProductForSale', 'imageUrl', 'String'),
    ('Catalog/model/publication.play', 'ApproveProductForSale', 'price',    'Money'),
    ('Catalog/model/publication.play', 'ApproveProductForSale', 'currency', 'CurrencyCode'),
    ('Catalog/model/publication.play', 'WithdrawProduct',       'sku',      'Sku'),
    # Storefront
    ('Storefront/model/basket.play',   'AddToBasket',    'unitPrice',   'Money'),
    ('Storefront/model/checkout.play', 'CheckOut',       'email',       'EmailAddress'),
    # Sales: ConfirmOrder copies what Sales decided on from the order state; a client has to send it
    ('Sales/model/approval.play',      'ConfirmOrder',   'lines',       'OrderLine[]'),
    ('Sales/model/approval.play',      'ConfirmOrder',   'shipTo',      'PostalAddress'),
    ('Sales/model/approval.play',      'ConfirmOrder',   'market',      'Market'),
    ('Sales/model/approval.play',      'ConfirmOrder',   'quotedTotal', 'Money'),
]
# Automations whose command now needs inputs the trigger cannot carry are not rendered (the confirmation becomes a manual step)
REMOVE_SLICES = [('Sales/model/approval.play', 'Automation ConfirmApprovedOrder'),
                 # the decision state exists for protected reads only; it maps composite values (lines, shipTo) a rendered projection cannot hold
                 ('Sales/model/state.play', 'StateView OrderState')]
ROOT_LINE = 'command reads protected\n'

def command_blocks(text):
    """yield (start, end) of every `command X` block (the block ends at the next line with the same or smaller indent)."""
    for m in re.finditer(r'^( *)command (\w+)\n', text, re.M):
        ind = len(m.group(1)); a = m.start(); rest = text[m.end():]
        n = re.search(r'\n(?= {0,%d}[^ \n])' % ind, rest)
        b = m.end() + (n.start() + 1 if n else len(rest))
        yield a, b, m.group(2), ind

def strip(path, root):
    text = open(path).read(); orig = text
    if path.endswith('application.play'):
        text = text.replace(ROOT_LINE, '')
        text = text.replace('reaction batches atomic\n', '')   # STAGE-ESM-043: Stage does not preserve the atomic reaction boundary (Cratis/Stage#422)
    for f, header in REMOVE_SLICES:
        if path.endswith(f):
            m = re.search(r'^( *)slice %s\n' % re.escape(header), text, re.M)
            if m:
                ind = len(m.group(1)); rest = text[m.end():]
                n = re.search(r'\n(?= {0,%d}[^ \n])' % ind, rest)
                text = text[:m.start()] + text[m.end() + (n.start() + 1 if n else len(rest)):]
    out = []; last = 0
    for a, b, name, ind in command_blocks(text):
        blk = text[a:b]; pad = ' ' * (ind + 2)
        blk = re.sub(r'^ *reads \w+( as \w+)? by \w+\n', '', blk, flags=re.M)
        # validate: drop `require` rules and their continuation lines; drop the validate header when nothing is left
        lines = blk.split('\n'); keep = []; i = 0
        while i < len(lines):
            l = lines[i]
            if re.match(r'^ *require ', l):
                li = len(l) - len(l.lstrip()); i += 1
                while i < len(lines) and (len(lines[i]) - len(lines[i].lstrip())) > li and lines[i].strip(): i += 1
                continue
            keep.append(l); i += 1
        blk = '\n'.join(keep)
        blk = re.sub(r'^ *validate\n(?= *(produces|stream|authorize|\n|$))', '', blk, flags=re.M)
        # read paths in mappings -> inputs
        added = []
        def repl(m):
            prop = m.group(1)
            added.append(prop); return '%s%s = %s' % (m.group(0)[:len(m.group(0)) - len(m.group(0).lstrip())], prop, prop)
        blk = re.sub(r'^ +(\w+) += +[A-Z]\w*\.\w+$', repl, blk, flags=re.M)
        for f, cmd, prop, typ in EXTRA_INPUTS:
            if path.endswith(f) and cmd == name and prop in added:
                # insert after the first property line (the identifier)
                blk = re.sub(r'^( *\w+ +\w+ identifier\n)', lambda m: m.group(1) + '%s%s %s\n' % (pad, prop, typ), blk, count=1, flags=re.M)
        out.append((a, b, blk))
    for a, b, blk in reversed(out):
        text = text[:a] + blk + text[b:]
    if text != orig: open(path, 'w').write(text)

root = sys.argv[1]
for p in glob.glob(root + '/**/*.play', recursive=True): strip(p, root)
