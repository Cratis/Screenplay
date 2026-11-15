#!/usr/bin/env python3
"""Render variant step: a stand-in for a construct Stage does not render yet. Listed in tools/render-variant.sh.

A query answered by a `performer` (an opaque read, Screenplay#862) has no projection, and Stage renders a state view only when every
read model has exactly one projection or reducer (STAGE-ESM-007). The dashboard read model is projected from the events instead - the
shape the sample used before the performer: every `from` states every counter, because any event may be the first one a row sees.
`add x by a * b` (ESM v29) is rendered since Stage#597, so the variant keeps it.
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
    t = read(f)
    # performer queries: only the Purchasing dashboard has one
    m = re.search(r'(?m)^( *)query CurrentPurchasingFigures => observable PurchasingFigures\[\]\n(?:\1  .*\n|\n)*?(?=\1\S|\n\1\S|\1screen)', t)
    if m and 'performer' in m.group(0):
        t = t[:m.start()] + PURCHASING_FIGURES + '\n' + t[m.end():].lstrip('\n'); stood_in += 1
        write(f, t)
print('stand_in_for_unrendered: %d construct(s) replaced' % stood_in)
