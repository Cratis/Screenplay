// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { NavigationItemSyntax, OutletFilterSyntax, OutletGroupSyntax, OutletStatSyntax, OutletTabSyntax } from '../Syntax/Outlets';

// Every malformed outlet item reports the same code the C# compiler does, and the well formed parts survive.
describe('when parsing outlet items that are wrong', () => {
    const under = (...lines: string[]) => parse(['module Shop', '  outlet o extends', ...lines.map(line => `    ${line}`)].join('\n'));
    const codes = (...lines: string[]) => under(...lines).diagnostics.map(diagnostic => diagnostic.code);
    const items = (...lines: string[]) => under(...lines).value.modules[0].outlets![0].items;

    it('should refuse an unquoted group title', () => codes('group g title More').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should refuse anything but buttons under a group', () => codes('group g title "G"', '  frobnicate').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should read a group without an icon', () => ((items('group g title "G"', '  button b title "B"')[0] as OutletGroupSyntax).icon === undefined).should.equal(true));
    it('should read a quoted group icon', () => (items('group g title "G" icon "a b"')[0] as OutletGroupSyntax).icon!.should.equal('a b'));
    it('should read a localized group title', () => (items('group g title $strings.more')[0] as OutletGroupSyntax).title.should.equal('$strings.more'));
    it('should refuse a second position under a group', () => codes('group g title "G"', '  before a', '  after b').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should read where a group stands', () => (items('group g title "G"', '  after a')[0] as OutletGroupSyntax).position!.placement.should.equal('After'));
    it('should read a tab without a screen', () => ((items('tab t title "T"')[0] as OutletTabSyntax).shows === undefined).should.equal(true));
    it('should refuse a tab binding that is not shows', () => codes('tab t title "T" sets Q.p').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should refuse a stat binding that is not value from', () => codes('stat s title "S" shows Screen').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should read a stat without a binding', () => ((items('stat s title "S"')[0] as OutletStatSyntax).query === undefined).should.equal(true));
    it('should read a filter without a binding', () => ((items('filter f title "F"')[0] as OutletFilterSyntax).query === undefined).should.equal(true));
    it('should refuse a block under a filter', () => codes('filter f title "F"', '  frobnicate').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should refuse a second position under a filter', () => codes('filter f title "F"', '  before a', '  after b').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should read where a stat stands', () => (items('stat s title "S"', '  before a')[0] as OutletStatSyntax).position!.id.should.equal('a'));
    it('should read where a tab stands', () => (items('tab t title "T"', '  after a')[0] as OutletTabSyntax).position!.id.should.equal('a'));
    it('should refuse a hide with no id', () => codes('hide').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should refuse a block under a hide', () => codes('hide a', '  frobnicate').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should refuse an outlet line with an unknown mode', () => parse(['module Shop', '  outlet o replaces'].join('\n')).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.InvalidOutletDeclaration]));
    it('should refuse a second position under a button', () => codes('button b title "B"', '  before a', '  after b').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should refuse a repeated runs', () => codes('button b title "B" runs close runs submit').should.deep.equal([DiagnosticCodes.InvalidToolbarButton]));
});

describe('when parsing navigation items in an outlet', () => {
    const under = (...lines: string[]) => parse(['module Shop', '  outlet sidebar extends', ...lines.map(line => `    ${line}`)].join('\n'));
    const codes = (...lines: string[]) => under(...lines).diagnostics.map(diagnostic => diagnostic.code);
    const item = (...lines: string[]) => under(...lines).value.modules[0].outlets![0].items[0] as NavigationItemSyntax;

    it('should read every part', () => {
        const read = item('item "nav:a" title $strings.a icon "an icon" target Shop.Orders slot side');
        `${read.id}|${read.title}|${read.icon}|${read.target!.module}.${read.target!.feature}|${read.slot}`.should.equal('nav:a|$strings.a|an icon|Shop.Orders|side');
    });
    it('should read a module target', () => (item('item a title "A" target Shop').target!.feature === null).should.equal(true));
    it('should read nested items', () => item('item a title "A"', '  item b title "B" target Shop').children!.map(child => child.id).should.deep.equal(['b']));
    it('should read where it stands', () => item('item a title "A" target Shop', '  before home').position!.placement.should.equal('Before'));
    it('should refuse a second position', () => codes('item a title "A" target Shop', '  before x', '  after y').should.deep.equal([DiagnosticCodes.InvalidOutletItem]));
    it('should refuse anything else under it', () => codes('item a title "A" target Shop', '  frobnicate').should.deep.equal([DiagnosticCodes.InvalidNavigationItem]));
    it('should refuse an item without an id', () => codes('item').should.deep.equal([DiagnosticCodes.InvalidNavigationItem]));
    it('should refuse an unknown part', () => codes('item a title "A" color red').should.deep.equal([DiagnosticCodes.InvalidNavigationItem]));
    it('should refuse a repeated part', () => codes('item a title "A" title "B"').should.deep.equal([DiagnosticCodes.InvalidNavigationItem]));
    it('should refuse an unquoted title', () => codes('item a title A').should.deep.equal([DiagnosticCodes.InvalidNavigationItem]));
    it('should refuse a quoted slot', () => codes('item a title "A" slot "side"').should.deep.equal([DiagnosticCodes.InvalidNavigationItem]));
    it('should refuse a quoted target', () => codes('item a title "A" target "Shop"').should.deep.equal([DiagnosticCodes.InvalidNavigationItem]));
    it('should refuse an item without a title', () => codes('item a target Shop').should.deep.equal([DiagnosticCodes.InvalidNavigationItem]));
    it('should keep a module region apart from a slot named navigation', () => parse(['module Shop', '  navigation', '  feature F'].join('\n')).diagnostics.should.deep.equal([]));
});
