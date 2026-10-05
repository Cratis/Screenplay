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
});
