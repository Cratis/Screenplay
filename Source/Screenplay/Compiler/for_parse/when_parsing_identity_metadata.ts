// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse, parseForAuthoring } from '../ScreenplayCompiler';
import { mergeDocuments } from '../Files/PlayFolderMerge';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';

const declaration = ['identity', '  department String optional from claim "department"', '  partner Bool', '    ```csharp', '    return context.Identity.HasRole("Partner");', '    ```', '  external String', '    file Identity/External.cs'].join('\n');

describe('when parsing identity metadata', () => {
    it('should retain claim code and file sources', () => {
        const result = parse(declaration);
        result.diagnostics.should.deep.equal([]);
        result.value.identity!.details.map(detail => detail.source.kind).should.deep.equal(['ClaimIdentitySourceSyntax', 'CodeIdentitySourceSyntax', 'FileIdentitySourceSyntax']);
    });

    it('should keep detail paths known after declaration', () => {
        parse('policy P\n  require claim "x" matches $identity.department\n' + declaration).diagnostics.should.deep.equal([]);
    });

    it('should preserve legacy code fences and warn', () => {
        const result = parse('identity\n  partner Bool\n    csharp\n      ```\n      return true;\n      ```');
        result.value.identity!.details[0].source.kind.should.equal('CodeIdentitySourceSyntax');
        result.diagnostics.map(diagnostic => diagnostic.code).should.contain('PLAY0397');
    });

    it.each([
        ['identity\n  value String from claim unquoted', 'PLAY0635'],
        ['identity\n  value String\n    cache forever', 'PLAY0635'],
        ['identity\n  value String\n    file first.cs\n    file second.cs', 'PLAY0635'],
        ['identity\n  value String\n    file first.cs\n      cache forever', 'PLAY0635'],
        ['identity\n  value String from query Q by $context.tenant', 'PLAY0644'],
        ['identity\n  value String from query Q by value', 'PLAY0644'],
    ])('should reject invalid detail source %s', (source, code) => {
        parse(source).diagnostics.map(diagnostic => diagnostic.code).should.contain(code);
    });

    it('should resolve details across physical files only after assembly', () => {
        const policy = parseForAuthoring('policy P\n  require claim "x" matches $identity.department', 'policy.play', [], false);
        const metadata = parseForAuthoring(declaration, 'application.play', [], false);
        mergeDocuments([policy, metadata]).diagnostics.should.deep.equal([]);
    });

    it('should reject repeated folder blocks', () => {
        const result = mergeDocuments([parseForAuthoring(declaration, 'first.play', [], false), parseForAuthoring(declaration, 'second.play', [], false)]);
        result.diagnostics.map(diagnostic => diagnostic.code).should.contain('PLAY0172');
    });

    it('should walk every source child', () => {
        const visited: string[] = [];
        class Walker extends ScreenplaySyntaxWalker {
            override visitNode(node: SyntaxNode): void { visited.push(node.kind); }
        }
        new Walker().visitApplication(parse(declaration).value);
        visited.should.include.members(['IdentitySyntax', 'IdentityDetailSyntax', 'ClaimIdentitySourceSyntax', 'CodeIdentitySourceSyntax', 'CodeBlockSyntax', 'FileIdentitySourceSyntax', 'FileReferenceSyntax', 'TypeRefSyntax']);
    });
});
