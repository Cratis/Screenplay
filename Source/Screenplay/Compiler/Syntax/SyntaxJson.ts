// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import { OperationPhaseSyntax } from './Operations';
import { ProducesSyntax } from './Reactions';
import { isBlankImplementationHint } from '../Text/ImplementationHintText';

export type SyntaxJsonValue = string | number | boolean | null | SyntaxJsonValue[] | { [member: string]: SyntaxJsonValue };

const isNode = (value: unknown): value is SyntaxNode =>
    typeof value === 'object' && value !== null && typeof (value as { kind?: unknown }).kind === 'string';

// The canonical JSON form of a syntax tree, the same form the C# SyntaxJson writes: 'kind' first, then the
// members in ordinal order, with source locations left out. Because a node only carries the members this
// compiler models, the result is the C# form narrowed to those members.
export function toSyntaxJson(node: SyntaxNode): SyntaxJsonValue {
    return write(node);
}

function write(value: unknown): SyntaxJsonValue {
    if (Array.isArray(value)) {
        return value.map(write);
    }
    if (isNode(value)) {
        validateOperation(value);
        const result: { [member: string]: SyntaxJsonValue } = { kind: value.kind };
        const members = Object.keys(value).filter(member => member !== 'kind' && member !== 'location' && member !== 'targetLocation' && !(value.kind === 'OperationSyntax' && member === 'usesLocation')).sort(ordinal);
        for (const member of members) {
            result[member] = write((value as unknown as Record<string, unknown>)[member]);
        }
        return result;
    }
    if (value === undefined) {
        return null;
    }
    return value as SyntaxJsonValue;
}

function validateOperation(node: SyntaxNode): void {
    if (node.kind === 'ProducesSyntax') {
        const production = node as ProducesSyntax;
        const operation = production.inlineOperation;
        if (operation != null) {
            if (production.inlineEvent !== null) throw new Error('A production cannot declare both an event and an operation.');
            if (production.event !== operation.name || production.for !== null || production.tags.length > 0) throw new Error('An inline operation requires a matching target without event metadata.');
            if (operation.inputs.length !== production.mappings.length || operation.inputs.some((input, index) => input.name !== production.mappings[index].property)) throw new Error('Inline operation inputs and mappings must correspond in order.');
        }
    }
    if (node.kind === 'OperationPhaseSyntax') {
        const phase = node as OperationPhaseSyntax;
        if (phase.file !== null && phase.code !== null) throw new Error('An operation phase has at most one file or inline payload.');
        if (phase.implementation !== null && (!Array.isArray(phase.implementation.hints) || phase.implementation.hints.some(hint => hint == null || isBlankImplementationHint(hint.text)))) throw new Error('Implementation hints must be a collection of nonblank hints.');
    }
}

function ordinal(left: string, right: string): number {
    return left < right ? -1 : left > right ? 1 : 0;
}
