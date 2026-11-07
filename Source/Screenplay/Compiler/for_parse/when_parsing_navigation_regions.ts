// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';

// Screenplay 6 writes navigation regions as outlets. A module's Screenplay 5 'navigation sidebar|topbar' is an error
// naming the migration; the layout stays opaque here, so the C# compiler alone reports the layout's region.
describe('when parsing navigation regions', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parse([
            'layout Shell',
            '  content',
            '  navigation sidebar',
            '    item sales title "Sales" target Sales',
            '',
            'module Sales',
            '  navigation sidebar extends',
            '    item customers title "Customers" target Sales.Customers',
            '  navigation topbar',
            '    item orders title "Orders" target Sales.Orders',
            '  feature Orders',
            '    slice StateView OrderList',
            '  feature Customers',
        ].join('\n'));
    });

    it('should report each module region as removed', () => {
        result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal([`${DiagnosticCodes.RemovedNavigationRegion}@7`, `${DiagnosticCodes.RemovedNavigationRegion}@9`]);
    });

    it('should name the outlet that replaces the region', () => {
        result.diagnostics[0].message.should.contain("'outlet sidebar extends'");
    });

    it('should name the migration', () => {
        result.diagnostics[1].message.should.contain('screenplay migrate --from 5 --to 6');
    });

    it('should keep the module and its features', () => {
        result.value.modules.map(module => `${module.name}:${module.features.map(feature => feature.name).join(',')}`).should.deep.equal(['Sales:Orders,Customers']);
    });
});
