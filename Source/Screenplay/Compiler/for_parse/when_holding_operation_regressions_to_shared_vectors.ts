// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { LineReader } from '../Parsing/LineReader';
import { validateOperations } from '../Parsing/OperationValidator';
import { ParserContext } from '../Parsing/ParserContext';
import { parse } from '../ScreenplayCompiler';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { implicitDestination, requiresExplicitDestinations } from '../Syntax/ProductionDestinations';

const vectors = JSON.parse(readFileSync(new URL('../Conformance/operation-regressions.json', import.meta.url), 'utf8')) as {
    prefix: string;
    cases: { name: string; before?: string; body: string; codes: string[]; absent: string[] }[];
    documents: { path: string; source: string }[];
};

describe('when holding operation regressions to shared vectors', () => {
    it.each(vectors.cases)('$name', vector => {
        const result = parse((vector.before ?? '') + vectors.prefix + vector.body);
        const codes = result.diagnostics.map(diagnostic => diagnostic.code);
        for (const code of vector.codes) expect(codes).toContain(code);
        for (const code of vector.absent) expect(codes).not.toContain(code);
    });
    it('should preserve mixed-kind candidate occurrences and exclude them in public destination helpers', () => {
        const result = parse(vectors.prefix + vectors.cases[0].body);
        const slice = result.value.modules[0].features[0].slices[0];
        const resolver = new AuthoringProductionResolver(result.value);
        expect(resolver.resolve('Send', slice).candidates.map(candidate => candidate.kind)).toEqual(['event', 'operation']);
        const context = { resolver, slice };
        const command = slice.commands[0];
        expect(requiresExplicitDestinations(command, context)).toBe(false);
        expect(implicitDestination(command, command.produces[0], context)).toBe('id');
        expect(implicitDestination(command, command.produces[1], context)).toBeUndefined();
    });
    it('should validate imported mixed productions once after assembly and expose no operation event destination', () => {
        const result = compileApplication(new Map(vectors.documents.map(document => [document.path, document.source])), ['root.play']);
        expect(result.diagnostics).toEqual([]);
        const slice = result.value.modules[0].features[0].slices.find(slice => slice.name === 'Here')!;
        const command = slice.commands[0];
        const context = { resolver: new AuthoringProductionResolver(result.value), slice };
        expect(requiresExplicitDestinations(command, context)).toBe(false);
        expect(implicitDestination(command, command.produces[0], context)).toBe('id');
        expect(implicitDestination(command, command.produces[1], context)).toBeUndefined();
    });
    it('should suppress inline operation destinations even without the additive resolution context', () => {
        const result = parse(vectors.prefix + '      command C\n        id Uuid identifier\n        produces event Recorded\n        produces operation Send\n          uses Mailer\n');
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(requiresExplicitDestinations(command)).toBe(false);
        expect(implicitDestination(command, command.produces[0])).toBe('id');
        expect(implicitDestination(command, { ...command.produces[0] })).toBe('id');
        expect(implicitDestination(command, command.produces[1])).toBeUndefined();
    });
    it('should reject programmatic raw assertions through unknown imported shapes', () => {
        const vector = vectors.cases.find(vector => vector.name === 'unknown imported assertion remains concrete')!;
        const syntax = parse(vector.before + vectors.prefix + vector.body.replace('missing', 'null')).value;
        const mapping = syntax.modules[0].features[0].slices[0].specifications[0].thenOperations![0].values[0];
        Object.assign(mapping, { source: { kind: 'RawExpressionSyntax', text: 'missing', location: mapping.location } });
        const context = new ParserContext(new LineReader([]));
        validateOperations(syntax, context);
        expect(context.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0502');
    });
    it('should locate concrete and invalid assertion values at their complete RHS', () => {
        const vector = vectors.cases.find(vector => vector.name === 'strict quoted assertion')!;
        const invalid = parse(vectors.prefix + vector.body);
        expect(invalid.diagnostics.find(diagnostic => diagnostic.code === 'PLAY0502')?.location.column).toBe(18);
        const valid = parse(vectors.prefix + vector.body.replace('"Ada" + "Bob"', '"α𝄞"'));
        const mapping = valid.value.modules[0].features[0].slices[0].specifications[0].thenOperations![0].values[0];
        expect(mapping.source.location.column).toBe(18);
        expect(mapping.source.kind === 'LiteralExpressionSyntax' && mapping.source.value).toBe('α𝄞');
    });
});
