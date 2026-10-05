// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import { InvalidSyntaxJson } from './InvalidSyntaxJson';
import { isExactNumberToken, parseExactNumber } from './ExactNumber';
import { validateSyntaxInvariants } from './SyntaxInvariants';
import { syntaxMemberNames, validateSyntaxMembers } from './SyntaxMemberContracts';
import { isExactOnlyMember } from './LegacyWireProjection';
import { legacySourceOptions, validatedSourceOptions } from './SourceOptions';

export type SyntaxJsonValue = string | number | boolean | null | SyntaxJsonValue[] | { [member: string]: SyntaxJsonValue };

const isNode = (value: unknown): value is SyntaxNode =>
    typeof value === 'object' && value !== null && typeof (value as { kind?: unknown }).kind === 'string';
const sourceRoots = new Set(['ApplicationSyntax', 'ProjectionSyntax', 'CaptureSyntax', 'SpecificationSyntax']);

// The canonical JSON form of a syntax tree, the same form the C# SyntaxJson writes: 'kind' first, then the
// members in ordinal order, with source locations left out. Because a node only carries the members this
// compiler models, the result is the C# form narrowed to those members.
export function toSyntaxJson(node: SyntaxNode): SyntaxJsonValue {
    validateNumbers(node, 'legacy', 0);
    return write(node);
}

// The enriched internal tree written completely, Legacy members included. Not a wire form: it exists so the
// walker and other tree consumers can be held to every node the parser builds.
export function toCompleteSyntaxJson(node: SyntaxNode): SyntaxJsonValue {
    validateNumbers(node, 'legacy', 0);
    return write(node, 'legacy', true);
}

function write(value: unknown, owningMode = 'legacy', complete = false): SyntaxJsonValue {
    if (Array.isArray(value)) return value.map(item => write(item, owningMode, complete));
    if (isNode(value)) {
        if (sourceRoots.has(value.kind)) owningMode = validatedSourceOptions((value as unknown as { sourceOptions?: unknown }).sourceOptions ?? legacySourceOptions).numericMode;
        validateSyntaxInvariants(value);
        const result: { [member: string]: SyntaxJsonValue } = { kind: value.kind };
        const structural = { ...value } as unknown as Record<string, unknown>;
        if (value.kind === 'ApplicationSyntax' && structural.eventSources === undefined) structural.eventSources = [];
        if (value.kind === 'CommandSyntax' && structural.stream === undefined) structural.stream = null;
        if (value.kind === 'CommandSyntax' && structural.streamCandidates === undefined) structural.streamCandidates = [];
        const known = owningMode === 'exact' ? syntaxMemberNames(value.kind) : undefined;
        const members = Object.keys(structural).filter(member => (known === undefined || known.has(member)) && !(owningMode === 'legacy' && !complete && isExactOnlyMember(value.kind, member)) && member !== 'kind' && member !== 'location' && member !== 'targetLocation' && member !== 'referenceLocation' && member !== 'referenceLength' && member !== 'nameWasEscaped' && !(value.kind === 'OperationSyntax' && member === 'usesLocation')).sort(ordinal);
        for (const member of members) {
            const memberValue = structural[member];
            if (member === 'sourceOptions' && (memberValue as { numericMode?: string } | undefined)?.numericMode === 'legacy') continue;
            result[member] = write(memberValue, owningMode, complete);
        }
        return result;
    }
    if (value === undefined) return null;
    return value as SyntaxJsonValue;
}

function validateNumbers(value: unknown, owningMode: string, depth: number): void {
    if (depth > 96 && owningMode === 'exact') throw new InvalidSyntaxJson('Syntax nesting exceeds the supported depth of 96.');
    if (Array.isArray(value)) { value.forEach(item => validateNumbers(item, owningMode, depth + 1)); return; }
    if (typeof value !== 'object' || value === null) return;
    const node = value as Record<string, unknown>;
    if (sourceRoots.has(node.kind as string) || Object.hasOwn(node, 'sourceOptions')) {
        // Omission is Legacy on every complete source root, never inheritance from its container.
        const options = Object.hasOwn(node, 'sourceOptions') ? validatedSourceOptions(node.sourceOptions) : legacySourceOptions;
        if (depth > 0 && owningMode !== options.numericMode) throw new InvalidSyntaxJson('Conflicting source numeric options.');
        owningMode = options.numericMode;
    }
    if (depth === 0 && owningMode === 'exact' && !sourceRoots.has(node.kind as string)) throw new InvalidSyntaxJson('Exact syntax requires a complete source root.');
    if (owningMode === 'exact' && isNode(node)) validateSyntaxMembers(node);
    if (node.kind === 'RawExpressionSyntax' && owningMode === 'exact' && typeof node.text === 'string' && isExactNumberToken(node.text)) throw new InvalidSyntaxJson('An exact numeric operand must be an explicit ExactNumber, not opaque numeric text.');
    if (node.kind === 'LiteralExpressionSyntax' && Object.hasOwn(node, 'value')) {
        if (owningMode === 'exact' && (typeof node.value === 'number' || (node.value !== null && !['string', 'boolean', 'object'].includes(typeof node.value)))) throw new InvalidSyntaxJson('Exact source requires supported primitive values or an explicit ExactNumber literal.');
        if (typeof node.value === 'object' && node.value !== null) {
            const literal = node.value as { literalType?: unknown; value?: unknown };
            if (literal.literalType === 'ExactNumber') {
                if (owningMode !== 'exact' || typeof literal.value !== 'string' || parseExactNumber(literal.value)?.value !== literal.value || Object.keys(literal).length !== 2) throw new InvalidSyntaxJson('Malformed or incompatible ExactNumber literal.');
            } else if (owningMode === 'exact') throw new InvalidSyntaxJson('Exact source refuses old or plain object literal insertion.');
        }
    }
    const known = owningMode === 'exact' && isNode(node) ? syntaxMemberNames(node.kind) : undefined;
    for (const [name, member] of Object.entries(node)) if ((known === undefined || known.has(name) || (isNode(member) && sourceRoots.has(member.kind))) && name !== 'location' && name !== 'targetLocation' && name !== 'usesLocation') validateNumbers(member, owningMode, depth + 1);
}

function ordinal(left: string, right: string): number {
    return left < right ? -1 : left > right ? 1 : 0;
}
