// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { NavigationTargetSyntax } from '../Outlets';
import { ScreenplaySyntaxWalker } from '../ScreenplaySyntaxWalker';
import { SyntaxNode } from '../SyntaxNode';

class Targets extends ScreenplaySyntaxWalker {
    readonly targets: NavigationTargetSyntax[] = [];
    override visitNode(node: SyntaxNode): void {
        if (node.kind === 'NavigationTargetSyntax') this.targets.push(node as NavigationTargetSyntax);
    }
}

describe('when walking a navigation screen target', () => {
    let walker: Targets;
    beforeEach(() => {
        walker = new Targets();
        walker.visitApplication(parse(`module Shop
  outlet sidebar extends
    item projects title "Projects" target Shop.Projects screen Projects
  feature Projects
    slice StateView Listing
      screen Projects`).value);
    });
    it('should visit the target once without changing the selected screen', () => walker.targets.map(target => target.screen).should.deep.equal(['Projects']));
});
