// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { parse, parseSpecificationSource } from '../ScreenplayCompiler';

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

describe('when refusing tags on typed examples', () => {
    const example = (body: string) => parse('module M\n  feature F\n    slice StateChange S\n      event E\n        @tag String\n      example Tagged : E\n        ' + body);
    it('should refuse tag with PLAY1144 rather than a property diagnostic', () => example('tag audit').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY1144']));
    it('should refuse no tags with PLAY1144 rather than a property diagnostic', () => example('no tags').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY1144']));
    it('should preserve payload mappings named tag', () => example('tag = "payload"').diagnostics.should.deep.equal([]));
});

describe('when parsing misplaced and conflicting tag expectations', () => {
    const codes = (source: string): string[] => parseSpecificationSource(source).diagnostics.map(diagnostic => diagnostic.code);
    it('should refuse a tag on a given event', () => codes('specification S\n  given E\n    tag audit').should.include('PLAY1144'));
    it('should refuse a tag on an appended event', () => codes('specification S\n  when append E\n    tag audit').should.include('PLAY1144'));
    it('should refuse a tag on a redelivered event', () => codes('specification S\n  when redelivered E to R\n    tag audit').should.include('PLAY1144'));
    it('should refuse a numeric tag', () => codes('specification S\n  then E\n    tag 123').should.include('PLAY1144'));
    it('should refuse an empty tag', () => codes('specification S\n  then E\n    tag ""').should.include('PLAY1144'));
    it('should refuse a whitespace tag', () => codes('specification S\n  then E\n    tag " "').should.include('PLAY1144'));
    it('should refuse no tags combined with a tag', () => codes('specification S\n  then E\n    tag audit\n    no tags').should.include('PLAY1144'));
    it('should refuse a repeated no tags', () => codes('specification S\n  then E\n    no tags\n    no tags').should.include('PLAY1144'));
    it('should refuse a context expression tag', () => codes('specification S\n  then E\n    tag $context.occurred').should.include('PLAY1144'));
    it('should refuse children', () => codes('specification S\n  then E\n    tag audit\n      x = 1').should.include('PLAY1144'));
});
