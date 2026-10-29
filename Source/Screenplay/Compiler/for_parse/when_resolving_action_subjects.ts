// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { resolveActionSubjects } from '../ScreenActionSubjects';
import { ScreenActionSyntax, ScreenGuardedActionSyntax } from '../Syntax/Screens';
import { ApplicationSyntax } from '../Syntax/Structure';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const source = 'concept ItemId : Uuid\nconcept TenantId : Uuid\nmodule Work\n  feature Items\n    slice StateView List\n      readmodel Row\n        rowId ItemId key\n        itemId TenantId\n        ready Bool\n      query Rows => Row[]\n      command Retry\n        itemId ItemId identifier\n      screen List\n        data Row[] via query Rows\n        action Retry';
const screen = (application: ApplicationSyntax) => application.modules[0].features[0].slices[0].screens[0];
const action = (application: ApplicationSyntax) => screen(application).directives[1] as ScreenActionSyntax;

describe('when resolving action subjects', () => {
    for (const [clause, expected] of [['', 'Item'], [' for item', 'Item'], [' for screen', 'Screen']]) {
        it(`should resolve '${clause}' without modifying the authored tree`, () => {
            const raw = parse(source + clause).value;
            const before = JSON.stringify(toSyntaxJson(raw));
            const effective = resolveActionSubjects(raw).value;
            action(effective).subject!.should.equal(expected);
            JSON.stringify(toSyntaxJson(raw)).should.equal(before);
            (action(raw).inputs ?? []).should.deep.equal([]);
            action(effective).location.should.deep.equal(action(raw).location);
        });
    }
    it('should keep same-name precedence before key fallback', () => {
        action(resolveActionSubjects(parse(source.replace('itemId TenantId', 'itemId ItemId')).value).value).inputs![0].binding.should.equal('item.itemId');
    });
    it('should bind a renamed key by nominal concept', () => {
        const resolved = resolveActionSubjects(parse(source.replace('        itemId TenantId\n', '')).value);
        action(resolved.value).inputs![0].binding.should.equal('item.rowId');
        resolved.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0819').should.equal(true);
    });
    it('should retain an explicit mapping', () => {
        const effective = resolveActionSubjects(parse(source + ' for item\n          with itemId from $context.currentItem').value).value;
        action(effective).inputs![0].binding.should.equal('$context.currentItem');
    });
    for (const replacement of ['ItemId optional identifier', 'ItemId generated identifier', 'TenantId identifier']) {
        it(`should not infer an item from ${replacement}`, () => {
            (action(resolveActionSubjects(parse(source.replace('ItemId identifier', replacement)).value).value).subject ?? 'Screen').should.equal('Screen');
        });
    }
    it('should not infer a row from two identifier inputs in an assembled snapshot', () => {
        const raw = parse(source).value;
        const module = raw.modules[0];
        const feature = module.features[0];
        const slice = feature.slices[0];
        const command = slice.commands[0];
        const ambiguous = { ...command, properties: [...command.properties, { ...command.properties[0], name: 'otherId' }] };
        const snapshot = { ...raw, modules: [{ ...module, features: [{ ...feature, slices: [{ ...slice, commands: [ambiguous] }] }] }] };
        (action(resolveActionSubjects(snapshot).value).subject ?? 'Screen').should.equal('Screen');
    });
    it('should not use primitive equality as nominal proof', () => {
        const primitive = source.replace('rowId ItemId key', 'rowId Uuid key').replace('itemId ItemId identifier', 'itemId Uuid identifier');
        (action(resolveActionSubjects(parse(primitive).value).value).subject ?? 'Screen').should.equal('Screen');
    });
    it('should leave ambiguous key input unresolved', () => {
        const ambiguous = source.replace('        itemId TenantId\n', '        otherId ItemId key\n') + ' for item';
        const resolved = resolveActionSubjects(parse(ambiguous).value);
        action(resolved.value).inputs!.should.deep.equal([]);
        resolved.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0820' && diagnostic.severity === 'warning').should.equal(true);
    });
    it('should retain a guarded screen collection without selecting an item', () => {
        const guarded = source.replace('        action Retry', '        action "Retry" for screen\n          when item.ready == true execute Retry');
        const resolved = resolveActionSubjects(parse(guarded).value);
        (screen(resolved.value).directives[1] as ScreenGuardedActionSyntax).alternatives[0].arguments.should.deep.equal([]);
        resolved.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0821' && diagnostic.severity === 'warning').should.equal(true);
    });
});
