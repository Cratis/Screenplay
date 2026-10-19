#!/bin/bash
# Builds the RENDERABLE VARIANT of Acme Commerce and renders it with `cratis render`.
# The source of truth (this folder, compiled with screenplay --warnaserror) is NOT changed; the variant differs only where the
# renderer (Stage / cratis) cannot yet take what the language says. Every difference is listed here, with its reason:
#   0. DOMAINS (default "Catalog Storefront"): only the domains Stage can render today are in the variant, with their shell navigation.
#   1. translation/ folders are deleted: Stage renders StateChange/StateView/Automation slices, not Translate slices or
#      public events (STAGE-ESM-024/032/033; Stage issues #79/#212/#213). `cratis render --scope` cannot skip them (a scoped render
#      omits the application shell). The domains are bridged instead by tools/render-bridge/bridge.play.txt: reactions that
#      invoke the receiving domain's command, and the private events of the receiving domain declared in a StateChange slice.
#   2. LEGACY_TEXT_RULES=1 (cratis 3.71.0 only, admits ESM <= v20): concept text rules (starts with / contains / not contains,
#      ESM v24) become `matches` rules. Not needed on cratis >= 3.75.0.
#   3. A command whose event triggers a reaction cannot have a rendered specification (STAGE-ESM-011): those specifications
#      are listed in tools/render-bridge/drop-specifications.txt (one `<file>|<specification name>` per line) and removed.
#   5. `reaction batches atomic` (ESM v22) and `command reads protected` / command `reads` (ESM v25) render: nothing is stripped
#      (STRIP_READS=1 runs tools/render-bridge/strip_reads.py, the old behaviour for a renderer older than Stage#566/#574).
#   6. A query a `performer` answers (Purchasing dashboard) is an opaque read Stage cannot render (STAGE-ESM-007): the figures are projected from the
#      events instead. `add x by a * b` (ESM v29, Storefront BasketSize.addedValue) is refused by STAGE-ESM-050 (Chronicle adds one event property):
#      the total is left out. Both in tools/render-bridge/stand_in_for_unrendered.py.
#   4. The custom package Acme.Commerce needs `cratis render --package-set tools/packages/acme-commerce.package-set.json` and the
#      built npm package _packages/scene.commerce. Without PACKAGE_SET=1 the three acme: components are replaced by core
#      tables / the package is removed from the profile (render still works, the custom components are missing).
#
# usage:  tools/render-variant.sh [--destination <dir>] [-- <extra cratis render args>]
#   W=/tmp/acme-render         work folder (variant source in $W/AcmeRender, rendered app in $W/out)
#   CRATIS=cratis              the cratis CLI
#   LEGACY_TEXT_RULES=1        see 2        PACKAGE_SET=1   see 4
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"; SAMPLE="$(dirname "$HERE")"
W=${W:-${TMPDIR:-/tmp}/acme-render}; CRATIS=${CRATIS:-cratis}; DEST="$W/out"
if [ "$1" = "--destination" ]; then DEST=$2; shift 2; fi
[ "$1" = "--" ] && shift
rm -rf "$W/AcmeRender"; mkdir -p "$W"
rsync -a --exclude tools --exclude _packages --exclude README.md "$SAMPLE/" "$W/AcmeRender/"
V="$W/AcmeRender"
# 0. which domains: Stage cannot render every domain of the application yet (see 5.), so DOMAINS picks the ones in the variant
DOMAINS=${DOMAINS:-"Catalog Storefront"}
python3 - "$V" "$DOMAINS" <<'PY'
import sys,re,os,shutil
v=sys.argv[1]; keep=sys.argv[2].split()
modules={'Catalog':'Merchandising','Storefront':'Shop','Sales':'Orders','Purchasing':'Buying','Warehouse':'Stock'}
for d,m in modules.items():
    if d not in keep: shutil.rmtree(v+'/'+d,ignore_errors=True)
gone=[m for d,m in modules.items() if d not in keep]
p=v+'/Shell/navigation.play'; lines=open(p).read().split('\n'); out=[]; skip=None
for l in lines:
    ind=len(l)-len(l.lstrip())
    if skip is not None:
        if l.strip()=='' or ind>skip: continue
        skip=None
    if l.strip().startswith('item') and re.search(r'target (%s)\b'%'|'.join(gone),l):
        skip=ind; continue
    # a toolbar button of the application shell that starts a slice of a module that is not in the variant
    if gone and l.strip().startswith('button') and re.search(r'\bslice (%s)\.'%'|'.join(gone),l):
        skip=ind; continue
    out.append(l)
open(p,'w').write('\n'.join(out))
PY
# 1. no Translate slices in the rendered variant: the inbound captures become bridge commands first, then translation/ goes
python3 "$HERE/render-bridge/bridge_translations.py" "$V"
rm -rf "$V"/*/translation
# 1b. what Stage cannot render yet: atomic reaction batches (ESM v22), protected decision reads (ESM v25)
# Stage (>= the release carrying #566/#574) renders both, so nothing is stripped unless STRIP_READS=1 asks for the old behaviour
if [ "$STRIP_READS" = "1" ]; then python3 "$HERE/render-bridge/strip_reads.py" "$V"; fi
# 1c. stand-ins for what Stage cannot render yet: a performer query (STAGE-ESM-007), a product operand in a projection (STAGE-ESM-050)
python3 "$HERE/render-bridge/stand_in_for_unrendered.py" "$V"
# 2. legacy text rules
if [ "$LEGACY_TEXT_RULES" = "1" ]; then
python3 - "$V" <<'PY'
import sys,re
p=sys.argv[1]+'/Shared/concepts.play'; t=open(p).read()
t=re.sub(r'starts with "([^"]+)"',lambda m:'matches "^'+re.escape(m.group(1))+'"',t)
t=t.replace('not contains " "     message','matches "^[^ ]*$"     message').replace('not contains " "  message','matches "^[^ ]*$"  message')
t=re.sub(r'contains "@"','matches email',t)
open(p,'w').write(t)
PY
fi
# 3. specifications the renderer cannot take
if [ -f "$HERE/render-bridge/drop-specifications.txt" ]; then
python3 - "$V" "$HERE/render-bridge/drop-specifications.txt" <<'PY'
import sys,re
root=sys.argv[1]
for line in open(sys.argv[2]):
    line=line.strip()
    if not line or line.startswith('#'): continue
    f,name=line.split('|'); p=root+'/'+f; t=open(p).read()
    m=re.search(r'\n( *)specification '+re.escape(name)+r'\n',t)
    if not m: print('not found:',line); continue
    ind=len(m.group(1)); a=m.start()+1; rest=t[m.end():]
    n=re.search(r'\n(?= {0,%d}[^ \n])'%ind,rest)
    b=m.end()+(n.start()+1 if n else len(rest))
    open(p,'w').write(t[:a]+t[b:])
PY
fi
# 4. the custom package
if [ "$PACKAGE_SET" != "1" ]; then
  sed -i.bak '/^    Acme.Commerce$/d' "$V/Shell/shell.play"; rm -f "$V/Shell/shell.play.bak"
  sed -i.bak '/compatible with Acme.Commerce/d' "$V/Shared/theme.play"; rm -f "$V/Shared/theme.play.bak"
  python3 - "$V" <<'PY'
import sys,re,glob
for p in glob.glob(sys.argv[1]+'/**/*.play',recursive=True):
    t=open(p).read()
    n=re.sub(r'\n( *)component Acme\.Commerce\.[A-Za-z]+ [A-Za-z]+\n(?:\1 +[^\n]*\n)*',lambda m:'\n',t)
    if n!=t: open(p,'w').write(n)
PY
fi
# the bridge: how the rendered application connects the domains (reactions instead of public events)
if [ -f "$HERE/render-bridge/bridge.play.txt" ]; then mkdir -p "$V/RenderBridge"; cp "$HERE/render-bridge/bridge.play.txt" "$V/RenderBridge/bridge.play"; fi
rm -rf "$DEST"
ARGS=""; [ "$PACKAGE_SET" = "1" ] && ARGS="--package-set $HERE/packages/acme-commerce.package-set.json"
$CRATIS render "$V" --target cratis --name AcmeCommerce --destination "$DEST" $ARGS "$@"
echo "rendered to $DEST - next: (cd $DEST && dotnet build && npm install && npm run build && dotnet test AcmeCommerce.Specs)"
