// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import { CommandSyntax } from './Commands';
import { OperationPhaseSyntax } from './Operations';
import { CommandStreamSyntax, EventSourceSyntax, EventStreamSyntax } from './EventSources';
import { ProducesSyntax } from './Reactions';
import { isBlankImplementationHint } from '../Text/ImplementationHintText';
import { pattern } from '../Text/patterns';

export type SyntaxJsonValue = string | number | boolean | null | SyntaxJsonValue[] | { [member: string]: SyntaxJsonValue };

const sourceStreamName = pattern('^[A-Za-z_]\\w*(?![\\s\\S])');

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
        validateSourceStream(value);
        const result: { [member: string]: SyntaxJsonValue } = { kind: value.kind };
        const structural = { ...value } as unknown as Record<string, unknown>;
        if (value.kind === 'ApplicationSyntax' && structural.eventSources === undefined) structural.eventSources = [];
        if (value.kind === 'CommandSyntax' && structural.stream === undefined) structural.stream = null;
        if (value.kind === 'CommandSyntax' && structural.streamCandidates === undefined) structural.streamCandidates = [];
        const members = Object.keys(structural).filter(member => member !== 'kind' && member !== 'location' && member !== 'targetLocation' && member !== 'referenceLocation' && member !== 'referenceLength' && member !== 'nameWasEscaped' && !(value.kind === 'OperationSyntax' && member === 'usesLocation')).sort(ordinal);
        for (const member of members) {
            result[member] = write(structural[member]);
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

function validateSourceStream(node: SyntaxNode): void {
    const name = (value: string) => { if (typeof value !== 'string' || !sourceStreamName.test(value)) throw new Error('Event source and stream names must be identifiers.'); };
    if (node.kind === 'EventSourceSyntax' || node.kind === 'EventStreamSyntax') {
        const declaration = node as EventSourceSyntax | EventStreamSyntax;
        name(declaration.name);
        if (declaration.id !== null && (typeof declaration.id !== 'string' || declaration.id.trim() === '')) throw new Error('A rename pin must be nonempty.');
        const type = declaration.kind === 'EventSourceSyntax' ? declaration.identifier : declaration.streamId;
        if (type !== null && (type.isCollection || type.isOptional)) throw new Error('Source identifiers and stream ids require nonoptional scalar type references.');
    }
    if (node.kind === 'CommandSyntax') {
        const command = node as CommandSyntax;
        if (command.stream?.propertyCandidate != null) throw new Error('The authoritative command stream cannot contain an ambiguous property candidate.');
        if (command.streamCandidates !== undefined && !Array.isArray(command.streamCandidates)) throw new Error('Command stream candidates must be a collection.');
        for (const rejected of command.streamCandidates ?? []) {
            if (rejected == null) throw new Error('Command stream candidates cannot contain null.');
            validateSourceStream(rejected);
            if (rejected.propertyCandidate === null && command.stream == null) throw new Error('A duplicate route candidate requires an authoritative route.');
            if (rejected.propertyCandidate !== null && command.properties.some(property => JSON.stringify(toSyntaxJson(property)) === JSON.stringify(toSyntaxJson(rejected.propertyCandidate!)))) throw new Error('An ambiguous property is owned only by its stream candidate.');
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
