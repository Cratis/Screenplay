// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Deterministic Tarjan decomposition, shared by dependency queries and timeline diagnostics.
export function stronglyConnectedGroups(graph: ReadonlyMap<string, ReadonlySet<string>>): string[][] {
    const indices = new Map<string, number>();
    const low = new Map<string, number>();
    const stack: string[] = [];
    const active = new Set<string>();
    const groups: string[][] = [];
    const visit = (node: string) => {
        indices.set(node, indices.size);
        low.set(node, indices.get(node)!);
        stack.push(node);
        active.add(node);
        for (const next of [...graph.get(node)!].sort()) {
            if (!indices.has(next)) {
                visit(next);
                low.set(node, Math.min(low.get(node)!, low.get(next)!));
            } else if (active.has(next)) low.set(node, Math.min(low.get(node)!, indices.get(next)!));
        }
        if (low.get(node) !== indices.get(node)) return;
        const group: string[] = [];
        let member: string;
        do {
            member = stack.pop()!;
            active.delete(member);
            group.push(member);
        } while (member !== node);
        groups.push(group.sort());
    };
    for (const node of [...graph.keys()].sort()) if (!indices.has(node)) visit(node);
    return groups;
}
