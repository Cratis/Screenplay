// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { toSyntaxJson as writeSyntaxJson } from '../Syntax/SyntaxJson';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { EventSourceSyntax } from '../Syntax/EventSources';
import { EventSourceCatalog } from '../Syntax/EventSourceCatalog';

const toSyntaxJson = <Node extends SyntaxNode>(node: Node) => writeSyntaxJson(node);

const source = parse('eventsource Account\n  identifier String\n  stream Transactions\n    streamId String').value.eventSources![0];
const command = parse('eventsource Account\n  stream Transactions\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Transactions').value.modules[0].features[0].slices[0].commands[0];
const route = command.stream!;
const mapping = { kind: 'PropertyMappingSyntax' as const, property: 'streamId', source: { kind: 'LiteralExpressionSyntax' as const, value: 'key', location: route.location }, location: route.location };

describe('when serializing source-stream authoring invariants', () => {
    it.each(['', 'Account.Transactions', 'Account\n', '1Account'])('should reject invalid declaration names %s', name => expect(() => toSyntaxJson({ ...source, name })).toThrow());
    it.each([' ', ''])('should reject blank rename pins %s', id => expect(() => toSyntaxJson({ ...source, id })).toThrow());
    it('should reject invalid scalar source and stream type modifiers', () => {
        for (const flag of ['isOptional', 'isCollection']) {
            expect(() => toSyntaxJson({ ...source, identifier: { ...source.identifier!, [flag]: true } })).toThrow();
            expect(() => toSyntaxJson({ ...source.streams[0], streamId: { ...source.streams[0].streamId!, [flag]: true } })).toThrow();
        }
    });
    it('should reject wrong mapping targets and malformed ambiguous candidates', () => {
        expect(() => toSyntaxJson({ ...route, streamId: { ...mapping, property: 'payload' } })).toThrow();
        const candidate = { kind: 'PropertySyntax' as const, name: 'stream', type: { kind: 'TypeRefSyntax' as const, name: 'Account.Transactions', isOptional: false, isCollection: false, location: route.location }, isIdentifier: false, isGenerated: false, location: route.location };
        expect(() => toSyntaxJson({ ...route, propertyCandidate: candidate })).not.toThrow();
        for (const propertyCandidate of [{ ...candidate, name: 'other' }, { ...candidate, type: { ...candidate.type, name: 'Other.Stream' } }, { ...candidate, type: { ...candidate.type, isOptional: true } }, { ...candidate, type: { ...candidate.type, isCollection: true } }, { ...candidate, isIdentifier: true }, { ...candidate, isGenerated: true }]) {
            expect(() => toSyntaxJson({ ...route, propertyCandidate })).toThrow();
        }
        expect(() => toSyntaxJson({ ...route, propertyCandidate: candidate, streamId: mapping })).toThrow();
        expect(() => toSyntaxJson({ ...route, streamId: mapping })).not.toThrow();
        expect(() => toSyntaxJson({ ...route, eventSource: 'Invalid.Source' })).toThrow();
        expect(() => toSyntaxJson({ ...route, stream: 'Invalid.Stream' })).toThrow();
    });
    it('should default old programmatic root and command omissions as the Csharp reader does', () => {
        const oldApplication = parse('').value;
        const { eventSources: _sources, ...oldRoot } = oldApplication;
        const { stream: _stream, ...oldCommand } = command;
        expect((toSyntaxJson(oldRoot) as Record<string, unknown>).eventSources).toEqual([]);
        expect((toSyntaxJson(oldCommand) as Record<string, unknown>).stream).toBeNull();
        expect(new EventSourceCatalog(oldRoot).resolve('Account', 'Transactions').kind).toBe('notFound');
    });
    it('should retain declaration-only and unkeyed stream members exactly', () => {
        const node = parse('eventsource Account\n  description "Account"\n  id "Old"\n  stream Transactions\n    description "History"\n    id "OldStream"').value;
        expect(toSyntaxJson(node.eventSources![0])).toEqual({ kind: 'EventSourceSyntax', description: 'Account', id: 'Old', identifier: null, name: 'Account', streams: [{ kind: 'EventStreamSyntax', description: 'History', id: 'OldStream', name: 'Transactions', streamId: null }] });
    });
    it('should not admit null or non-string new declaration names in programmatic trees', () => {
        for (const name of [null, 42]) expect(() => toSyntaxJson({ ...source, name } as unknown as EventSourceSyntax)).toThrow();
        expect(() => toSyntaxJson({ ...source, id: 42 } as unknown as EventSourceSyntax)).toThrow();
    });
});
