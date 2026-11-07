// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { NavigationItemSyntax, OutletFilterSyntax, OutletGroupSyntax, OutletStatSyntax, OutletTabSyntax } from '../Syntax/Outlets';
import type { ToolbarButtonSyntax } from '../Syntax/Screens';

// A module, feature or slice screen changes an outlet its template declares; the declaring layout and template stay
// opaque to this compiler, as do the checks of what an item names.
describe('when parsing outlets', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parse([
            'module Shop',
            '  outlet sidebar extends',
            '    item orders title "Orders" target Shop.Orders',
            '      after home',
            '  feature Orders',
            '    outlet actions overrides',
            '      hide export',
            '      button add title "Add" slice Add emphasis primary runs submit',
            '        before refresh',
            '      group more title "More" icon dots',
            '        button archive title "Archive" slice Archive',
            '    outlet toolbar inherits',
            '    slice StateView OrderList',
            '      screen OrderList',
            '        outlet filters extends',
            '          filter status title "Status" sets AllOrders.status',
            '        outlet stats extends',
            '          stat total title "Total" value from Totals.count',
            '        outlet tabs extends',
            '          tab details title "Details" shows OrderDetails',
        ].join('\n'));
    });

    const module = () => result.value.modules[0];
    const feature = () => module().features[0];
    const screen = () => feature().slices[0].screens[0];

    it('should report nothing', () => result.diagnostics.should.deep.equal([]));
    it('should read the module outlet', () => module().outlets!.map(outlet => `${outlet.name}:${outlet.mode}`).should.deep.equal(['sidebar:Extend']));
    it('should read the navigation item and where it stands', () => {
        const item = module().outlets![0].items[0] as NavigationItemSyntax;
        `${item.id}:${item.target!.feature}:${item.position!.placement}:${item.position!.id}`.should.equal('orders:Orders:After:home');
    });
    it('should read the feature modes', () => feature().outlets!.map(outlet => outlet.mode).should.deep.equal(['Override', 'Inherit']));
    it('should read the hidden item', () => feature().outlets![0].hides!.map(hide => hide.id).should.deep.equal(['export']));
    it('should read the button with its built-in and position', () => {
        const button = feature().outlets![0].items[0] as ToolbarButtonSyntax;
        `${button.runs}:${button.position!.placement}:${button.position!.id}`.should.equal('submit:Before:refresh');
    });
    it('should read the group and its buttons', () => {
        const group = feature().outlets![0].items[1] as OutletGroupSyntax;
        `${group.icon}:${group.buttons.map(button => button.id).join(',')}`.should.equal('dots:archive');
    });
    it('should read the filter binding', () => {
        const filter = screen().outlets![0].items[0] as OutletFilterSyntax;
        `${filter.query}.${filter.parameter}`.should.equal('AllOrders.status');
    });
    it('should read the stat binding', () => {
        const stat = screen().outlets![1].items[0] as OutletStatSyntax;
        `${stat.query}.${stat.field}`.should.equal('Totals.count');
    });
    it('should read the tab screen', () => (screen().outlets![2].items[0] as OutletTabSyntax).shows!.should.equal('OrderDetails'));
});

describe('when parsing outlets that are wrong', () => {
    const codes = (...lines: string[]) => parse(['module Shop', ...lines].join('\n')).diagnostics.map(diagnostic => diagnostic.code);

    it('should refuse a declaration below a template', () => codes('  outlet actions of actions').should.deep.equal([DiagnosticCodes.InvalidOutletDeclaration]));
    it('should refuse an outlet without a mode', () => codes('  outlet actions').should.deep.equal([DiagnosticCodes.InvalidOutletDeclaration]));
    it('should refuse items under inherits', () => codes('  outlet actions inherits', '    hide export').should.deep.equal([DiagnosticCodes.InvalidOutletDeclaration]));
    it('should refuse an outlet changed twice', () => codes('  outlet actions extends', '  outlet actions overrides').should.deep.equal([DiagnosticCodes.DuplicateOutlet]));
    it('should refuse an unknown line', () => codes('  outlet actions extends', '    frobnicate').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should refuse a filter without a valid binding', () => codes('  outlet filters extends', '    filter status title "Status" sets status').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should refuse an unknown built-in', () => codes('  outlet actions extends', '    button go title "Go" runs fly').should.deep.equal([DiagnosticCodes.InvalidToolbarButton]));
    it('should refuse a logo below the layout', () => codes('  outlet sidebar extends', '    logo image "/x.svg" title "X"').should.deep.equal([DiagnosticCodes.NavigationLogoNotAllowed]));
    it('should refuse a Screenplay 5 module toolbar', () => codes('  toolbar actions', '    button go title "Go"').should.deep.equal([DiagnosticCodes.RemovedFeatureToolbar]));
});
