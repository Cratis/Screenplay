// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { ScreenShowSyntax, ScreenTemplateReferenceSyntax, ScreenToolbarSyntax, ToolbarButtonSyntax } from '../Syntax/Screens';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';

// Cratis/Screenplay#1178: where a slice's screen fits, how a hook opens its slice and what a template slot shows, read the
// way the C# compiler reads them. Whether what a line names exists is the C# compiler's check.
describe('when parsing placement and hooks', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let feature: FeatureSyntax;

    const button = (id: string): ToolbarButtonSyntax =>
        feature.outlets!.flatMap(outlet => outlet.items).find(item => item.kind === 'ToolbarButtonSyntax' && item.id === id) as ToolbarButtonSyntax;

    beforeEach(() => {
        result = parse([
            'module Catalog',
            '  feature Products',
            '    outlet toolbar overrides',
            '      button add title "Add" slice DraftProduct opens dialog',
            '      button price title "Set price" slice SetProductPrice for item opens in slot detail',
            '      button withdraw title "Withdraw" slice WithdrawProduct for item',
            '        runs',
            '        enabled when item.status == "onSale"',
            '        confirm "Withdraw?"',
            '    slice StateChange DraftProduct',
            '      screen DraftProduct',
            '        fits drawer, pane, dialog',
            '    slice StateView ProductCatalog',
            '      screen ProductCatalog',
            '        template ListPage',
            '          detail',
            '            show ProductDetails by sku from products.selection',
            '        toolbar extra',
            '          item export action Export',
            '            enabled when item.status == "draft"',
        ].join('\n'));
        feature = result.value!.modules[0].features[0];
    });

    it('should report no warnings or errors', () => result.diagnostics.filter(diagnostic => diagnostic.severity !== 'information').should.be.empty);
    it('should read where a screen fits in order', () => feature.slices[0].screens[0].fits!.should.deep.equal(['Drawer', 'Pane', 'Dialog']));
    it('should read a hook that opens a dialog', () => button('add').opens!.should.equal('Dialog'));
    it('should read a hook that opens in a slot', () => [button('price').opens, button('price').opensSlot].should.deep.equal(['Slot', 'detail']));
    it('should read what a hook acts on', () => button('price').subject!.should.equal('Item'));
    it('should read a hook that runs', () => button('withdraw').opens!.should.equal('Runs'));
    it('should read when a hook is enabled', () => button('withdraw').enabledWhen!.kind.should.equal('ComparisonConditionSyntax'));
    it('should read the confirmation', () => button('withdraw').confirm!.should.equal('Withdraw?'));
    it('should read what a slot shows', () => {
        const template = feature.slices[1].screens[0].directives[0] as ScreenTemplateReferenceSyntax;
        const show = template.slots[0].directives[0] as ScreenShowSyntax;
        [show.kind, show.screen, show.by, show.element].should.deep.equal(['ScreenShowSyntax', 'ProductDetails', 'sku', 'products']);
    });
    it('should read when a toolbar item is enabled', () => {
        const toolbar = feature.slices[1].screens[0].directives[1] as ScreenToolbarSyntax;
        toolbar.items[0].enabledWhen!.kind.should.equal('ComparisonConditionSyntax');
    });
});

describe('when parsing placement and hooks that are wrong', () => {
    const codes = (...lines: string[]) => parse(['module Catalog', '  feature Products', ...lines].join('\n')).diagnostics.map(diagnostic => diagnostic.code);
    const screen = (...lines: string[]) => codes('    slice StateView Catalog', '      screen Catalog', ...lines);
    const hook = (...lines: string[]) => codes('    outlet toolbar extends', ...lines);

    it('should refuse an unknown place to fit', () => screen('        fits dialog, sideways').should.deep.equal([DiagnosticCodes.InvalidScreenFits]));
    it('should refuse a place fitted twice', () => screen('        fits dialog, dialog').should.deep.equal([DiagnosticCodes.InvalidScreenFits]));
    it('should refuse a second fits line', () => screen('        fits dialog', '        fits page').should.deep.equal([DiagnosticCodes.InvalidScreenFits]));
    it('should refuse a template fits slot on a screen', () => screen('        fits slot main').should.deep.equal([DiagnosticCodes.InvalidScreenFits]));
    it('should refuse a malformed show', () => screen('        show Details from products').should.deep.equal([DiagnosticCodes.InvalidScreenShow]));
    it('should refuse running and opening', () => hook('      button x title "X" slice S opens dialog', '        runs').should.deep.equal([DiagnosticCodes.InvalidToolbarButton]));
    it('should refuse running a built in and the slice', () => hook('      button x title "X" slice S runs refresh', '        runs').should.deep.equal([DiagnosticCodes.InvalidToolbarButton]));
    it('should refuse an unknown opening', () => hook('      button x title "X" slice S opens window').should.deep.equal([DiagnosticCodes.InvalidToolbarButton]));
    it('should refuse an unknown subject', () => hook('      button x title "X" slice S for row').should.deep.equal([DiagnosticCodes.InvalidToolbarButton]));
});
