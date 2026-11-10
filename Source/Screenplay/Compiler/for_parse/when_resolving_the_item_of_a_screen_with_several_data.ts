// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { resolveActionSubjects } from '../ScreenActionSubjects';
import { ScreenActionSyntax, ScreenToolbarSyntax } from '../Syntax/Screens';

const head = 'concept ItemId : Uuid\nmodule Work\n  feature Items\n    slice StateView List\n      readmodel Header\n        title String\n      readmodel Row\n        rowId ItemId key\n      readmodel Other\n        otherId ItemId key\n      query HeaderOf => Header\n      query Rows => Row[]\n      query Others => Other[]\n      command Retry\n        itemId ItemId identifier\n      command Tag\n        tags ItemId[]\n      screen List\n';
const resolve = (screen: string) => resolveActionSubjects(parse(head + screen).value);
const directives = (screen: string) => {
    const resolved = resolve(screen);
    return { resolved, directives: resolved.value.modules[0].features[0].slices[0].screens[0].directives };
};
const codes = (screen: string, code: string) => parse(head + screen).diagnostics.filter(diagnostic => diagnostic.code === code);
const unresolved = (resolved: ReturnType<typeof resolve>) => resolved.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0346');

describe('when resolving the item of a screen with several data', () => {
    it('should bind the plain action to the collection a table shows', () => {
        const { resolved, directives: nodes } = directives('        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        action Retry for item');
        const action = nodes.find(node => node.kind === 'ScreenActionSyntax') as ScreenActionSyntax;
        action.subject!.should.equal('Item');
        action.inputs![0].binding.should.equal('item.rowId');
        unresolved(resolved).length.should.equal(0);
    });
    it('should bind regardless of data order', () => {
        const { resolved, directives: nodes } = directives('        data Row[] via query Rows\n        data Header via query HeaderOf\n        table Row\n        action Retry for item');
        (nodes.find(node => node.kind === 'ScreenActionSyntax') as ScreenActionSyntax).inputs![0].binding.should.equal('item.rowId');
        unresolved(resolved).length.should.equal(0);
    });
    it('should bind the toolbar action to the collection a table shows', () => {
        const { resolved, directives: nodes } = directives('        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        toolbar\n          item retry action Retry for item');
        const toolbar = nodes.find(node => node.kind === 'ScreenToolbarSyntax') as ScreenToolbarSyntax;
        toolbar.items[0].parameters[0].binding.path.should.equal('item.rowId');
        unresolved(resolved).length.should.equal(0);
    });
    it('should use a table in a section for an action in the same section', () => {
        unresolved(resolve('        data Header via query HeaderOf\n        data Row[] via query Rows\n        section Rows\n          table Row\n          action Retry for item')).length.should.equal(0);
    });
    it('should name the candidates when two tables show two collections', () => {
        const { resolved, directives: nodes } = directives('        data Row[] via query Rows\n        data Other[] via query Others\n        table Row\n        table Other\n        action Retry for item');
        const [diagnostic] = unresolved(resolved);
        diagnostic.message.should.contain('Row[], Other[]');
        diagnostic.message.should.contain('table over exactly one collection');
        ((nodes.find(node => node.kind === 'ScreenActionSyntax') as ScreenActionSyntax).inputs ?? []).should.deep.equal([]);
    });
    it('should stay unresolved when two data have no table', () => {
        unresolved(resolve('        data Header via query HeaderOf\n        data Row[] via query Rows\n        action Retry for item')).length.should.equal(1);
    });
    it('should not narrow by a table over a record', () => {
        unresolved(resolve('        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Header\n        action Retry for item')).length.should.equal(1);
    });
    it('should keep a single data unchanged', () => {
        const { resolved, directives: nodes } = directives('        data Row[] via query Rows\n        action Retry for item');
        (nodes.find(node => node.kind === 'ScreenActionSyntax') as ScreenActionSyntax).inputs![0].binding.should.equal('item.rowId');
        unresolved(resolved).length.should.equal(0);
    });
    it('should not narrow a guarded action by a table', () => {
        unresolved(resolve('        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        action "Retry" for item\n          when item.rowId == item.rowId execute Retry')).length.should.equal(1);
    });
    it('should report an unknown item field after narrowing to the table collection', () => {
        codes('        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        action Retry for item\n          with itemId from item.missing', 'PLAY0345').length.should.equal(1);
        codes('        data Row[] via query Rows\n        action Retry for item\n          with itemId from item.missing', 'PLAY0345').length.should.equal(1);
    });
    it('should report a collection cardinality mismatch after narrowing like a single data', () => {
        const narrowed = codes('        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        action Tag for item\n          with tags from item.rowId', 'PLAY0348');
        const single = codes('        data Row[] via query Rows\n        action Tag for item\n          with tags from item.rowId', 'PLAY0348');
        single.length.should.equal(1);
        narrowed.map(diagnostic => diagnostic.message).should.deep.equal(single.map(diagnostic => diagnostic.message));
    });
    it('should bind a screen-level table for an action in a section without tables', () => {
        unresolved(resolve('        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        section Tools\n          action Retry for item')).length.should.equal(0);
    });
    it('should not use a table in a sibling section for an action at screen level', () => {
        unresolved(resolve('        data Header via query HeaderOf\n        data Row[] via query Rows\n        section A\n          table Row\n        action Retry for item')).length.should.equal(1);
    });
});
