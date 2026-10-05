// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InvalidSyntaxJson } from './InvalidSyntaxJson';
import { syntaxCollections } from './SyntaxCollections';
import { SyntaxNode } from './SyntaxNode';

type Member = readonly [name: number, rule: number, required: boolean];
type Rule = readonly [type: string, ...arguments: unknown[]];
const rules: readonly Rule[] = syntaxCollections.rules;
const patterns = new Map<number, RegExp>(rules.flatMap(([type, expression], index) => type === 'string' && typeof expression === 'string' ? [[index, new RegExp(expression, 'u')] as const] : []));
const contracts = new Map<string, readonly Member[]>(syntaxCollections.kinds.map((kind, index) => [kind, rules[syntaxCollections.contracts[index]][1] as readonly Member[]]));
const names = new Map<string, ReadonlySet<string>>([...contracts].map(([kind, members]) => [kind, new Set(['kind', ...members.map(([name]) => syntaxCollections.names[name])])]));
const object = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value);

// One indexed native-owned contract is shared by restoration and Exact typed writing. Defaults may
// be omitted, but present values cannot change their primitive width, enum, or node base kind.
export function validateSyntaxMembers(node: SyntaxNode): void {
    const members = contracts.get(node.kind);
    if (!Object.hasOwn(node, 'kind') || members === undefined || !matchesMembers(node as unknown as Record<string, unknown>, members)) throw new InvalidSyntaxJson(`Invalid typed members for syntax kind '${node.kind}'.`);
}

// Closed-world writing is opt-in for feature-owned shapes, not a change to older Legacy nodes.
// Source-only members are the writer's existing metadata omissions, never additional wire members.
export function validateClosedSyntaxMembers(node: SyntaxNode, sourceOnlyMembers: ReadonlySet<string>): void {
    const known = syntaxMemberNames(node.kind);
    for (const member of Object.keys(node)) {
        if (!known.has(member) && !sourceOnlyMembers.has(member)) throw new InvalidSyntaxJson(`Unknown member '${member}' for syntax kind '${node.kind}'.`);
    }
}

export function syntaxMemberNames(kind: string): ReadonlySet<string> {
    const members = names.get(kind);
    if (members === undefined) throw new InvalidSyntaxJson(`Unknown syntax kind '${kind}'.`);
    return members;
}

function matchesMembers(value: Record<string, unknown>, members: readonly Member[]): boolean {
    return members.every(([name, rule, required]) => Object.hasOwn(value, syntaxCollections.names[name]) ? matches(value[syntaxCollections.names[name]], rule) : !required);
}

// Array.every skips holes, so each index is inspected: a hole is never a valid element.
function allIndicesMatch(items: readonly unknown[], rule: number): boolean {
    for (let position = 0; position < items.length; position++) {
        if (!Object.hasOwn(items, position) || !matches(items[position], rule)) return false;
    }
    return true;
}

function matches(value: unknown, index: number): boolean {
    const [type, ...args] = rules[index];
    switch (type) {
        case 'ref': return object(value) && typeof value.kind === 'string' && args.some(kind => syntaxCollections.kinds[kind as number] === value.kind);
        case 'union':
        case 'one': {
            const count = args.reduce<number>((count, rule) => count + (matches(value, rule as number) ? 1 : 0), 0);
            return type === 'one' ? count === 1 : count > 0;
        }
        case 'const': return value === args[0];
        case 'enum': return args.includes(value);
        case 'null': return value === null;
        case 'array': return Array.isArray(value) && allIndicesMatch(value, args[0] as number);
        case 'object': return object(value) && Object.keys(value).every(name => (args[0] as readonly Member[]).some(([key]) => syntaxCollections.names[key] === name)) && matchesMembers(value, args[0] as readonly Member[]);
        case 'integer':
        case 'number': return typeof value === 'number' && Number.isFinite(value) && (type !== 'integer' || Number.isInteger(value)) && (args.length === 0 || (value >= (args[0] as number) && value <= (args[1] as number) && !(type === 'integer' && args[0] === 0 && Object.is(value, -0))));
        case 'string': return typeof value === 'string' && (args.length === 0 || patterns.get(index)!.test(value));
        default: return type === 'boolean' && typeof value === 'boolean';
    }
}
