// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';

// The TypeScript compiler recognizes the navigation regions of a layout and of a module and skips them whole,
// like every other layout and screen composition member. The reference checks (PLAY0740-PLAY0752) are C# only.
describe('when parsing navigation regions', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parse([
            'layout Shell',
            '  content',
            '  navigation sidebar',
            '    logo image "/acme.svg" title "Acme"',
            '    item sales title "Sales" target Sales',
            '      item orders title "Orders" target Sales.Orders',
            '',
            'module Sales',
            '  navigation sidebar extends',
            '    item customers title "Customers" target Sales.Customers',
            '  navigation topbar overrides',
            '    item orders title "Orders" target Sales.Orders',
            '  feature Orders',
            '    slice StateView OrderList',
            '  feature Customers',
        ].join('\n'));
    });

    it('should report nothing', () => {
        result.diagnostics.should.deep.equal([]);
    });

    it('should keep the module and its features', () => {
        result.value.modules.map(module => `${module.name}:${module.features.map(feature => feature.name).join(',')}`).should.deep.equal(['Sales:Orders,Customers']);
    });
});
