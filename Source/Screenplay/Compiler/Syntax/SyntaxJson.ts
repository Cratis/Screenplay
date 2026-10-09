// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import { InvalidSyntaxJson } from './InvalidSyntaxJson';
import { isExactNumberToken, parseExactNumber } from './ExactNumber';
import { validateSyntaxInvariants } from './SyntaxInvariants';
import { syntaxMemberNames, validateClosedSyntaxMembers, validateSyntaxMembers } from './SyntaxMemberContracts';
import { ValidationRuleSyntax } from './Commands';
import { isLegacyOmittedMember } from './LegacyWireProjection';
import { legacySourceOptions, validatedSourceOptions } from './SourceOptions';

// Returned record objects have null prototypes.
export type SyntaxJsonValue = string | number | boolean | null | SyntaxJsonValue[] | { [member: string]: SyntaxJsonValue };

const isNode = (value: unknown): value is SyntaxNode =>
    typeof value === 'object' && value !== null && typeof (value as { kind?: unknown }).kind === 'string';
const omitted = new Set(['kind', 'location', 'targetLocation', 'referenceLocation', 'referenceLength', 'nameWasEscaped', 'declaredTriggers', 'directiveLocations']);
const sourceRoots = new Set(['ApplicationSyntax', 'ProjectionSyntax', 'CaptureSyntax', 'SpecificationSyntax']);

// The canonical JSON form of a syntax tree, the same form the C# SyntaxJson writes: 'kind' first, then the
// members in ordinal order, with source locations left out. Because a node only carries the members this
// compiler models, the result is the C# form narrowed to those members.
export function toSyntaxJson(node: SyntaxNode): SyntaxJsonValue {
    const detached = snapshot(node);
    validateNumbers(detached, 'legacy', 0);
    return write(detached);
}

// The enriched internal tree written completely, Legacy members included. Not a wire form: it exists so the
// walker and other tree consumers can be held to every node the parser builds.
export function toCompleteSyntaxJson(node: SyntaxNode): SyntaxJsonValue {
    const detached = snapshot(node);
    validateNumbers(detached, 'legacy', 0);
    return write(detached, 'legacy', true);
}

// Read caller-owned properties only once, before validation. Both validation and projection see the same
// detached data; accessors, proxies and shared references cannot change it between passes.
function snapshot(value: unknown): unknown {
    const copies = new WeakMap<object, unknown>();
    const pending: (() => void)[] = [];
    // Schedule depth-first reads without consuming the call stack, including for ignored metadata.
    function detach(value: unknown): unknown {
        if (typeof value !== 'object' || value === null) return value;
        if (copies.has(value)) return copies.get(value);
        if (Array.isArray(value)) {
            const items = dataArray<unknown>();
            copies.set(value, items);
            const length = value.length;
            for (let index = length - 1; index >= 0; index--) pending.push(() => {
                items[index] = detach(Object.hasOwn(value, index) ? value[index] : undefined);
            });
            return items;
        }
        const copy = Object.create(null) as Record<string, unknown>;
        copies.set(value, copy);
        const names = Object.keys(value);
        // Node identity also accepts inherited, hidden and class-getter kinds; read it just once.
        if (!names.includes('kind') && 'kind' in value) names.unshift('kind');
        for (let index = names.length - 1; index >= 0; index--) pending.push(() => {
            const name = names[index];
            const member = (value as Record<string, unknown>)[name];
            if (name === 'sourceOptions') pending.push(() => {
                // Source option contracts also reject exotic prototypes and hidden/symbol members. Preserve
                // that rejected shape as data rather than sanitizing it into an apparently valid option record.
                if (typeof member === 'object' && member !== null &&
                    ((Object.getPrototypeOf(member) !== Object.prototype && Object.getPrototypeOf(member) !== null) || Reflect.ownKeys(member).length !== Object.keys(member).length)) {
                    copy[name] = Object.assign(Object.create(null), { numericMode: 'invalid' });
                }
            });
            copy[name] = detach(member);
        });
        return copy;
    }
    const detached = detach(value);
    while (pending.length > 0) pending.pop()!();
    return detached;
}

function dataArray<T>(): T[] {
    const items: T[] = [];
    // JSON.stringify consults inherited toJSON even on plain arrays. Shadow that hook without changing
    // their indexed JSON bytes or normal Array prototype (including for existing tree consumers).
    Object.defineProperty(items, 'toJSON', { value: undefined });
    return items;
}

function write(value: unknown, owningMode = 'legacy', complete = false): SyntaxJsonValue {
    if (Array.isArray(value)) {
        // A fresh plain array: never map (Symbol.species) or serialize through a caller-supplied hook.
        const items = dataArray<SyntaxJsonValue>();
        for (let index = 0; index < value.length; index++) items.push(write(value[index], owningMode, complete));
        return items;
    }
    if (isNode(value)) {
        if (sourceRoots.has(value.kind)) owningMode = validatedSourceOptions((value as unknown as { sourceOptions?: unknown }).sourceOptions ?? legacySourceOptions).numericMode;
        validateSyntaxInvariants(value);
        // #307 shapes are closed in both modes, before any projection can hide unknown members.
        if (value.kind === 'ImplementationSyntax' || value.kind === 'ImplementationHintSyntax') validateClosedSyntaxMembers(value, omitted);
        if (value.kind === 'ValidationRuleSyntax') {
            const rule = value as ValidationRuleSyntax;
            if (rule.implementation != null) {
                if (rule.file != null) validateClosedSyntaxMembers(rule.file, omitted);
                if (rule.code != null) validateClosedSyntaxMembers(rule.code, omitted);
            }
        }
        const result = Object.create(null) as { [member: string]: SyntaxJsonValue };
        result.kind = value.kind;
        const structural = { ...value } as unknown as Record<string, unknown>;
        if (value.kind === 'ApplicationSyntax' && structural.eventSources === undefined) structural.eventSources = [];
        if (value.kind === 'CommandSyntax' && structural.stream === undefined) structural.stream = null;
        if (value.kind === 'CommandSyntax' && structural.streamCandidates === undefined) structural.streamCandidates = [];
        const known = owningMode === 'exact' ? syntaxMemberNames(value.kind) : undefined;
        const members = Object.keys(structural).filter(member => (known === undefined || known.has(member)) && !(owningMode === 'legacy' && !complete && isLegacyOmittedMember(value, member)) && !omitted.has(member) && !(value.kind === 'OperationSyntax' && member === 'usesLocation')).sort(ordinal);
        for (const member of members) {
            const memberValue = structural[member];
            if (member === 'documentation' && value.kind !== 'EventSyntax' && memberValue == null) continue;
            if (value.kind === 'SpecificationSyntax' && member === 'description' && memberValue == null) continue;
            if (member === 'dependsOn' && (value.kind === 'ModuleSyntax' || value.kind === 'FeatureSyntax') && Array.isArray(memberValue) && memberValue.length === 0) continue;
            if (member === 'sourceOptions' && (memberValue as { numericMode?: string } | undefined)?.numericMode === 'legacy') continue;
            if ((member === 'examples' || value.kind === 'SpecificationSyntax' && (member === 'parameters' || member === 'cases')) && Array.isArray(memberValue) && memberValue.length === 0) continue;
            if (value.kind === 'SpecificationErrorSyntax' && member === 'caseValue' && memberValue == null) continue;
            if (member === 'inlineProperty' && memberValue == null) continue;
            if ((value.kind === 'SpecificationExampleSyntax' || value.kind === 'SpecificationRedeliverySyntax') && (member === 'stream' || member === 'noStream') && memberValue == null) continue;
            if (value.kind === 'SpecificationSyntax' && member === 'thenNoEvents' && memberValue === false) continue;
            if (value.kind === 'SpecificationSyntax' && member === 'whenRedelivered' && memberValue == null) continue;
            if (value.kind === 'InvokesSyntax' && member === 'onRefused' && Array.isArray(memberValue) && memberValue.length === 0) continue;
            result[member] = write(memberValue, owningMode, complete);
        }
        return result;
    }
    if (value === undefined) return null;
    if (typeof value !== 'object' || value === null) return value as SyntaxJsonValue;
    const record = value as Record<string, unknown>;
    // The ordered ExactNumber form, whatever the input key order; other objects are copied without any toJSON hook.
    const copy = Object.create(null) as { [member: string]: SyntaxJsonValue };
    if (record.literalType === 'ExactNumber') {
        copy.literalType = 'ExactNumber';
        copy.value = record.value as string;
        return copy;
    }
    for (const name of Object.keys(record)) if (name !== 'toJSON' && record[name] !== undefined) copy[name] = write(record[name], owningMode, complete);
    return copy;
}

function validateNumbers(value: unknown, owningMode: string, depth: number): void {
    if (depth > 96 && owningMode === 'exact') throw new InvalidSyntaxJson('Syntax nesting exceeds the supported depth of 96.');
    if (Array.isArray(value)) { for (let index = 0; index < value.length; index++) validateNumbers(value[index], owningMode, depth + 1); return; }
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
