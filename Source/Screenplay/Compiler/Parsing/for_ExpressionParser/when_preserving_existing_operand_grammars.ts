// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseCaptureSource, parseProjectionSource, parseSpecificationSource } from '../../ScreenplayCompiler';
import { parseProjectionExpression } from '../ProjectionExpressionParser';
import { parseCondition } from '../ConditionParser';
import { sourceContext } from '../SourceOptionsParser';
import { splitLines } from '../SourceLineSplitter';
import { parseLiteral } from '../ExpressionParser';
import { compatibleValue } from '../ResponseValidator';
import { parseExactNumber } from '../../Syntax/ExactNumber';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { mergeDocuments } from '../../Files/PlayFolderMerge';

const location = { line: 1, column: 1 };
const context = () => sourceContext([]);

describe('when preserving existing operand grammars', () => {
    it.each(['`open', '`value ${missing`', '$causedBy', 'literal nope', 'not an expression'])('should model malformed or opaque projection text without adding Legacy diagnostics: %s', text => {
        const owner = context();
        const value = parseProjectionExpression(text, location, owner);
        value.kind.should.be.oneOf(['TemplateExpressionSyntax', 'CausedByExpressionSyntax', 'RawExpressionSyntax']);
        owner.diagnostics.should.deep.equal([]);
    });
    it.each(['', 'amount', 'amount invalid 1', 'amount ==', 'amount > 1 and', 'amount > 1 or'])('should retain a null malformed comparison rather than invent an operand: %s', text => {
        expect(parseCondition(context(), text, location)).toBeNull();
    });
    it.each(['amount == 1e3+4', '(amount == 1e0', 'amount ==', 'amount > 1 and', 'amount > 1 or'])('should reject incomplete or cut-off Exact comparisons: %s', text => {
        const owner = sourceContext(splitLines('numbers exact\n'));
        parseCondition(owner, text, location);
        owner.diagnostics.some(diagnostic => diagnostic.severity === 'error').should.equal(true);
    });
    it.each(['claim "limit" matches 1e3+4', '(authenticated', 'role unquoted', 'authenticated and', 'authenticated or'])('should reject incomplete or cut-off Exact policy conditions: %s', text => {
        parse(`numbers exact\npolicy P\n  require ${text}\n`).success.should.equal(false);
    });
    it('should reject a malformed Exact capture header while preserving Legacy fallback', () => {
        parseCaptureSource('numbers exact\ncapture\n').success.should.equal(false);
    });
    it('should preserve the starts-with operator', () => {
        parseCondition(context(), 'name starts with "x"', location)!.kind.should.equal('ComparisonConditionSyntax');
    });
    it('should refuse nonnumeric opaque text as an exact literal without entering Legacy conversion', () => {
        expect(parseLiteral('opaque', location, sourceContext(splitLines('numbers exact\n')))).toBeUndefined();
    });
    it.each(['projection', 'capture', 'specification'])('should reject the wrong standalone declaration family: %s', family => {
        const result = family === 'projection' ? parseProjectionSource('capture C\n') : family === 'capture' ? parseCaptureSource('projection P\n') : parseSpecificationSource('capture C\n');
        result.success.should.equal(false);
    });
    it.each([
        'seed invalid\n  ignored\n',
        'seed\n  invalid group\n    ignored\n',
        'seed\n  for "global"\n    invalid\n      ignored\n',
        'seed\n  for "global"\n    Added\n      not a mapping\n',
        'policy P\n  require\n',
        'policy P\n  require role unquoted\n',
        'policy P\n  require claim "limit" invalid 1\n',
        'policy P\n  require claim "limit" matches\n',
        'policy P\n  require authenticated and\n',
        'policy P\n  require authenticated or\n',
        'policy P\n  require authenticated\n  require role "ignored"\n',
        'policy P\n  file policy.cs\n  file ignored.cs\n  unknown\n',
        'policy P\n  ```csharp\nreturn true;\n  ```\n  file ignored.cs\n',
    ])('should enrich formerly opaque declarations without changing Legacy diagnostics: %s', source => {
        parse(source).diagnostics.should.deep.equal([]);
    });
    it.each([
        'capture\n  unknown\n',
        'capture C\n  children invalid\n    unknown\n  nested invalid value\n    unknown\n',
        'capture C\n  map\n    invalid\n    split invalid\n      invalid target!\n    amount = `unterminated\n  append invalid\n',
        'capture C\n  append Added\n    unknown\n    when\n    when amount from 1\n    when amount or\n    when amount or other and third\n',
    ])('should retain existing capture fallback behavior: %s', source => {
        const parsed = parseCaptureSource(source);
        parsed.diagnostics.should.deep.equal([]);
        parsed.value.length.should.equal(1);
    });
    it.each(['validate csharp\n          ghost Uuid identifier', 'validate\n          ```unknown\n          ghost Uuid identifier\n          ```'])('should preserve opaque validation ownership after an invalid code opener: %s', block => {
        const parsed = parse(`module M\n  feature F\n    slice StateChange S\n      command C\n        ${block}\n        produces Added\n`);
        const command = parsed.value.modules[0].features[0].slices[0].commands[0];
        command.properties.should.deep.equal([]);
        command.produces.map(production => production.event).should.deep.equal(['Added']);
    });
    it('should compare exact whole and fractional values using canonical facts', () => {
        const whole = { kind: 'LiteralExpressionSyntax' as const, value: parseExactNumber('9223372036854775808')!, location };
        const fraction = { ...whole, value: parseExactNumber('1.0000000000000000000000000001')! };
        const type = { kind: 'TypeRefSyntax' as const, name: 'Int', isCollection: false, isOptional: false, location };
        compatibleValue(whole, type, new Map(), new Map()).should.equal(true);
        compatibleValue(fraction, type, new Map(), new Map()).should.equal(false);
        compatibleValue({ ...whole, value: 'text' }, type, new Map(), new Map()).should.equal(false);
        compatibleValue(fraction, { ...type, name: 'Decimal' }, new Map(), new Map()).should.equal(true);
        compatibleValue({ ...whole, value: true }, { ...type, name: 'Decimal' }, new Map(), new Map()).should.equal(false);
    });
    it('should recognize declarations on restored or programmatic roots without parser-owned marks', () => {
        const root = { ...parse('policy P\n  require authenticated\n').value };
        mergeDocuments([{ value: root, success: true, diagnostics: [] }]).success.should.equal(true);
    });
    it('should refuse deep or unsupported Exact programmatic values at serialization', () => {
        const value = parse('numbers exact\nseed\n  for "global"\n    Added\n      amount = 1\n').value;
        const edited = value as unknown as { seeds: { groups: { events: { properties: { source: { value: unknown } }[] }[] }[] }[] };
        edited.seeds[0].groups[0].events[0].properties[0].source.value = undefined;
        expect(() => toSyntaxJson(value)).toThrow();
        let nested: unknown = { kind: 'LiteralExpressionSyntax', value: parseExactNumber('1'), location };
        for (let index = 0; index < 100; index++) nested = { kind: 'ListExpressionSyntax', items: [nested], location };
        const deep = { kind: 'CaptureSyntax', sourceOptions: { numericMode: 'exact' }, name: 'C', source: null, key: null, map: [], children: [], nested: [], appends: [{ kind: 'CaptureAppendSyntax', event: 'Added', mappings: [{ kind: 'PropertyMappingSyntax', property: 'amount', source: nested, location }], location }], location };
        expect(() => toSyntaxJson(deep)).toThrow();
    });
});
