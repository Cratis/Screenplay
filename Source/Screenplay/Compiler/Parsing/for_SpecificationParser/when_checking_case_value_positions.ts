// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseSpecificationSource } from '../../ScreenplayCompiler';
import { expandEffectiveSpecificationExamples } from '../SpecificationCommandExamples';

const declarations = `module M
  feature F
    slice StateChange S
      command Record
        amount Int
        returns amount
      event Recorded
        amount Int`;

const table = `
      specification Recording
        parameter expected Int
        case Small expected = 10
        when Record amount = 10
        then returns case.expected`;

describe('when checking case value positions', () => {
    it('refuses optional parameters at required targets even with nonnull values', () => {
        expect(parse(declarations + table.replace('expected Int', 'expected Int optional')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0585');
    });
    it('reports undeclared references within a table', () => {
        expect(parse(declarations + table.replace('case.expected', 'case.missing')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0584');
    });
    it('keeps unresolved target shapes unknown rather than guessing types', () => {
        const source = declarations + table.replace('when Record', 'when Missing');
        expect(parse(source).diagnostics.some(diagnostic => diagnostic.code === 'PLAY0587')).toEqual(false);
    });
    it('checks scalar response parameter references', () => {
        expect(parse(declarations + table).diagnostics).toEqual([]);
        const incompatible = table.replace('expected Int', 'expected Decimal');
        expect(parse(declarations + incompatible).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0587');
    });
    it('checks record response parameter references', () => {
        const source = declarations.replace('returns amount', 'returns\n          value Int = amount') + table.replace('then returns case.expected', 'then returns\n          value = case.expected');
        expect(parse(source).diagnostics).toEqual([]);
        expect(parse(source.replace('expected Int', 'expected Decimal')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0587');
    });
    it('accepts whole collections as case values', () => {
        const source = declarations.replaceAll('amount Int', 'amount Int[]').replace('returns amount', '') + table.replace('expected Int', 'expected Int[]').replace('expected = 10', 'expected = [10,20]').replace('when Record amount = 10', 'when Record amount = case.expected').replace('then returns case.expected', 'then Recorded amount = case.expected');
        expect(parse(source).diagnostics).toEqual([]);
        const effective = expandEffectiveSpecificationExamples(parse(source).value).specifications[0].effective;
        expect(effective.when!.values[0].source.kind).toEqual('ListExpressionSyntax');
    });
    it('accepts optional null values without defaulting omitted assignments', () => {
        const source = `module M\n  feature F\n    slice StateView S\n      readmodel View\n        note String optional\n      specification Showing\n        parameter note String optional\n        case Missing note = null\n        given readmodel View note = case.note\n        then readmodel View note = case.note`;
        expect(parse(source).diagnostics).toEqual([]);
    });
    it('checks absent read-model key types', () => {
        const source = `module M\n  feature F\n    slice StateView S\n      readmodel View\n        id Int\n      query ById => View\n        by id Int\n      specification Missing\n        parameter id Decimal\n        case One id = 1\n        then no readmodel View for case.id`;
        expect(parse(source).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0587');
    });
    it('checks query arguments and result properties', () => {
        const source = `module M\n  feature F\n    slice StateView S\n      readmodel View\n        id Int\n      query ById => View\n        by id Int\n      specification Reading\n        parameter id Int\n        case One id = 1\n        when query ById\n          id = case.id\n        then result\n          id = case.id`;
        expect(parse(source).diagnostics).toEqual([]);
        expect(parse(source.replace('parameter id Int', 'parameter id Decimal')).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0587')).toHaveLength(2);
    });
    it('checks a restated scalar route id', () => {
        const source = `eventsource Source\n  identifier String\n  stream Stream\n    streamId String\nmodule M\n  feature F\n    slice StateChange S\n      event E\n      specification Appending\n        parameter key String\n        case One key = "stream"\n        when append E\n          for "event"\n          stream Source.Stream\n            streamId = case.key`;
        expect(parse(source).diagnostics).toEqual([]);
    });
    it('reserves table addresses as well as derived addresses', () => {
        const source = declarations + table.replace('Recording', 'Recording_Small') + table;
        expect(parse(source).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0586');
    });
    it('refuses derived collisions in specification-only documents', () => {
        const source = `specification Recording\n  parameter value Int\n  case Small value = 1\n  when Record amount = case.value\nspecification Recording_Small\n  when Record amount = 1`;
        expect(parseSpecificationSource(source).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0586');
    });
    it('reports unknown parameter types and unused parameters', () => {
        expect(parse(declarations + table.replace('expected Int', 'expected Unknown')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0587');
        expect(parse(declarations + table.replace('then returns case.expected', 'then returns 10')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0588');
    });
    it('accepts whole structured parameter values', () => {
        const source = `type Detail\n  value Int\n` + declarations.replaceAll('amount Int', 'amount Detail').replace('returns amount', '') + table.replace('expected Int', 'expected Detail').replace('expected = 10', 'expected = {"value":10}').replace('when Record amount = 10', 'when Record amount = case.expected').replace('then returns case.expected', 'then Recorded amount = case.expected');
        expect(parse(source).diagnostics).toEqual([]);
    });
    it.each(['given clock case.instant', 'when clock case.instant', 'given caller\n    role case.role', 'given caller\n    claim "department" = case.value', 'when Record amount = {"value":case.amount}'])('refuses references in excluded positions: %s', step => {
        expect(parseSpecificationSource(`specification Excluded\n  ${step}`).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0584');
    });
    it('does not reserve case words inside quoted literals', () => {
        expect(parseSpecificationSource('specification Literal\n  given caller\n    role "case.role"\n  when Record amount = "case.value"').diagnostics).toEqual([]);
    });
    it('reports a repeated bad route value once at its case', () => {
        const source = `eventsource Source\n  identifier String\n  stream Stream\n    streamId String\nmodule M\n  feature F\n    slice StateChange S\n      event E\n      specification Appending\n        parameter key String\n        case Bad key = "e\u0301"\n        given E\n          for "event"\n          stream Source.Stream\n            streamId = case.key\n        when append E\n          for "event"\n          stream Source.Stream\n            streamId = case.key`;
        const diagnostics = parse(source).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0549');
        expect(diagnostics).toHaveLength(1);
        expect(diagnostics[0].message).toContain("Case 'Bad'");
        expect(diagnostics[0].location.line).toEqual(11);
    });
    it.each([
        ['parameter value Int\n    invalid', 'PLAY0577'],
        ['parameter value Int\n  case One\n    invalid\n  when Record amount = case.value', 'PLAY0582'],
        ['parameter value Int\n  case One value = 1\n    unknown = 2\n  when Record amount = case.value', 'PLAY0582'],
        ['parameter value Int\n  case One value = 1\n    value = 2\n  when Record amount = case.value', 'PLAY0582'],
        ['parameter value Int\n  case One value = 1\n    invalid child\n      nested = 2\n  when Record amount = case.value', 'PLAY0582'],
        ['parameter value Int\n  case One value = 1\n    value = 2\n      nested = 3\n  when Record amount = case.value', 'PLAY0583'],
        ['parameter value Int\n  case One value = "one" "two"\n  when Record amount = case.value', 'PLAY0583'],
        ['parameter value Int\n  case One value = 1\n  when Record amount = case.value.other', 'PLAY0584'],
    ])('refuses malformed declarations and assignments: %s', (body, code) => {
        expect(parseSpecificationSource(`specification Table\n  ${body}`).diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
    });
});
