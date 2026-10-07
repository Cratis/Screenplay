// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

describe('when declaring dependencies in library', () => {
    const syntax = parse(readFileSync(resolve(__dirname, '../../../../Samples/Library/library.play'), 'utf8')).value;
    const changed = { ...syntax, modules: syntax.modules.map(module => ({ ...module, features: module.features.map(feature => ({ ...feature, dependsOn: feature.name === 'Loans' ? [{ kind: 'DependsOnSyntax' as const, target: 'Catalog', location: feature.location }] : [] })) })) };
    const stripped = { ...changed, modules: changed.modules.map(module => ({ ...module, features: module.features.map(feature => ({ ...feature, dependsOn: [] })) })) };
    it('should preserve sample syntax modulo the authoring declarations', () => JSON.stringify(toSyntaxJson(stripped)).should.equal(JSON.stringify(toSyntaxJson(syntax))));
    it('should omit empty dependency collections', () => JSON.stringify(toSyntaxJson(syntax)).should.not.contain('dependsOn'));
    it('should carry authored targets on wire', () => JSON.stringify(toSyntaxJson(changed)).should.contain('"kind":"DependsOnSyntax","target":"Catalog"'));
});
