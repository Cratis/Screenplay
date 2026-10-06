// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../SyntaxJson';

const wire = (source: string): string => JSON.stringify(toSyntaxJson(parse(source).value));

// The wire form of an unmarked document is frozen at what it was before exact numeric source existed.
describe('when writing unmarked source', () => {
    it('should keep the wire bytes of an empty document', () => {
        wire('').should.equal('{"kind":"ApplicationSyntax","concepts":[],"domain":null,"eventSources":[],"fileImports":[],"imports":[],"modules":[],"personas":[],"systems":[],"types":[]}');
    });

    it('should keep the wire bytes of a concept without validations', () => {
        wire('concept Amount : Decimal\n').should.equal('{"kind":"ApplicationSyntax","concepts":[{"kind":"ConceptSyntax","attributes":[],"name":"Amount","type":"Decimal","values":[]}],"domain":null,"eventSources":[],"fileImports":[],"imports":[],"modules":[],"personas":[],"systems":[],"types":[]}');
    });

    it('should leave out every member exact numbers introduced', () => {
        const source = [
            'concept Amount : Decimal', '    validate', '        greater than 0',
            'policy P', '    role "admin"',
            'seed', '    for "a"', '        E', '            x = 1'
        ].join('\n') + '\n';
        const members = new Set<string>();
        const collect = (value: unknown): void => {
            if (Array.isArray(value)) value.forEach(collect);
            else if (typeof value === 'object' && value !== null) for (const [name, member] of Object.entries(value)) { members.add(name); collect(member); }
        };
        collect(toSyntaxJson(parse(source).value));
        for (const member of ['policies', 'seeds', 'validations', 'requirements', 'sourceOptions']) members.has(member).should.equal(false, member);
    });

    it('should keep the wire bytes of a specification without a caller', () => {
        const source = 'module M\n  feature F\n    slice StateChange S\n      specification T\n        when C\n';
        wire(source).should.equal('{"kind":"ApplicationSyntax","concepts":[],"domain":null,"eventSources":[],"fileImports":[],"imports":[],"modules":[{"kind":"ModuleSyntax","authorize":null,"description":null,"features":[{"kind":"FeatureSyntax","authorize":null,"description":null,"features":[],"fileImports":[],"isPlacement":false,"name":"F","slices":[{"kind":"SliceSyntax","captures":[],"commands":[],"constraints":[],"description":null,"events":[],"name":"S","operations":[],"projections":[],"queries":[],"reactions":[],"readModels":[],"screens":[],"specifications":[{"kind":"SpecificationSyntax","given":[],"givenCaptures":[],"givenClock":null,"givenOperationFailures":[],"givenReadModels":[],"name":"T","thenCompensated":[],"thenDenied":null,"thenErrors":[],"thenEvents":[],"thenEventsInAnyOrder":false,"thenNoResult":null,"thenOperations":[],"thenReadModels":[],"thenResults":[],"thenReturns":null,"when":{"kind":"SpecificationCommandSyntax","commandType":"C","for":null,"generatedValues":[],"values":[]},"whenAppended":null,"whenCapture":null,"whenClock":null,"whenQuery":null,"whenTrigger":null}],"type":"StateChange"}]}],"fileImports":[],"isPlacement":false,"name":"M"}],"personas":[],"systems":[],"types":[]}');
    });

    it('should leave out an absent caller but keep a stated one', () => {
        const source = 'module M\n  feature F\n    slice StateChange S\n      specification A\n        when C\n      specification B\n        given caller\n          authenticated\n        when C\n';
        const text = wire(source);
        text.split('"givenCaller"').length.should.equal(2);
        text.should.contain('"givenCaller":{"kind":"SpecificationCallerSyntax"');
    });
});
