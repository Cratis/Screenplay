// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse, CompilationResult } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';

const source = `concept LinkId : String
module Demo
  feature Links
    slice StateChange ReviseLink
      command ReviseLink
        linkId LinkId identifier
        supersedesLinkId LinkId
        produces event LinkRevised
          linkId LinkId = linkId
          supersedesLinkId LinkId = supersedesLinkId
    slice StateView Links
      readmodel LinksView
        linkId LinkId
        supersededByLinkId LinkId optional
      query GetLink => LinksView optional
        by linkId LinkId
      projection Links => LinksView
        no automap
        from LinkRevised key linkId
          linkId = linkId
          clear supersededByLinkId
        from LinkRevised key supersedesLinkId
          linkId = supersedesLinkId
          supersededByLinkId = linkId`;

describe('when compiling duplicate projection events with different keys', () => {
    let result: CompilationResult<ApplicationSyntax>;
    beforeEach(() => { result = parse(source); });

    it('should fail ordinary compilation', () => { result.success.should.be.false; });
    it('should report the second from line and column', () => {
        const duplicates = result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.DuplicateEventInProjectionScope);
        duplicates.should.have.lengthOf(1);
        duplicates[0].location.line.should.equal(22);
        duplicates[0].location.column.should.equal(9);
        duplicates[0].message.should.equal("Event 'LinkRevised' is used more than once in projection 'Links' at the root level (Chronicle allows one use of an event type per projection level)");
    });
});
