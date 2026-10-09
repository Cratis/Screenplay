// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../../Syntax/SyntaxNode';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';

describe('when describing additional declarations', () => {
    it.each([
        ['concept Value : String\n  description "Intent"', 'ConceptSyntax'],
        ['policy Access\n  description "Intent"\n  require authenticated', 'PolicySyntax'],
        ['module M\n  feature F\n    slice StateView S\n      screen V\n        description "Intent"', 'ScreenSyntax'],
        ['module M\n  feature F\n    slice StateView S\n      event E\n      projection P\n        description "Intent"\n        from E', 'ProjectionSyntax'],
        ['module M\n  feature F\n    slice StateChange S\n      event E\n      constraint C\n        description "Intent"\n        unique event E', 'UniqueEventConstraintSyntax'],
    ])('should retain descriptions on %s', (source, kind) => {
        const result = parse(source);
        result.diagnostics.should.be.empty;
        class Nodes extends ScreenplaySyntaxWalker {
            readonly nodes: SyntaxNode[] = [];
            override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
        }
        const visitor = new Nodes();
        visitor.visitApplication(result.value);
        (visitor.nodes.find(node => node.kind === kind) as unknown as { description: string }).description.should.equal('Intent');
    });
    it('should retain a bare enum description value', () => {
        const result = parse('concept Status : Enum\n  description\n  description "States"');
        result.diagnostics.should.be.empty;
        result.value.concepts[0].values.should.deep.equal(['description']);
        result.value.concepts[0].description!.should.equal('States');
    });
    it('should validate form descriptions without swallowing field inputs', () => {
        const result = parse('module M\n  form F for C\n    description "One"\n    description "Two"\n    field value');
        result.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.DuplicateDescription).should.be.true;
    });
});
