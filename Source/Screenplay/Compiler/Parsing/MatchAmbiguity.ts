// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { hasExponentialMatchAmbiguity } from './hasExponentialMatchAmbiguity';
import { MatchAmbiguityGraph } from './MatchAmbiguityGraph';
import { MatchAmbiguityNode } from './MatchAmbiguityNode';
import { MatchAmbiguityParser } from './MatchAmbiguityParser';
import { trimMatchAmbiguityGraph } from './trimMatchAmbiguityGraph';

function descendants(node: MatchAmbiguityNode): MatchAmbiguityNode[] { return [node, ...node.children.flatMap(descendants)]; }
function unwrap(node: MatchAmbiguityNode): MatchAmbiguityNode {
    while (['group', 'concat', 'alternate'].includes(node.kind) && node.children.length === 1) node = node.children[0];
    return node;
}
function rewrite(node: MatchAmbiguityNode, pattern: string): string | undefined {
    if (node.minimum > 1) return undefined;
    let body = unwrap(node.children[0]);
    let minimum = node.minimum;
    if (body.kind === 'repeat' && body.maximum === null && body.minimum <= 1) {
        minimum *= body.minimum;
        body = unwrap(body.children[0]);
    } else if (body.kind === 'alternate' && body.children.length === 2) {
        const left = unwrap(body.children[0]);
        const right = unwrap(body.children[1]);
        // Restrict this shortcut to one character token; a compound body may stay ambiguous.
        if (left.kind !== 'characters' || pattern.slice(left.offset, left.offset + left.length) !== pattern.slice(right.offset, right.offset + right.length)) return undefined;
        body = left;
    } else return undefined;
    const text = pattern.slice(body.offset, body.offset + body.length);
    return (body.kind === 'characters' ? text : `(?:${text})`) + (minimum === 0 ? '*' : '+');
}

/** Finding for an admitted pattern; analysis budget exhaustion produces no finding. */
export class MatchAmbiguity {
    constructor(readonly offset: number, readonly length: number, readonly subexpression: string, readonly suggestion: string | undefined) { }
    get description(): string {
        return `Match subexpression '${this.subexpression}' at offset ${this.offset} is exponentially ambiguous and can cause excessive browser backtracking.${this.suggestion === undefined ? '' : ` Use '${this.suggestion}' instead.`}`;
    }
    static find(pattern: string): MatchAmbiguity | undefined {
        const expression = new MatchAmbiguityParser(pattern).parse();
        const graph = new MatchAmbiguityGraph(expression);
        if (graph.exhausted) return undefined;
        const live = trimMatchAmbiguityGraph(graph);
        const candidates = descendants(expression).filter(node => node.kind === 'repeat' && node.maximum === null).sort((left, right) => left.length - right.length || left.offset - right.offset);
        for (const candidate of candidates) {
            if (hasExponentialMatchAmbiguity(graph, candidate, live)) return new MatchAmbiguity(candidate.offset, candidate.length, pattern.slice(candidate.offset, candidate.offset + candidate.length), rewrite(candidate, pattern));
            if (graph.exhausted) return undefined;
        }
        return undefined;
    }
}
