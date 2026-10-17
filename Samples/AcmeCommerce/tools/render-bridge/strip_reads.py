#!/usr/bin/env python3
"""Render variant step: remove what Stage cannot render yet from the Acme Commerce source (cratis 3.75.0: STAGE-ESM-043/044).

 * application preamble `reaction batches atomic` (ESM v22, Stage#422) and `command reads protected` (ESM v25, Stage#503)
 * command `reads <ReadModel> [as alias] by <key>` and the `require` rules over read paths in its `validate` block
 * `target = Read.prop` produced-event mappings become command inputs (`prop Type`, typed from the read model)
 * the specifications that exercised a removed guard, or that drive a command which gained inputs
usage: strip_reads.py <variant-folder>
"""
import re, sys, glob, os

root = sys.argv[1]
files = glob.glob(root + '/**/*.play', recursive=True)
text = {f: open(f).read() for f in files}

# 1. preamble
for f in files:
    t = text[f]
    n = re.sub(r'^reaction batches atomic\n', '', t, flags=re.M)
    n = re.sub(r'^command reads protected\n', '', n, flags=re.M)
    text[f] = n

# 2. read model property types
models = {}
for f in files:
    for m in re.finditer(r'^( *)readmodel (\w+)\n((?:\1  .*\n|\n)+)', text[f], flags=re.M):
        props = {}
        for line in m.group(3).split('\n'):
            p = re.match(r'^\s+(\w+)\s+([\w\[\]]+)(?:\s+\w+)*\s*$', line)
            if p and len(line) - len(line.lstrip()) == len(m.group(1)) + 2:
                props[p.group(1)] = p.group(2)
        models[m.group(2)] = props

def indent(l): return len(l) - len(l.lstrip(' '))

dropped = []
for f in files:
    lines = text[f].split('\n')
    out = []; i = 0
    while i < len(lines):
        line = lines[i]
        m = re.match(r'^( *)command (\w+)\s*$', line)
        if not m:
            out.append(line); i += 1; continue
        # collect the command block
        base = len(m.group(1)); j = i + 1
        while j < len(lines) and (lines[j].strip() == '' or indent(lines[j]) > base): j += 1
        block = lines[i:j]
        reads = {}  # alias-or-model -> model
        keep = []
        k = 0
        removed_messages = []
        gained = []
        while k < len(block):
            l = block[k]
            r = re.match(r'^ *reads (\w+)(?: as (\w+))?(?: by \w+)?\s*$', l)
            if r:
                reads[r.group(2) or r.group(1)] = r.group(1)
                if r.group(2) is None: reads[r.group(1)] = r.group(1)
                k += 1
                # a multi-line `by` header
                while k < len(block) and re.match(r'^ +by\s*$', block[k]):
                    ind = indent(block[k]); k += 1
                    while k < len(block) and indent(block[k]) > ind: k += 1
                continue
            keep.append(l); k += 1
        if reads:
            body = '\n'.join(keep)
            names = '|'.join(re.escape(a) for a in reads)
            # require rules over read paths (with their message/severity children)
            out_b = []; k = 0; ls = body.split('\n')
            while k < len(ls):
                l = ls[k]
                if re.match(r'^ +require .*\b(%s)\.\w+' % names, l):
                    ind = indent(l); k += 1
                    while k < len(ls) and ls[k].strip() != '' and indent(ls[k]) > ind:
                        mm = re.match(r'^ +message "(.*)"', ls[k])
                        if mm: removed_messages.append(mm.group(1))
                        k += 1
                    continue
                out_b.append(l); k += 1
            body = '\n'.join(out_b)
            ls = body.split('\n'); out_b = []
            for idx, l in enumerate(ls):
                if re.match(r'^ +validate\s*$', l):
                    nxt = next((x for x in ls[idx + 1:] if x.strip() != ''), '')
                    if indent(nxt) <= indent(l): continue
                out_b.append(l)
            body = '\n'.join(out_b)
            # produced-event mappings from read paths
            def sub_map(mm):
                pad, target, space, alias, prop = mm.groups()
                model = reads.get(alias)
                typ = models.get(model, {}).get(prop)
                if typ is None: return mm.group(0)
                gained.append((prop, typ))
                return '%s%s%s= %s' % (pad, target, space, prop)
            body = re.sub(r'^( +)(\w+)( *)= (%s)\.(\w+)\s*$' % names, sub_map, body, flags=re.M)
            if gained:
                ls = body.split('\n')
                for idx, l in enumerate(ls):
                    if re.match(r'^ +\w+\s+\w+\s+identifier\s*$', l):
                        pad = re.match(r'^( +)', l).group(1)
                        seen = set()
                        add = []
                        for p, t in gained:
                            if p in seen: continue
                            seen.add(p); add.append('%s%s %s' % (pad, p, t))
                        ls[idx + 1:idx + 1] = add
                        break
                body = '\n'.join(ls)
            block = body.split('\n')
            dropped.append((f, m.group(2), removed_messages, bool(gained)))
        out.extend(block); i = j
    text[f] = '\n'.join(out)

# 3. specifications: drop the ones that exercised a removed guard, or drive a command that gained inputs
def drop_spec(t, name):
    m = re.search(r'\n( *)specification %s\n' % re.escape(name), t)
    if not m: return t
    ind = len(m.group(1)); rest = t[m.end():]
    n = re.search(r'\n(?= {0,%d}[^ \n])' % ind, rest)
    return t[:m.start() + 1] + rest[(n.start() + 1 if n else len(rest)):]

for f, cmd, messages, gained in dropped:
    t = text[f]
    for sm in list(re.finditer(r'(?m)^( *)specification (\w+)\n((?:\1 .*\n|\n)*)', t)):
        body = sm.group(3)
        when_cmd = re.search(r'^\s+when (\w+)', body, flags=re.M)
        examples = dict(re.findall(r'^\s+example (\w+) : (\w+)', t, flags=re.M))
        target = when_cmd.group(1) if when_cmd else None
        target = examples.get(target, target)
        if target != cmd: continue
        guard = any(('error "%s"' % msg) in body for msg in messages)
        if guard or gained:
            t = drop_spec(t, sm.group(2))
    text[f] = t

for f in files:
    open(f, 'w').write(text[f])
print('strip_reads: %d command(s) rewritten' % len(dropped))
