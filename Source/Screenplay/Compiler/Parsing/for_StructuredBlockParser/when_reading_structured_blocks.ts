// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ExpressionSyntax } from '../../Syntax/Expressions';

const head = 'module M\n  feature F\n    slice StateChange S\n      specification T\n        when C\n';
const read = (...lines: string[]) => parse(head + lines.map(line => `          ${line}`).join('\n') + '\n');
const codes = (...lines: string[]) => read(...lines).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);
const valueOf = (...lines: string[]): ExpressionSyntax => read(...lines).value.modules[0].features[0].slices[0].specifications[0].when!.values[0].source;

describe('when reading structured blocks', () => {
    it('should read an object with quoted and escaped member names', () => {
        const value = valueOf('detail', '  "first name" = "Ada"', '  @item = 1');
        expect(value).toMatchObject({ kind: 'ObjectExpressionSyntax', members: [{ name: 'first name' }, { name: 'item' }] });
    });
    it('should read nested lists, empty nested lists and empty objects', () => {
        const value = valueOf('grid', '  item', '    item 1', '  item none', '  item empty');
        expect(value).toMatchObject({ kind: 'ListExpressionSyntax', items: [{ kind: 'ListExpressionSyntax' }, { kind: 'ListExpressionSyntax', items: [] }, { kind: 'ObjectExpressionSyntax', members: [] }] });
    });
    it('should read an empty list', () => expect(valueOf('tags none')).toMatchObject({ kind: 'ListExpressionSyntax', items: [] }));
    it('should refuse a line under an empty list', () => expect(codes('tags none', '  item 1')).toEqual(['PLAY0291@7']));
    it('should read an object without members', () => expect(valueOf('detail empty')).toMatchObject({ kind: 'ObjectExpressionSyntax', members: [] }));
    it('should refuse a line under an object without members', () => expect(codes('detail empty', '  name = 1')).toEqual(['PLAY0291@7']));
    it('should refuse a nested member with nothing under it', () => expect(codes('detail', '  inner')).toEqual(['PLAY0291@7']));
    it('should refuse a bare item with nothing under it', () => expect(codes('tags', '  item')).toEqual(['PLAY0291@7']));
    it('should refuse a member line in a list', () => expect(codes('tags', '  item 1', '  name = 2')).toEqual(['PLAY0291@8']));
    it('should refuse an item line in an object', () => expect(codes('detail', '  name = 2', '  item 1')).toEqual(['PLAY0291@8']));
    it('should refuse a malformed member', () => expect(codes('detail', '  name : 2')).toEqual(['PLAY0291@7']));
    it('should refuse lines under a member with a value', () => expect(codes('detail', '  name = 2', '    more = 3')).toEqual(['PLAY0291@8']));
    it('should refuse lines under an item with a value', () => expect(codes('tags', '  item 1', '    more = 3')).toEqual(['PLAY0291@8']));
    it('should refuse a value that is not a scalar', () => expect(codes('detail', '  name = other')).toEqual(['PLAY0291@7']));
    it('should refuse a case reference inside a block', () => expect(codes('detail', '  name = case.amount')).toEqual(['PLAY0584@7']));
    it('should refuse inline json inside a block', () => expect(codes('detail', '  name = [1]')).toEqual(['PLAY0833@7']));
    it('should refuse a repeated member', () => expect(codes('detail', '  name = 1', '  name = 2')).toEqual(['PLAY0294@8']));
    it('should read a structured return opened with value', () => {
        const result = parse(head + '        then returns value\n          id = 1\n');
        expect(result.value.modules[0].features[0].slices[0].specifications[0].thenReturns).toMatchObject({ kind: 'ScalarSpecificationReturnSyntax', value: { kind: 'ObjectExpressionSyntax' } });
    });
    it('should refuse a structured return with nothing under it', () => expect(parse(head + '        then returns value\n').diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0291']));
    it('should read an empty structured return', () => expect(parse(head + '        then returns value none\n').diagnostics).toEqual([]));
    it('should read a structured generated fixture', () => expect(parse(head + '          generated receipt\n            item 1\n').diagnostics).toEqual([]));
    it('should read a pinned generated block', () => expect(parse(head.replace('        when C\n', '        given generated C.receipt none\n        when C\n')).diagnostics).toEqual([]));
});
