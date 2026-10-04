// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import { InvalidSyntaxJson } from './InvalidSyntaxJson';
import { isExactNumberToken, parseExactNumber } from './ExactNumber';
import { CommandSyntax } from './Commands';
import { OperationPhaseSyntax } from './Operations';
import { CommandStreamSyntax, EventSourceSyntax, EventStreamSyntax } from './EventSources';
import { ProducesSyntax } from './Reactions';
import { isBlankImplementationHint } from '../Text/ImplementationHintText';
import { isSourceStreamName, isSourceStreamTypeName } from '../Text/SourceStreamNames';

export type SyntaxJsonValue = string | number | boolean | null | SyntaxJsonValue[] | { [member: string]: SyntaxJsonValue };

const isNode = (value: unknown): value is SyntaxNode =>
    typeof value === 'object' && value !== null && typeof (value as { kind?: unknown }).kind === 'string';

// The canonical JSON form of a syntax tree, the same form the C# SyntaxJson writes: 'kind' first, then the
// members in ordinal order, with source locations left out. Because a node only carries the members this
// compiler models, the result is the C# form narrowed to those members.
export function toSyntaxJson(node: SyntaxNode): SyntaxJsonValue {
    validateNumbers(node, 'legacy', 0);
    return write(node);
}

function write(value: unknown): SyntaxJsonValue {
    if (Array.isArray(value)) {
        return value.map(write);
    }
    if (isNode(value)) {
        validateOperation(value);
        validateSourceStream(value);
        const result: { [member: string]: SyntaxJsonValue } = { kind: value.kind };
        const structural = { ...value } as unknown as Record<string, unknown>;
        if (value.kind === 'ApplicationSyntax' && structural.eventSources === undefined) structural.eventSources = [];
        if (value.kind === 'CommandSyntax' && structural.stream === undefined) structural.stream = null;
        if (value.kind === 'CommandSyntax' && structural.streamCandidates === undefined) structural.streamCandidates = [];
        const members = Object.keys(structural).filter(member => member !== 'kind' && member !== 'location' && member !== 'targetLocation' && member !== 'referenceLocation' && member !== 'referenceLength' && member !== 'nameWasEscaped' && !(value.kind === 'OperationSyntax' && member === 'usesLocation')).sort(ordinal);
        for (const member of members) {
            const memberValue = structural[member];
            if (member === 'sourceOptions' && (memberValue as { numericMode?: string } | undefined)?.numericMode === 'legacy') continue;
            result[member] = write(memberValue);
        }
        return result;
    }
    if (value === undefined) {
        return null;
    }
    return value as SyntaxJsonValue;
}

function validateNumbers(value: unknown, owningMode: string, depth: number): void {
    if (depth > 96 && owningMode === 'exact') throw new InvalidSyntaxJson('Syntax nesting exceeds the supported depth of 96.');
    if (Array.isArray(value)) { value.forEach(item => validateNumbers(item, owningMode, depth + 1)); return; }
    if (typeof value !== 'object' || value === null) return;
    const node = value as Record<string, unknown>;
    if (Object.hasOwn(node, 'sourceOptions')) {
        const options = node.sourceOptions as Record<string, unknown> | null;
        if (typeof options !== 'object' || options === null || Object.keys(options).length !== 1 || (options.numericMode !== 'legacy' && options.numericMode !== 'exact')) throw new InvalidSyntaxJson('Malformed source numeric options.');
        if (depth > 0 && owningMode !== options.numericMode) throw new InvalidSyntaxJson('Conflicting source numeric options.');
        owningMode = options.numericMode;
    }
    if (node.kind === 'RawExpressionSyntax' && owningMode === 'exact' && typeof node.text === 'string' && isExactNumberToken(node.text)) throw new InvalidSyntaxJson('An exact numeric operand must be an explicit ExactNumber, not opaque numeric text.');
    if (node.kind === 'LiteralExpressionSyntax') {
        if (owningMode === 'exact' && (typeof node.value === 'number' || (node.value !== null && !['string', 'boolean', 'object'].includes(typeof node.value)))) throw new InvalidSyntaxJson('Exact source requires supported primitive values or an explicit ExactNumber literal.');
        if (typeof node.value === 'object' && node.value !== null) {
            const literal = node.value as { literalType?: unknown; value?: unknown };
            if (literal.literalType === 'ExactNumber') {
                if (owningMode !== 'exact' || typeof literal.value !== 'string' || parseExactNumber(literal.value)?.value !== literal.value || Object.keys(literal).length !== 2) throw new InvalidSyntaxJson('Malformed or incompatible ExactNumber literal.');
            } else if (owningMode === 'exact') throw new InvalidSyntaxJson('Exact source refuses old or plain object literal insertion.');
        }
    }
    for (const [name, member] of Object.entries(node)) if (name !== 'location' && name !== 'targetLocation' && name !== 'usesLocation') validateNumbers(member, owningMode, depth + 1);
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

function validateSourceStream(node: SyntaxNode): void {
    const name = (value: string) => { if (!isSourceStreamName(value)) throw new Error('Event source and stream names must be identifiers.'); };
    const hasKind = (value: unknown, kind: string): boolean => isNode(value) && value.kind === kind;
    const collection = (value: unknown, kind: string, message: string) => {
        if (!Array.isArray(value) || [...value].some(element => !hasKind(element, kind))) throw new Error(message);
    };
    if (node.kind === 'ApplicationSyntax') {
        const sources = (node as unknown as { eventSources?: unknown }).eventSources;
        if (sources !== undefined) collection(sources, 'EventSourceSyntax', 'Event sources must be a collection of event source nodes without null elements.');
    }
    if (node.kind === 'EventSourceSyntax' || node.kind === 'EventStreamSyntax') {
        const declaration = node as EventSourceSyntax | EventStreamSyntax;
        name(declaration.name);
        if (declaration.id !== null && (typeof declaration.id !== 'string' || declaration.id.trim() === '')) throw new Error('A rename pin must be nonempty.');
        const type = declaration.kind === 'EventSourceSyntax' ? declaration.identifier : declaration.streamId;
        if (type !== null && !hasKind(type, 'TypeRefSyntax')) throw new Error('Source identifiers and stream ids require type reference nodes.');
        if (type !== null && (type.isCollection || type.isOptional)) throw new Error('Source identifiers and stream ids require nonoptional scalar type references.');
        if (type !== null && !isSourceStreamTypeName(type.name)) throw new Error('Source identifiers and stream ids require an exact type reference name.');
        if (declaration.kind === 'EventSourceSyntax') {
            if (!Array.isArray(declaration.streams) || [...declaration.streams].some(stream => stream == null)) throw new Error('Event streams must be a collection without null elements.');
            collection(declaration.streams, 'EventStreamSyntax', 'Event streams must contain event stream nodes.');
        }
    }
    if (node.kind === 'CommandSyntax') {
        const command = node as CommandSyntax;
        if (command.stream?.propertyCandidate != null) throw new Error('The authoritative command stream cannot contain an ambiguous property candidate.');
        if (command.stream != null && !hasKind(command.stream, 'CommandStreamSyntax')) throw new Error('The authoritative command stream must be a command stream node.');
        if (command.streamCandidates !== undefined && !Array.isArray(command.streamCandidates)) throw new Error('Command stream candidates must be a collection.');
        for (const rejected of command.streamCandidates ?? []) {
            if (rejected == null) throw new Error('Command stream candidates cannot contain null.');
            if (!hasKind(rejected, 'CommandStreamSyntax')) throw new Error('Command stream candidates must contain command stream nodes.');
            validateSourceStream(rejected);
            if (rejected.propertyCandidate === null && command.stream == null) throw new Error('A duplicate route candidate requires an authoritative route.');
            if (rejected.propertyCandidate !== null && command.properties.some(property => property === rejected.propertyCandidate)) throw new Error('An ambiguous property is owned only by its stream candidate.');
        }
    }
    if (node.kind === 'CommandStreamSyntax') {
        const route = node as CommandStreamSyntax;
        name(route.eventSource);
        name(route.stream);
        if (route.streamId !== null && route.streamId.property !== 'streamId') throw new Error('A command stream maps only streamId.');
        const candidate = route.propertyCandidate;
        if (candidate !== null && (candidate.name !== 'stream' || candidate.type.name !== `${route.eventSource}.${route.stream}` || candidate.type.isCollection || candidate.type.isOptional || candidate.isGenerated || candidate.isIdentifier || route.streamId !== null)) throw new Error('An ambiguous route must retain its exact unmodified property candidate, without selecting nested routing.');
    }
}

function ordinal(left: string, right: string): number {
    return left < right ? -1 : left > right ? 1 : 0;
}
