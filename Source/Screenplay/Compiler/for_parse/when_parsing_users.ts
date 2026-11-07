// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing users', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'persona Clerk',
            'persona Visitor',
            'user kari "Kari \\"K\\" Nordmann" as Clerk, Visitor',
            'user ola as Clerk',
            'user guest',
        );
    });

    it('should succeed without diagnostics', () => result.diagnostics.length.should.equal(0));
    it('should keep every user in order', () => result.value!.users!.map(user => user.name).should.deep.equal(['kari', 'ola', 'guest']));
    it('should unescape the display name', () => (result.value!.users![0].displayName === 'Kari "K" Nordmann').should.be.true);
    it('should keep the personas a user plays in order', () => result.value!.users![0].personas.should.deep.equal(['Clerk', 'Visitor']));
    it('should leave a missing display name null', () => (result.value!.users![1].displayName === null).should.be.true);
    it('should leave a user with no persona playing none', () => result.value!.users![2].personas.length.should.equal(0));
});
