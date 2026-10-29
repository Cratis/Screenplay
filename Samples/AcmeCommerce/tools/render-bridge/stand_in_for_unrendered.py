#!/usr/bin/env python3
"""Render variant step: stand-ins for two constructs Stage does not render yet. Both are listed in tools/render-variant.sh.

1. A query answered by a `performer` (an opaque read, Screenplay#862) has no projection, and Stage renders a state view only when every
   read model has exactly one projection or reducer (STAGE-ESM-007). The dashboard read model is projected from the events instead - the
   shape the sample used before the performer: every `from` states every counter, because any event may be the first one a row sees.
2. `add total by unitPrice * quantity` (ESM v29) adds a product of two event values; Chronicle's projection engine adds one event property
   (STAGE-ESM-050). The total is left out of the variant: its mapping, its read-model property, the screen field and the assertions -
   only inside the slice that holds the product, because the same name can be a command or event field in another slice.
usage: stand_in_for_unrendered.py <variant-folder>
"""
import re, sys, glob

root = sys.argv[1]
def read(p): return open(p).read()
def write(p, t): open(p, 'w').write(t)

PURCHASING_FIGURES = '''      query CurrentPurchasingFigures => observable PurchasingFigures[]

      projection PurchasingFiguresProjection => PurchasingFigures
        from PurchaseOrderCreated
          key "global"
          summaryId = "global"
          increment orders
          add suppliers by 0
          add unitsOrdered by 0
          add unitsReceived by 0
        from SupplierRegistered
          key "global"
          summaryId = "global"
          add orders by 0
          increment suppliers
          add unitsOrdered by 0
          add unitsReceived by 0
        from PurchaseOrderLineAdded
          key "global"
          summaryId = "global"
          add orders by 0
          add suppliers by 0
          add unitsOrdered by quantity
          add unitsReceived by 0
        from PurchaseOrderGoodsReceived
          key "global"
          summaryId = "global"
          add orders by 0
          add suppliers by 0
          add unitsOrdered by 0
          add unitsReceived by quantity
'''

stood_in = 0
for f in glob.glob(root + '/*/model/*.play'):
    t = read(f); o = t
    # 1. performer queries: only the Purchasing dashboard has one
    m = re.search(r'(?m)^( *)query CurrentPurchasingFigures => observable PurchasingFigures\[\]\n(?:\1  .*\n|\n)*?(?=\1\S|\n\1\S|\1screen)', t)
    if m and 'performer' in m.group(0):
        t = t[:m.start()] + PURCHASING_FIGURES + '\n' + t[m.end():].lstrip('\n'); stood_in += 1
    # 2. products: drop the target within the slice that holds the product, and nowhere else (the same name can be a command or event field)
    lines = t.splitlines(keepends=True); out = []; i = 0
    while i < len(lines):
        sm = re.match(r'( *)slice ', lines[i])
        if not sm:
            out.append(lines[i]); i += 1; continue
        indent = len(sm.group(1)); j = i + 1
        while j < len(lines) and (not lines[j].strip() or len(lines[j]) - len(lines[j].lstrip(' ')) > indent): j += 1
        s = ''.join(lines[i:j])
        for pm in list(re.finditer(r'(?m)^ *(?:add|subtract) (\w+) by \w+ \* \w+\n', s)):
            target = pm.group(1)
            s = s.replace(pm.group(0), '')
            s = re.sub(r'(?m)^ *%s +\S+( optional)?\n' % target, '', s)            # the read-model property
            s = re.sub(r'(?m)^ *field %s +label .*\n' % target, '', s)            # a summary field of a screen
            s = re.sub(r'(?m)^ *column %s +label .*\n' % target, '', s)           # a table column
            s = re.sub(r'(?m)^ *%s += .*\n' % target, '', s)                      # an expected value
            stood_in += 1
        out.append(s); i = j
    t = ''.join(out)
    if t != o: write(f, t)
print('stand_in_for_unrendered: %d construct(s) replaced' % stood_in)
