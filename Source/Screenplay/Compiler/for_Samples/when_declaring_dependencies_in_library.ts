// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

describe('when declaring dependencies in library', () => {
    let baseline: CompilationResult<ApplicationSyntax>;
    let declared: CompilationResult<ApplicationSyntax>;
    let stripped: ApplicationSyntax;

    beforeEach(() => {
        const source = readFileSync(resolve(__dirname, '../../../../Samples/Library/library.play'), 'utf8');
        const declaredSource = source.replace('  feature Loans', '  feature Loans\n    depends on Catalog\n    depends on Members');
        baseline = parse(source);
        declared = parse(declaredSource);
        stripped = { ...declared.value, modules: declared.value.modules.map(module => ({ ...module, features: module.features.map(feature => ({ ...feature, dependsOn: [] })) })) };
    });

    it('should parse the added declarations', () => declared.value.modules.flatMap(module => module.features).find(feature => feature.name === 'Loans')!.dependsOn!.map(dependency => dependency.target).should.deep.equal(['Catalog', 'Members']));
    it('should preserve sample syntax modulo the authoring declarations', () => JSON.stringify(toSyntaxJson(stripped)).should.equal(JSON.stringify(toSyntaxJson(baseline.value))));
    it('should add no diagnostics for valid sibling targets', () => declared.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.message]).should.deep.equal(baseline.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.message])));
    it('should omit empty dependency collections', () => JSON.stringify(toSyntaxJson(baseline.value)).should.not.contain('dependsOn'));
    it('should carry authored targets on wire', () => JSON.stringify(toSyntaxJson(declared.value)).should.contain('"kind":"DependsOnSyntax","target":"Catalog"'));
});
