#!/usr/bin/env python3
"""Render variant step: replace the Translate slices (Stage renders neither Translate slices nor public events) by what a single
event store can do - a bridge. Run it BEFORE the translation/ folders are deleted.

For every inbound capture `key K` / `append E`: the private event E (declared in a StateView slice of its domain, where Stage would not
generate its [EventType]) moves into a StateChange slice `Bridge<E>` whose command `Bridge<E>` appends it. The command is the bridge's
entry point: a reaction elsewhere could invoke it; none is generated because a reaction only carries payload values.
Specifications that mention a public event (or an event only a capture appended) cannot render, so they are removed.
usage: bridge_translations.py <variant-folder>
"""
import re, sys, glob, os

root = sys.argv[1]
def read(p): return open(p).read()
def write(p, t): open(p, 'w').write(t)
def indent(l): return len(l) - len(l.lstrip(' '))

public = set()      # public events (outbound) and foreign contracts (inbound)
captures = []       # (domain, module, feature, key, keyType, event)
for tf in glob.glob(root + '/*/translation/*.play'):
    domain = tf.split('/')[-3]
    t = read(tf)
    for m in re.finditer(r'^ +public event (\w+)', t, flags=re.M): public.add(m.group(1))
    for m in re.finditer(r'^ +event (\w+) from "', t, flags=re.M): public.add(m.group(1))
    mod = re.search(r'^module (\w+)', t, flags=re.M)
    cur_feature = None
    for line in t.split('\n'):
        f = re.match(r'^  feature (\w+)', line)
        if f: cur_feature = f.group(1)
    for sl in re.split(r'(?m)^(?=    slice )', t)[1:]:
        if 'direction inbound' not in sl: continue
        foreign = {}
        for em in re.finditer(r'(?m)^      event (\w+) from "\w+"\n((?:        .*\n)+)', sl):
            foreign[em.group(1)] = dict(re.findall(r'(?m)^        (\w+)\s+(\S+)', em.group(2)))
        for cm in re.finditer(r'(?m)^      capture \w+\n((?:        .*\n|\n)+)', sl):
            body = cm.group(1)
            src = re.search(r'from (\w+)', body); key = re.search(r'^\s+key (\w+)', body, flags=re.M)
            if not (src and key): continue
            ktype = foreign.get(src.group(1), {}).get(key.group(1))
            for ap in re.finditer(r'^\s+append (\w+)', body, flags=re.M):
                captures.append((domain, mod.group(1), None, key.group(1), ktype, ap.group(1)))

# find each appended private event's declaration in a model file (or in the translation slice itself) and move it into a bridge slice
bridge_by_file = {}
for domain, module, _, key, ktype, event in captures:
    for mf in glob.glob('%s/%s/model/*.play' % (root, domain)) + glob.glob('%s/%s/translation/*.play' % (root, domain)):
        t = read(mf)
        m = re.search(r'(?m)^( +)event %s\n((?:\1  .*\n|\n)*)' % event, t)
        if not m: continue
        # the feature of that slice
        before = t[:m.start()]
        feat = re.findall(r'(?m)^  feature (\w+)', before)[-1]
        decl = m.group(0)
        props = [(a, b) for a, b in re.findall(r'(?m)^\s{8}(\w+)\s+(\S+)\s*$', m.group(2)) if a not in ('description',)]
        t = t[:m.start()] + t[m.end():]
        write(mf, t)
        # a command of the model that also produces this event addresses it by its own property (`for sku`): the bridge must use the same
        # destination, otherwise a specification's `for "..."` is ambiguous (PLAY0273)
        for other in glob.glob('%s/%s/model/*.play' % (root, domain)):
            om = re.search(r'(?m)^ +produces (?:event )?%s\n +for (\w+)\n' % event, read(other))
            if om and om.group(1) != key and om.group(1) in dict(props):
                ktype, key = dict(props)[om.group(1)], om.group(1)
                break
        lines = ['    slice StateChange Bridge%s' % event,
                 '      description "Render bridge: stands in for the inbound translation of %s"' % event,
                 '      command Bridge%s' % event,
                 '        %s %s identifier' % (key, ktype or 'String')]
        lines += ['        %s %s' % (a, b) for a, b in props if a != key]
        lines += ['        produces %s' % event, '          for %s' % key]
        lines += ['          %s = %s' % (a, a) for a, b in props]
        lines += ['', '      event %s' % event]
        lines += ['        %s %s' % (a, b) for a, b in props]
        bridge_by_file.setdefault((domain, module, feat), []).extend(lines + [''])
        break

for (domain, module, feat), lines in bridge_by_file.items():
    out = '%s/%s/model/bridge.play' % (root, domain)
    head = '' if os.path.exists(out) and 'module %s' % module in read(out) else 'module %s\n' % module
    prev = read(out) if os.path.exists(out) else ''
    write(out, prev + head + '  feature %s\n' % feat + '\n'.join(lines) + '\n')

# specifications that mention a public event or a capture-only event cannot render
names = set(public)
for mf in glob.glob(root + '/*/model/*.play'):
    t = read(mf); changed = False
    for sm in list(re.finditer(r'(?m)^( *)specification (\w+)\n((?:\1 .*\n|\n)*)', t)):
        body = sm.group(3)
        if any(re.search(r'\b%s\b' % n, body) for n in names):
            ind = len(sm.group(1)); m = re.search(r'(?m)^ *specification %s\n' % sm.group(2), t)
            rest = t[m.end():]; n = re.search(r'\n(?= {0,%d}[^ \n])' % ind, rest)
            t = t[:m.start()] + rest[(n.start() + 1 if n else len(rest)):]; changed = True
    # comments that introduce a dropped public-event assertion stay harmless
    if changed: write(mf, t)
print('bridge_translations: %d inbound event(s) bridged, %d public name(s)' % (len(captures), len(public)))
