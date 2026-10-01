// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../ScreenplaySyntaxWalker';
import { SyntaxNode } from '../SyntaxNode';

class KindCollector extends ScreenplaySyntaxWalker {
    readonly kinds: string[] = [];

    override visitNode(node: SyntaxNode): void {
        this.kinds.push(node.kind);
    }
}

describe('when walking a document', () => {
    let collector: KindCollector;

    beforeEach(() => {
        collector = new KindCollector();
        collector.visitApplication(parse([
            'module Projects',
            '  feature Registration',
            '    slice StateChange RegisterProject',
            '      event ProjectRegistered',
            '        name String',
            '      command RegisterProject',
            '        name String',
            '      projection Summary => ProjectSummary',
            '        from ProjectRegistered',
        ].join('\n')).value);
    });

    it('should visit every node, commands before events as the C# walker does', () => {
        collector.kinds.should.deep.equal([
            'ApplicationSyntax', 'ModuleSyntax', 'FeatureSyntax', 'SliceSyntax',
            'CommandSyntax', 'PropertySyntax', 'TypeRefSyntax',
            'EventSyntax', 'PropertySyntax', 'TypeRefSyntax',
            'ProjectionSyntax', 'FromSyntax', 'EventSpecSyntax',
        ]);
    });
});
