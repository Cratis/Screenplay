// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import * as compiler from '../../ScreenplayCompiler';
import { ChildrenSyntax, FromSyntax } from '../../Syntax/Projections';

const source = [
    'module M',
    '  feature F',
    '    slice StateView C',
    '      projection P => V',
    '        children entries identified by entryId',
    '          from E',
    '            insert = title',
    '            sort = rank',
    '            once = title',
    '            set @once = title',
    '            set insert to title',
].join('\n');

// Ordering lines and 'set once' are keyword lines; a mapping that targets a property named insert, sort or once stays a plain mapping.
describe('when reading mappings named like keywords', () => {
    const result = compiler.parse(source);
    const children = result.value!.modules[0].features[0].slices[0].projections[0].blocks[0] as ChildrenSyntax;
    const mappings = (children.blocks[0] as FromSyntax).mappings;

    it('should report nothing', () => expect(result.diagnostics).toEqual([]));
    it('should declare no ordering', () => expect(children.ordering ?? null).toBeNull());
    it('should keep every line a plain mapping', () => expect(mappings.map(mapping => mapping.kind)).toEqual(Array(5).fill('SetMappingSyntax')));
    it('should target the named properties', () => expect(mappings.map(mapping => mapping.property)).toEqual(['insert', 'sort', 'once', 'once', 'insert']));
});
