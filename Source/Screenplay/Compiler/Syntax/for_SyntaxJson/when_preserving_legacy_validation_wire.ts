// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toCompleteSyntaxJson, toSyntaxJson } from '../SyntaxJson';

// Frozen from origin/main 7e161627, not regenerated from the implementation under test.
const cases = JSON.parse(readFileSync(new URL('./given/legacy-validation-wire.json', import.meta.url), 'utf8')) as { name: string; source: string; wire: string }[];

describe('when preserving Legacy validation wire', () => {
    it.each(cases)('should preserve main bytes for $name', ({ source, wire }) => {
        JSON.stringify(toSyntaxJson(parse(source).value)).should.equal(wire);
    });

    it('should retain wrapped inline payload and hints on Legacy wire', () => {
        const source = cases.find(test => test.name === 'named rule inline code')!.source.replace('            ```csharp', '            implementation\n              hint "Preserve predicate"\n              ```csharp').replace('            return true;', '              return true;').replace('            ```\n', '              ```\n');
        const wire = JSON.stringify(toSyntaxJson(parse(source).value));
        wire.should.contain('"kind":"ImplementationSyntax"');
        wire.should.contain('Preserve predicate');
        wire.should.contain('"code":"return true;"');
        wire.should.contain('"file":null');
    });

    it.each(cases.filter(test => test.name.startsWith('named rule')))('should retain unwrapped payloads internally for $name', ({ source, name }) => {
        const syntax = parse(source).value;
        const validation = syntax.modules[0].features[0].slices[0].commands[0].validations[0];
        if (validation.kind !== 'DeclarativeValidateSyntax') throw new Error('Expected declarative validation');
        const rule = validation.rules[0];
        if (name === 'named rule file') rule.file!.path.should.equal('Rules/Check.cs');
        else rule.code!.code.should.equal('return true;');
        JSON.stringify(toCompleteSyntaxJson(syntax)).should.contain(name === 'named rule file' ? 'Rules/Check.cs' : 'return true;');
    });
});
