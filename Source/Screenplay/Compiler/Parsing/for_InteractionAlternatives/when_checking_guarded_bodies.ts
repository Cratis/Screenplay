// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseForAuthoring } from '../../ScreenplayCompiler';

const source = (body: string) => `module M\n  feature F\n    slice StateView S\n      screen V\n        component App.Table entries\n          on click\n${body}`;

describe('when checking guarded interaction bodies', () => {
    it('retains execute inputs but not arguments owned by choice boundaries', () => {
        const result = parseForAuthoring(source('            when item.status == "open"\n              execute Save\n                with id from item.id\n            otherwise\n              with leaked from item.id\n              notify info "Closed"'));
        expect(result.inputUses.map(use => use.property)).toEqual(['id']);
    });
    it.each([
        ['item.status == "open"', 'warning'],
        ['$context.value == true', 'information'],
    ])('reports the right severity for where %s', (condition, severity) => {
        const diagnostic = parse(source(`            where ${condition}\n            notify info "Done"`)).diagnostics.find(diagnostic => diagnostic.code === 'PLAY0564');
        expect(diagnostic?.severity).toBe(severity);
    });
    it('accepts structured lists without deprecation', () => {
        expect(parse(source('            when item.status == "open"\n              notify info "Open"\n            otherwise\n              notify info "Closed"')).diagnostics).toEqual([]);
    });
});
