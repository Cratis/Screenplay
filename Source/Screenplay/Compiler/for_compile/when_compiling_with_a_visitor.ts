// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { compile, CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ApplicationSyntaxVisitor } from '../Syntax/Visitors';

class ModuleNames implements ApplicationSyntaxVisitor<string[]> {
    visit(syntax: ApplicationSyntax): string[] {
        return syntax.modules.map(module => module.name);
    }
}

describe('when compiling with a visitor', () => {
    let result: CompilationResult<string[]>;

    beforeEach(() => {
        result = compile('module Projects\nmodul Typo\nmodule Billing', new ModuleNames());
    });

    it('should return what the visitor produced', () => {
        result.value.should.deep.equal(['Projects', 'Billing']);
    });

    it('should carry the diagnostics', () => {
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0001']);
    });

    it('should not succeed when there are errors', () => {
        result.success.should.be.false;
    });
});
