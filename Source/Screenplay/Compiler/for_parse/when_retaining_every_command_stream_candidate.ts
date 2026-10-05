// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { CommandSyntax } from '../Syntax/Commands';

const declarations = 'import Account.Transactions\ntype Transactions\n  value String\neventsource Account\n  stream Onboarding\n  stream Transactions\n';
const prefix = 'module M\n  feature F\n    slice StateChange S\n      command C\n';
const ambiguous = '        stream Account.Transactions // ambiguous\n          deeper String // legacy\n';
const resolved = '        stream Account.Onboarding // resolved\n';

class Collector extends ScreenplaySyntaxWalker {
    readonly nodes: SyntaxNode[] = [];
    override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
}

describe('when retaining every rejected command stream header', () => {
    it.each([[resolved + ambiguous, 1, true], [ambiguous + resolved, 1, true], [ambiguous + ambiguous + ambiguous, 3, false], [resolved + resolved, 1, true]] as const)('should transport all candidates for %s', (body, count, hasRoute) => {
        const result = parse((declarations + prefix + body).replaceAll('\n', '\r\n'), 'input.play');
        expect(result.success).toBe(false);
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(command.streamCandidates).toHaveLength(count);
        expect(command.stream !== null).toBe(hasRoute);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain(body.includes(ambiguous) ? 'PLAY0505' : 'PLAY0504');
        expect(command.properties.every(property => property.name === 'deeper')).toBe(true);
        const structural = JSON.parse(JSON.stringify(toSyntaxJson(command)));
        expect(structural.streamCandidates).toHaveLength(count);
        expect(JSON.stringify(structural)).not.toContain('referenceLocation');
        const walker = new Collector(); walker.visitCommand(command);
        const properties = walker.nodes.filter(node => node.kind === 'PropertySyntax');
        expect(new Set(properties.map(property => property.location.line)).size).toBe(properties.length);
        for (const entries of [
            [['application.play', declarations + 'module M\n  feature F\n    import "barrel.play"'], ['barrel.play', 'import "command.play"'], ['command.play', 'slice StateChange S\n  command C\n' + body.replaceAll('        ', '    ')]],
            [['command.play', 'slice StateChange S\n  command C\n' + body.replaceAll('        ', '    ')], ['barrel.play', 'import "command.play"'], ['application.play', declarations + 'module M\n  feature F\n    import "barrel.play"']],
        ]) {
            const assembled = compileApplication(new Map(entries as [string, string][]));
            expect(assembled.success).toBe(false);
            expect(assembled.value.modules[0].features[0].slices[0].commands[0].streamCandidates).toHaveLength(count);
        }
        const candidate = command.streamCandidates![0];
        if (candidate.propertyCandidate !== null) {
            const authoritativeAmbiguity = { ...command, stream: candidate, streamCandidates: [] };
            const duplicatedProperty = { ...command, properties: [...command.properties, candidate.propertyCandidate!] };
            expect(() => toSyntaxJson(authoritativeAmbiguity)).toThrow();
            expect(() => toSyntaxJson(duplicatedProperty)).toThrow();
        }
    });
    it.each([false, true])('should transport independent equal properties in reverse header order %s', reverse => {
        const escaped = '        @stream Account.Transactions\n';
        const bare = '        stream Account.Transactions\n';
        const result = parse(declarations + prefix + (reverse ? bare + escaped : escaped + bare));
        expect(result.success).toBe(false);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0505');
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(command.stream).toBeNull();
        expect(command.properties).toHaveLength(1);
        expect(command.streamCandidates).toHaveLength(1);
        const property = command.properties[0];
        const candidate = command.streamCandidates![0].propertyCandidate!;
        expect(property).not.toBe(candidate);
        expect(property.nameWasEscaped).toBe(true);
        expect(toSyntaxJson(property)).toEqual(toSyntaxJson(candidate));
        const json = toSyntaxJson(result.value);
        expect(JSON.stringify(json)).not.toContain('nameWasEscaped');
        expect(toSyntaxJson(JSON.parse(JSON.stringify(json)))).toEqual(json);
        const independent = { ...command, properties: [{ ...candidate }] };
        expect(independent.properties[0]).not.toBe(candidate);
        expect(independent.properties[0]).toEqual(candidate);
        expect(() => toSyntaxJson(independent)).not.toThrow();
        // Validation must see the original graph before the codec detaches each JSON path.
        const aliased = { ...command, properties: [candidate] };
        expect(() => toSyntaxJson(aliased)).toThrow('An ambiguous property is owned only by its stream candidate.');
    });
    it.each([false, true])('should preserve repeated candidates without deduplication for the same instance %s', sameInstance => {
        const result = parse(declarations + prefix + ambiguous + ambiguous);
        expect(result.success).toBe(false);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0505')).toHaveLength(2);
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        const candidates = command.streamCandidates!;
        expect(candidates[0]).not.toBe(candidates[1]);
        expect(candidates[0].propertyCandidate).not.toBe(candidates[1].propertyCandidate);
        expect(toSyntaxJson(candidates[0])).toEqual(toSyntaxJson(candidates[1]));
        const repeated = {
            ...command,
            properties: [{ ...candidates[0].propertyCandidate! }],
            streamCandidates: [candidates[0], sameInstance ? candidates[0] : { ...candidates[0], propertyCandidate: { ...candidates[0].propertyCandidate! } }],
        };
        const json = toSyntaxJson(repeated);
        const decoded = JSON.parse(JSON.stringify(json));
        expect(decoded.stream).toBeNull();
        expect(decoded.streamCandidates).toHaveLength(2);
        expect(decoded.streamCandidates[0]).not.toBe(decoded.streamCandidates[1]);
        expect(decoded.streamCandidates[0].propertyCandidate).not.toBe(decoded.properties[0]);
        expect(toSyntaxJson(decoded)).toEqual(json);
    });
    it('should reject malformed programmatic candidate ownership', () => {
        const command = parse(declarations + prefix + resolved).value.modules[0].features[0].slices[0].commands[0];
        for (const invalid of [
            { ...command, streamCandidates: null },
            { ...command, streamCandidates: [null] },
            { ...command, stream: null, streamCandidates: [command.stream!] },
        ]) expect(() => toSyntaxJson(invalid as unknown as CommandSyntax)).toThrow();
        const old = { ...command, streamCandidates: undefined };
        expect(JSON.parse(JSON.stringify(toSyntaxJson(old))).streamCandidates).toEqual([]);
    });
    // TypeScript has no configurable registry. Capture and committed parsing both use the
    // fixed language set in ImplementationParser; unknown languages retain existing errors.
    it.each(['csharp', 'typescript', 'react', 'html', 'sql'])('should not invent declarations inside a %s fence', language => {
        const source = prefix + '        stream Fake.S\n          deeper String\n        handler\n          ```' + language + '\neventsource Fake\n  stream S\nimport Account.Transactions\ntype Transactions\n  value String\n// escaped JSON token: `\n          ```';
        const result = parse(source);
        expect(result.success).toBe(true);
        expect(result.value.eventSources).toEqual([]);
        expect(result.value.types).toEqual([]);
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(command.stream).toBeNull();
        expect(command.properties.map(property => property.name)).toEqual(['stream', 'deeper']);
    });
    it('should keep unknown-language reporting without expanding the fixed language set', () => {
        const result = parse(prefix + '        handler\n          ```python\n          print("eventsource Fake")\n          ```');
        expect(result.success).toBe(false);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0163');
    });
});
