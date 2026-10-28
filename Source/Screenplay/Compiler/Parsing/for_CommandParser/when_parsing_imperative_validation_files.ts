// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toCompleteSyntaxJson } from '../../Syntax/SyntaxJson';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../../Syntax/SyntaxNode';

const cases = [
    ['concept Money : Decimal\n  validate\n    file Rules/Money.cs', 'Rules/Money.cs'],
    ['module M\n  feature F\n    slice StateChange S\n      command Pay\n        validate\n          file Rules/Pay.cs', 'Rules/Pay.cs'],
];

describe('when parsing imperative validation files', () => {
    it.each(cases)('should preserve and visit the validation file for %s', (source, path) => {
        const parsed = parse(source);
        expect(parsed.diagnostics).toEqual([]);
        const json = toCompleteSyntaxJson(parsed.value!);
        expect(JSON.stringify(json)).toContain(`"path":"${path}"`);
        const kinds: string[] = [];
        class Walker extends ScreenplaySyntaxWalker { visitNode(node: SyntaxNode): void { kinds.push(node.kind); } }
        new Walker().visitApplication(parsed.value!);
        expect(kinds).toContain('FileReferenceSyntax');
    });
});
