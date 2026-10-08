// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSyntax, ValidationRuleSyntax } from './Commands';
import { ConceptSyntax } from './Declarations';
import { CommandStreamSyntax, EventSourceSyntax, EventStreamSyntax, EventStreamIdPartSyntax } from './EventSources';
import { PropertyMappingSyntax } from './Expressions';
import { HandlerSyntax, ImplementationSyntax, ImplementationHintSyntax } from './Implementations';
import { InvalidSyntaxJson } from './InvalidSyntaxJson';
import { OperationPhaseSyntax } from './Operations';
import { ProducesSyntax } from './Reactions';
import { SyntaxNode } from './SyntaxNode';
import { SpecificationEventSyntax, SpecificationStreamSyntax } from './Specifications';
import { isBlankImplementationHint } from '../Text/ImplementationHintText';
import { isSourceStreamName, isSourceStreamTypeName, sourceStreamPattern } from '../Text/SourceStreamNames';

// .NET \w is evaluated per UTF-16 code unit, so a supplementary-plane letter is not a name character.
const ruleNamePattern = sourceStreamPattern('^[A-Za-z_]\\w*$');

const hasKind = (value: unknown, kind: string): boolean => typeof value === 'object' && value !== null && !Array.isArray(value) && (value as SyntaxNode).kind === kind;
const refuse = (message: string): never => { throw new InvalidSyntaxJson(message); };
const collection = (value: unknown, kind: string, message: string): void => {
    if (!Array.isArray(value)) return refuse(message);
    // Indexed, so no caller-overridable iteration method or iterator is consulted.
    for (let position = 0; position < value.length; position++) if (!hasKind(value[position], kind)) refuse(message);
};

// The native ImplementationInvariants / OperationInvariants / EventSourceInvariants contracts.
// Shared by the isolated strict reader and the writer, without pulling the transport schema into Monaco.
export function validateSyntaxInvariants(node: SyntaxNode): void {
    if (node.kind === 'ConceptSyntax') {
        const concept = node as ConceptSyntax;
        for (const validation of concept.validations ?? []) {
            if (validation.kind === 'DeclarativeValidateSyntax' && validation.rules.some(rule => rule.implementation != null)) refuse('Implementation wrappers on named rules are supported only on commands.');
        }
    }
    if (node.kind === 'ValidationRuleSyntax') {
        const rule = node as ValidationRuleSyntax;
        if (rule.implementation != null) {
            if (!hasKind(rule.implementation, 'ImplementationSyntax')) refuse('A named rule requires an ImplementationSyntax wrapper.');
            if (rule.rule !== 'Rule' || rule.value?.kind !== 'PathExpressionSyntax' || !hasKind(rule.value, 'PathExpressionSyntax') || typeof rule.value.path !== 'string' || ruleNamePattern.exec(rule.value.path)?.[0] !== rule.value.path) refuse('An implementation wrapper requires a valid named rule.');
            if (rule.file != null && rule.code != null) refuse('A named rule has at most one file or inline payload.');
            // Native typed transport enforces these payload members in both numeric modes.
            if (rule.file != null && (!hasKind(rule.file, 'FileReferenceSyntax') || typeof rule.file.path !== 'string')) refuse('A named rule file requires a FileReferenceSyntax payload with a string path.');
            if (rule.code != null && (!hasKind(rule.code, 'CodeBlockSyntax') || typeof rule.code.language !== 'string' || typeof rule.code.code !== 'string')) refuse('A named rule inline payload requires a CodeBlockSyntax with string language and code.');
            validateSyntaxInvariants(rule.implementation);
        }
    }
    if (node.kind === 'ProducesSyntax') {
        const production = node as ProducesSyntax;
        const operation = production.inlineOperation;
        if (operation != null) {
            if (production.inlineEvent != null) refuse('A production cannot declare both an event and an operation.');
            if (production.when != null || production.event !== operation.name || production.for != null || (production.tags ?? []).length > 0) refuse('An inline operation requires a matching unconditional target without event metadata.');
            collection(operation.inputs, 'PropertySyntax', 'Operation inputs must be a collection of properties.');
            collection(production.mappings, 'PropertyMappingSyntax', 'Production mappings must be a collection of mappings.');
            if (operation.inputs.length !== production.mappings.length) refuse('Inline operation inputs and mappings must correspond in order.');
            for (let position = 0; position < operation.inputs.length; position++) if (operation.inputs[position].name !== production.mappings[position].property) refuse('Inline operation inputs and mappings must correspond in order.');
        }
    }
    if (node.kind === 'OperationPhaseSyntax') {
        const phase = node as OperationPhaseSyntax;
        if (phase.file != null && phase.code != null) refuse('An operation phase has at most one file or inline payload.');
        if (phase.implementation != null) validateSyntaxInvariants(phase.implementation);
    }
    if (node.kind === 'HandlerSyntax') {
        const handler = node as HandlerSyntax;
        // Native transport keeps old unwrapped structural handlers; only the new wrapper is admitted here.
        if (handler.implementation != null && handler.file != null && handler.code != null) refuse('A handler has at most one file or inline payload.');
    }
    if (node.kind === 'ImplementationSyntax') {
        const implementation = node as ImplementationSyntax;
        collection(implementation.hints, 'ImplementationHintSyntax', 'Implementation hints must be a collection of nonblank hints.');
        for (let position = 0; position < implementation.hints.length; position++) validateSyntaxInvariants(implementation.hints[position]);
    }
    if (node.kind === 'ImplementationHintSyntax') {
        const hint = node as ImplementationHintSyntax;
        if (typeof hint.text !== 'string' || isBlankImplementationHint(hint.text)) refuse('An implementation hint must be nonblank.');
    }
    validateSourceStream(node);
}

function validateSourceStream(node: SyntaxNode): void {
    const name = (value: string): void => { if (!isSourceStreamName(value)) refuse('Event source and stream names must be identifiers.'); };
    const mappings = (scalar: PropertyMappingSyntax | null, parts: PropertyMappingSyntax[]): void => {
        collection(parts, 'PropertyMappingSyntax', 'Stream id part mappings must be a collection.');
        if (scalar !== null && parts.length > 0) refuse('A route cannot map both scalar and composite stream ids.');
        parts.forEach(part => name(part.property));
    };
    if (node.kind === 'ApplicationSyntax') {
        const sources = (node as unknown as { eventSources?: unknown }).eventSources;
        if (sources !== undefined) collection(sources, 'EventSourceSyntax', 'Event sources must be a collection of event source nodes without null elements.');
    }
    if (node.kind === 'EventSourceSyntax' || node.kind === 'EventStreamSyntax') {
        const declaration = node as EventSourceSyntax | EventStreamSyntax;
        name(declaration.name);
        if (declaration.id !== null && (typeof declaration.id !== 'string' || declaration.id.trim() === '')) refuse('A rename pin must be nonempty.');
        const type = declaration.kind === 'EventSourceSyntax' ? declaration.identifier : declaration.streamId;
        if (type !== null && !hasKind(type, 'TypeRefSyntax')) refuse('Source identifiers and stream ids require type reference nodes.');
        if (type !== null && (type.isCollection || type.isOptional)) refuse('Source identifiers and stream ids require nonoptional scalar type references.');
        if (type !== null && !isSourceStreamTypeName(type.name)) refuse('Source identifiers and stream ids require an exact type reference name.');
        if (declaration.kind === 'EventSourceSyntax') collection(declaration.streams, 'EventStreamSyntax', 'Event streams must contain event stream nodes without null elements.');
        else {
            collection(declaration.streamIdParts, 'EventStreamIdPartSyntax', 'Stream id parts must be a collection.');
            if (declaration.streamId !== null && declaration.streamIdParts.length > 0) refuse('A stream cannot declare both scalar and composite stream ids.');
            declaration.streamIdParts.forEach(part => validateSourceStream(part));
        }
    }
    if (node.kind === 'EventStreamIdPartSyntax') {
        const part = node as EventStreamIdPartSyntax;
        name(part.name);
        if (!hasKind(part.type, 'TypeRefSyntax')) refuse('A stream id part requires a type.');
        if (part.type.isOptional || part.type.isCollection) refuse('Source identifiers and stream ids require nonoptional scalar type references.');
        if (!isSourceStreamTypeName(part.type.name)) refuse('Source identifiers and stream ids require an exact type reference name.');
    }
    if (node.kind === 'CommandSyntax') {
        const command = node as CommandSyntax;
        if (command.stream?.propertyCandidate != null) refuse('The authoritative command stream cannot contain an ambiguous property candidate.');
        if (command.stream != null && !hasKind(command.stream, 'CommandStreamSyntax')) refuse('The authoritative command stream must be a command stream node.');
        if (command.streamCandidates !== undefined) collection(command.streamCandidates, 'CommandStreamSyntax', 'Command stream candidates must be a collection without null elements.');
        const candidates = command.streamCandidates ?? [];
        for (let position = 0; position < candidates.length; position++) {
            const rejected = candidates[position];
            validateSourceStream(rejected);
            if (rejected.propertyCandidate === null && command.stream == null) refuse('A duplicate route candidate requires an authoritative route.');
            if (rejected.propertyCandidate !== null) for (let index = 0; index < command.properties.length; index++) if (command.properties[index] === rejected.propertyCandidate) refuse('An ambiguous property is owned only by its stream candidate.');
        }
    }
    if (node.kind === 'SpecificationEventSyntax') {
        const occurrence = node as SpecificationEventSyntax;
        if (occurrence.stream != null && occurrence.noStream != null) refuse('An event occurrence cannot declare both stream and no stream.');
        if (occurrence.stream != null && !hasKind(occurrence.stream, 'SpecificationStreamSyntax')) refuse('A specification stream requires a specification stream node.');
        if (occurrence.noStream != null && !hasKind(occurrence.noStream, 'SpecificationNoStreamSyntax')) refuse('An unrouted expectation requires a specification no stream node.');
    }
    if (node.kind === 'SpecificationStreamSyntax') {
        const route = node as SpecificationStreamSyntax;
        name(route.eventSource);
        name(route.stream);
        if (route.streamId !== null && route.streamId.property !== 'streamId') refuse('A specification stream maps only streamId.');
        mappings(route.streamId, route.streamIdParts);
    }
    if (node.kind === 'CommandStreamSyntax') {
        const route = node as CommandStreamSyntax;
        name(route.eventSource);
        name(route.stream);
        if (route.streamId !== null && route.streamId.property !== 'streamId') refuse('A command stream maps only streamId.');
        mappings(route.streamId, route.streamIdParts);
        const candidate = route.propertyCandidate;
        if (candidate !== null && (candidate.name !== 'stream' || candidate.type.name !== `${route.eventSource}.${route.stream}` || candidate.type.isCollection || candidate.type.isOptional || candidate.isGenerated || candidate.isIdentifier || route.streamId !== null || route.streamIdParts.length > 0)) refuse('An ambiguous route must retain its exact unmodified property candidate, without selecting nested routing.');
    }
}
