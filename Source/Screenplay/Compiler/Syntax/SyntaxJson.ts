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
        return Array.isArray(value) ? detachArray(value) : detachObject(value);
    }
    function detachArray(value: unknown[]): unknown {
        const items = dataArray<unknown>();
        copies.set(value, items);
        const length = value.length;
        for (let index = length - 1; index >= 0; index--) pending.push(() => {
            items[index] = detach(Object.hasOwn(value, index) ? value[index] : undefined);
        });
        return items;
    }
    function detachObject(value: object): unknown {
        const copy = Object.create(null) as Record<string, unknown>;
        copies.set(value, copy);
        const names = Object.keys(value);
        // Node identity also accepts inherited, hidden and class-getter kinds; read it just once.
        if (!names.includes('kind') && 'kind' in value) names.unshift('kind');
        for (let index = names.length - 1; index >= 0; index--) pending.push(() => {
            const name = names[index];
            const member = (value as Record<string, unknown>)[name];
            if (name === 'sourceOptions') pending.push(() => preserveRejectedSourceOptions(copy, name, member));
            copy[name] = detach(member);
        });
        return copy;
    }
    const detached = detach(value);
    while (pending.length > 0) pending.pop()!();
    return detached;
}

// Source option contracts also reject exotic prototypes and hidden/symbol members. Preserve
// that rejected shape as data rather than sanitizing it into an apparently valid option record.
function preserveRejectedSourceOptions(copy: Record<string, unknown>, name: string, member: unknown): void {
    if (typeof member === 'object' && member !== null && hasExoticShape(member)) {
        copy[name] = Object.assign(Object.create(null), { numericMode: 'invalid' });
    }
}

function hasExoticShape(member: object): boolean {
    return (Object.getPrototypeOf(member) !== Object.prototype && Object.getPrototypeOf(member) !== null) || Reflect.ownKeys(member).length !== Object.keys(member).length;
}

function dataArray<T>(): T[] {
    const items: T[] = [];
    // JSON.stringify consults inherited toJSON even on plain arrays. Shadow that hook without changing
    // their indexed JSON bytes or normal Array prototype (including for existing tree consumers).
    Object.defineProperty(items, 'toJSON', { value: undefined });
    return items;
}

// Members omitted from the wire form when null (or undefined), by node kind.
const omittedWhenNull: Record<string, readonly string[]> = {
    ApplicationSyntax: ['identity'],
    EventSyntax: ['origin'],
    ImportSyntax: ['origin'],
    SliceSyntax: ['direction'],
    ReactionSyntax: ['from'],
    ReducerSyntax: ['from'],
    ProducesSyntax: ['stream'],
    ConceptAttributeSyntax: ['scope', 'specialCategory'],
    ConceptSyntax: ['description', 'valueNumbers'],
    PolicySyntax: ['description'],
    FileConstraintSyntax: ['description'],
    UniqueEventConstraintSyntax: ['description'],
    UniquePropertyConstraintSyntax: ['description'],
    ProjectionSyntax: ['description'],
    ScreenSyntax: ['description'],
    FormSyntax: ['description'],
    SpecificationSyntax: ['description', 'whenRedelivered'],
    SpecificationErrorSyntax: ['caseValue'],
    SpecificationExampleSyntax: ['stream', 'noStream'],
    SpecificationRedeliverySyntax: ['stream', 'noStream']
};

// Members omitted from the wire form when empty arrays, by node kind.
const omittedWhenEmpty: Record<string, readonly string[]> = {
    ModuleSyntax: ['dependsOn'],
    FeatureSyntax: ['dependsOn'],
    SpecificationSyntax: ['parameters', 'cases'],
    InvokesSyntax: ['onRefused']
};

// Members omitted when they hold their default value, by node kind.
const omittedWhenDefault: Record<string, Record<string, unknown>> = {
    EventSyntax: { visibility: 'Private' },
    ImportSyntax: { visibility: 'Private' },
    ConceptAttributeSyntax: { criminal: false },
    PropertySyntax: { isSubject: false },
    SpecificationSyntax: { thenNoEvents: false }
};

const ownEntry = <T>(table: Record<string, T>, kind: string): T | undefined => Object.hasOwn(table, kind) ? table[kind] : undefined;
const isEmptyArray = (value: unknown): boolean => Array.isArray(value) && value.length === 0;

function isNullOmitted(kind: string, member: string, value: unknown): boolean {
    if (value != null) return false;
    return ownEntry(omittedWhenNull, kind)?.includes(member) === true
        || member === 'inlineProperty'
        || (member === 'documentation' && kind !== 'EventSyntax');
}

function isEmptyOmitted(kind: string, member: string, value: unknown): boolean {
    if (!isEmptyArray(value)) return false;
    return ownEntry(omittedWhenEmpty, kind)?.includes(member) === true || member === 'examples' || member === 'purposes';
}

function isDefaultOmitted(kind: string, member: string, value: unknown): boolean {
    if (member === 'sourceOptions') return (value as { numericMode?: string } | undefined)?.numericMode === 'legacy';
    const defaults = ownEntry(omittedWhenDefault, kind);
    return defaults !== undefined && Object.hasOwn(defaults, member) && value === defaults[member];
}

function isOmittedByDefault(kind: string, member: string, value: unknown): boolean {
    return isNullOmitted(kind, member, value) || isEmptyOmitted(kind, member, value) || isDefaultOmitted(kind, member, value);
}

function write(value: unknown, owningMode = 'legacy', complete = false): SyntaxJsonValue {
    if (Array.isArray(value)) return writeArray(value, owningMode, complete);
    if (isNode(value)) return writeNode(value, owningMode, complete);
    if (value === undefined) return null;
    if (typeof value !== 'object' || value === null) return value as SyntaxJsonValue;
    return writeRecord(value as Record<string, unknown>, owningMode, complete);
}

function writeArray(value: unknown[], owningMode: string, complete: boolean): SyntaxJsonValue {
    // A fresh plain array: never map (Symbol.species) or serialize through a caller-supplied hook.
    const items = dataArray<SyntaxJsonValue>();
    for (let index = 0; index < value.length; index++) items.push(write(value[index], owningMode, complete));
    return items;
}

function validateClosedImplementationShapes(value: SyntaxNode): void {
    // #307 shapes are closed in both modes, before any projection can hide unknown members.
    if (value.kind === 'ImplementationSyntax' || value.kind === 'ImplementationHintSyntax') validateClosedSyntaxMembers(value, omitted);
    if (value.kind !== 'ValidationRuleSyntax') return;
    const rule = value as ValidationRuleSyntax;
    if (rule.implementation == null) return;
    if (rule.file != null) validateClosedSyntaxMembers(rule.file, omitted);
    if (rule.code != null) validateClosedSyntaxMembers(rule.code, omitted);
}

function structuralMembers(value: SyntaxNode): Record<string, unknown> {
    const structural = { ...value } as unknown as Record<string, unknown>;
    if (value.kind === 'ApplicationSyntax' && structural.eventSources === undefined) structural.eventSources = [];
    if (value.kind === 'CommandSyntax' && structural.stream === undefined) structural.stream = null;
    if (value.kind === 'CommandSyntax' && structural.streamCandidates === undefined) structural.streamCandidates = [];
    return structural;
}

function projectedMemberNames(value: SyntaxNode, structural: Record<string, unknown>, owningMode: string, complete: boolean): string[] {
    const known = owningMode === 'exact' ? syntaxMemberNames(value.kind) : undefined;
    const hiddenByLegacy = (member: string) => owningMode === 'legacy' && !complete && isLegacyOmittedMember(value, member);
    const isProjected = (member: string) =>
        (known === undefined || known.has(member)) && !hiddenByLegacy(member) && !omitted.has(member) &&
        !(value.kind === 'OperationSyntax' && member === 'usesLocation');
    return Object.keys(structural).filter(isProjected).sort(ordinal);
}

function writeNode(value: SyntaxNode, owningMode: string, complete: boolean): SyntaxJsonValue {
    if (sourceRoots.has(value.kind)) owningMode = validatedSourceOptions((value as unknown as { sourceOptions?: unknown }).sourceOptions ?? legacySourceOptions).numericMode;
    validateSyntaxInvariants(value);
    validateClosedImplementationShapes(value);
    const result = Object.create(null) as Record<string, SyntaxJsonValue>;
    result.kind = value.kind;
    const structural = structuralMembers(value);
    for (const member of projectedMemberNames(value, structural, owningMode, complete)) {
        const memberValue = structural[member];
        if (isOmittedByDefault(value.kind, member, memberValue)) continue;
        result[member] = write(memberValue, owningMode, complete);
    }
    return result;
}

function writeRecord(record: Record<string, unknown>, owningMode: string, complete: boolean): SyntaxJsonValue {
    // The ordered ExactNumber form, whatever the input key order; other objects are copied without any toJSON hook.
    const copy = Object.create(null) as Record<string, SyntaxJsonValue>;
    if (record.literalType === 'ExactNumber') {
        copy.literalType = 'ExactNumber';
        copy.value = record.value as string;
        return copy;
    }
    for (const name of Object.keys(record)) if (name !== 'toJSON' && record[name] !== undefined) copy[name] = write(record[name], owningMode, complete);
    return copy;
}

function validateNumbers(value: unknown, owningMode: string, depth: number): void {
    assertSupportedDepth(owningMode, depth);
    if (Array.isArray(value)) {
        for (let index = 0; index < value.length; index++) validateNumbers(value[index], owningMode, depth + 1);
        return;
    }
    if (typeof value !== 'object' || value === null) return;
    const node = value as Record<string, unknown>;
    owningMode = resolveNumericMode(node, owningMode, depth);
    assertCompleteSourceRoot(node, owningMode, depth);
    if (owningMode === 'exact' && isNode(node)) validateSyntaxMembers(node);
    validateRawNumericText(node, owningMode);
    if (node.kind === 'LiteralExpressionSyntax' && Object.hasOwn(node, 'value')) validateLiteralValue(node.value, owningMode);
    validateNumberMembers(node, owningMode, depth);
}

function assertSupportedDepth(owningMode: string, depth: number): void {
    if (depth > 96 && owningMode === 'exact') throw new InvalidSyntaxJson('Syntax nesting exceeds the supported depth of 96.');
}

function assertCompleteSourceRoot(node: Record<string, unknown>, owningMode: string, depth: number): void {
    if (depth === 0 && owningMode === 'exact' && !sourceRoots.has(node.kind as string)) throw new InvalidSyntaxJson('Exact syntax requires a complete source root.');
}

function resolveNumericMode(node: Record<string, unknown>, owningMode: string, depth: number): string {
    if (!sourceRoots.has(node.kind as string) && !Object.hasOwn(node, 'sourceOptions')) return owningMode;
    // Omission is Legacy on every complete source root, never inheritance from its container.
    const options = Object.hasOwn(node, 'sourceOptions') ? validatedSourceOptions(node.sourceOptions) : legacySourceOptions;
    if (depth > 0 && owningMode !== options.numericMode) throw new InvalidSyntaxJson('Conflicting source numeric options.');
    return options.numericMode;
}

function validateRawNumericText(node: Record<string, unknown>, owningMode: string): void {
    if (node.kind !== 'RawExpressionSyntax' || owningMode !== 'exact') return;
    if (typeof node.text === 'string' && isExactNumberToken(node.text)) throw new InvalidSyntaxJson('An exact numeric operand must be an explicit ExactNumber, not opaque numeric text.');
}

function isUnsupportedExactPrimitive(value: unknown): boolean {
    return typeof value === 'number' || (value !== null && !['string', 'boolean', 'object'].includes(typeof value));
}

function validateLiteralValue(value: unknown, owningMode: string): void {
    if (owningMode === 'exact' && isUnsupportedExactPrimitive(value)) throw new InvalidSyntaxJson('Exact source requires supported primitive values or an explicit ExactNumber literal.');
    if (typeof value !== 'object' || value === null) return;
    const literal = value as { literalType?: unknown; value?: unknown };
    if (literal.literalType === 'ExactNumber') validateExactNumberLiteral(literal, owningMode);
    else if (owningMode === 'exact') throw new InvalidSyntaxJson('Exact source refuses old or plain object literal insertion.');
}

function isWellFormedExactNumber(literal: { value?: unknown }): boolean {
    return typeof literal.value === 'string' && parseExactNumber(literal.value)?.value === literal.value && Object.keys(literal).length === 2;
}

function validateExactNumberLiteral(literal: { value?: unknown }, owningMode: string): void {
    if (owningMode !== 'exact' || !isWellFormedExactNumber(literal)) throw new InvalidSyntaxJson('Malformed or incompatible ExactNumber literal.');
}

function validateNumberMembers(node: Record<string, unknown>, owningMode: string, depth: number): void {
    const known = owningMode === 'exact' && isNode(node) ? syntaxMemberNames(node.kind) : undefined;
    const skipped = new Set(['location', 'targetLocation', 'usesLocation']);
    const isTraversed = (name: string, member: unknown) =>
        (known === undefined || known.has(name) || (isNode(member) && sourceRoots.has(member.kind))) && !skipped.has(name);
    for (const [name, member] of Object.entries(node)) if (isTraversed(name, member)) validateNumbers(member, owningMode, depth + 1);
}

function ordinal(left: string, right: string): number {
    return left < right ? -1 : left > right ? 1 : 0;
}
