// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { parseSpecificationSource } from '../ScreenplayCompiler';

describe('when parsing specification tag expectations', () => {
    let result: ReturnType<typeof parseSpecificationSource>;
    beforeEach(() => {
        result = parseSpecificationSource(`specification Tagged
  when append E
  then E
    tag audit
    tag "audit"
    tag billing
  then F
    no tags`);
    });
    it('should accept the expectations', () => result.diagnostics.should.deep.equal([]));
    it('should keep tags in order with duplicates', () => result.value[0].thenEvents[0].tags!.map(tag => (tag.value as { value: string }).value).should.deep.equal(['audit', 'audit', 'billing']));
    it('should read the no tags marker', () => result.value[0].thenEvents[1].noTags!.kind.should.equal('SpecificationNoTagsSyntax'));
});

describe('when parsing misplaced and conflicting tag expectations', () => {
    const codes = (source: string): string[] => parseSpecificationSource(source).diagnostics.map(diagnostic => diagnostic.code);
    it('should refuse a tag on a given event', () => codes('specification S\n  given E\n    tag audit').should.include('PLAY1144'));
    it('should refuse no tags combined with a tag', () => codes('specification S\n  then E\n    tag audit\n    no tags').should.include('PLAY1144'));
    it('should refuse a repeated no tags', () => codes('specification S\n  then E\n    no tags\n    no tags').should.include('PLAY1144'));
    it('should refuse a context expression tag', () => codes('specification S\n  then E\n    tag $context.occurred').should.include('PLAY1144'));
    it('should refuse children', () => codes('specification S\n  then E\n    tag audit\n      x = 1').should.include('PLAY1144'));
});
