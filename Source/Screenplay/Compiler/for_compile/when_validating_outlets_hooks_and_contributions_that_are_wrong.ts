// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';

const layout = [
    'layout Shell',
    '  category application',
    '  main page routed primary',
    '  detail pane',
    '  outlet actions of actions for item in main',
    '  outlet filters of filters',
    '  outlet tabs of tabs',
    '  outlet stats of stats',
    '  outlet nav of navigation in main'
];

const slices = [
    '    slice StateChange Add',
    '      command Add',
    '        name String',
    '    slice StateChange Remove',
    '      command Remove',
    '        id String identifier',
    '    slice StateView Listing',
    '      readmodel Row',
    '        name String',
    '      readmodel Count',
    '        total Int',
    '      query Records => Row[]',
    '        filter name String',
    '      query Summary => Count'
];

const model = (...extra: string[]): string[] => [...layout, 'module Work', '  feature Tasks', ...slices, '  feature Empty', ...extra];
const codesOf = (lines: string[]): string[] => parse(lines.join('\n')).diagnostics.map(diagnostic => diagnostic.code);

const cases: [string, string[], string][] = [
    ['a contribution item of the wrong kind', model('  contribute to actions', '    filter f title "F"'), DiagnosticCodes.OutletItemWrongKind],
    ['a button invoking a slice that does not exist', model('  contribute to actions', '    button b title "B" slice Nothing'), DiagnosticCodes.ToolbarButtonSliceNotFound],
    ['a button invoking a slice that is not a state change', model('  contribute to actions', '    button b title "B" slice Listing'), DiagnosticCodes.ToolbarButtonSliceNotStateChange],
    ['a button invoking an ambiguous slice', ['layout Shell', '  main page', '  outlet actions of actions', 'module A', '  feature F', '    slice StateChange Add', '      command Add', '        name String', 'module B', '  feature F', '    slice StateChange Add', '      command Add', '        name String', '  contribute to actions', '    button b title "B" slice Add'], DiagnosticCodes.ToolbarButtonSliceAmbiguous],
    ['a button without a slice', model('  contribute to actions', '    button b title "B"'), DiagnosticCodes.ToolbarButtonWithoutSlice],
    ['a button in a group invoking nothing known', model('  contribute to actions', '    group g title "G"', '      button b title "B" slice Nothing'), DiagnosticCodes.ToolbarButtonSliceNotFound],
    ['a filter of a query that does not exist', model('  contribute to filters', '    filter f title "F" sets Nothing.name'), DiagnosticCodes.UnknownQuery],
    ['a filter of a parameter the query does not have', model('  contribute to filters', '    filter f title "F" sets Records.price'), DiagnosticCodes.OutletFilterParameterUnknown],
    ['a filter that sets nothing', model('  contribute to filters', '    filter f title "F"'), ''],
    ['a stat that shows nothing', model('  contribute to stats', '    stat s title "S"'), ''],
    ['a stat of a query that does not exist', model('  contribute to stats', '    stat s title "S" value from Nothing.total'), DiagnosticCodes.UnknownQuery],
    ['a stat of a list query', model('  contribute to stats', '    stat s title "S" value from Records.name'), DiagnosticCodes.OutletStatFromListQuery],
    ['a tab showing a screen that does not exist', model('  contribute to tabs', '    tab t title "T" shows Nothing'), DiagnosticCodes.UnknownScreen],
    ['a navigation item rendering into an unknown slot', model('  contribute to nav', '    item i title "I" target Work slot nowhere'), DiagnosticCodes.NavSlotUnknown],
    ['a navigation item without a target', model('  contribute to nav', '    item i title "I"'), DiagnosticCodes.NavTargetMissing],
    ['a navigation item targeting a module that does not exist', model('  contribute to nav', '    item i title "I" target Nowhere'), DiagnosticCodes.NavTargetMissing],
    ['a navigation item targeting a feature that does not exist', model('  contribute to nav', '    item i title "I" target Work.Nowhere'), DiagnosticCodes.NavTargetMissing],
    ['a navigation item targeting a module without features', ['layout Shell', '  main page', '  outlet nav of navigation', 'module Bare', '  contribute to nav', '    item i title "I" target Bare'], DiagnosticCodes.NavModuleWithoutFeatures],
    ['a navigation item with a sub-item targeting nothing', model('  contribute to nav', '    item i title "I"', '      item j title "J" target Nowhere'), DiagnosticCodes.NavTargetMissing],
    ['a contribution to an outlet that does not exist', model('  contribute to nowhere', '    button b title "B"'), DiagnosticCodes.UnknownOutlet],
    ['a module outlet that is not inherited', model('  outlet actions extends', '    hide missing'), DiagnosticCodes.OutletItemNotInherited],
    ['a button that runs a slice whose input nothing supplies', model('  outlet actions overrides', '    button b title "B" slice Add', '      runs'), DiagnosticCodes.RunsHookNeedsForm],
    ['a button that opens a slice without a screen', model('  outlet actions overrides', '    button b title "B" slice Add opens dialog'), DiagnosticCodes.HookToScreenlessSlice],
    ['a button that opens a slot the template does not declare', model('  contribute to actions', '    button b title "B" slice Add opens in slot nowhere'), DiagnosticCodes.HookToScreenlessSlice],
    ['an enabled condition on item for a screen', model('  contribute to actions', '    button b title "B" for screen', '      enabled when item.name == "x"'), DiagnosticCodes.EnabledWhenWithoutItem],
    ['an item standing beside an item the outlet does not have', model('  outlet actions extends', '    button b title "B" slice Add', '      before ghost'), DiagnosticCodes.OutletItemNotInherited],
    ['an item repeating the id of an inherited item', ['layout Shell', '  main page', '  outlet actions of actions', '    default save title "Save"', 'module Work', '  outlet actions extends', '    button save title "S"'], DiagnosticCodes.DuplicateOutletItem],
    ['two templates of a module offering the same outlet', ['layout Shell', '  main page', 'module M', '  screen template A', '    outlet shared of actions', '  screen template B', '    outlet shared of actions', '  contribute to shared', '    button b title "B"'], DiagnosticCodes.AmbiguousReference],
    ['two modules offering the same outlet', ['layout Shell', '  main page', 'module A', '  screen template T', '    outlet shared of actions', 'module B', '  screen template T', '    outlet shared of actions', 'module C', '  contribute to shared', '    button b title "B"'], DiagnosticCodes.AmbiguousReference],
    ['a declared outlet offering a logo from a module', model('  screen template T', '    outlet extra of navigation', '      logo image "/a.svg" title "A"'), DiagnosticCodes.NavigationLogoNotAllowed],
    ['an offered outlet repeating an item id', model('  screen template T', '    outlet extra of actions', '      default a title "A"', '      default a title "B"'), DiagnosticCodes.DuplicateOutletItem],
    ['two layouts declaring a logo in separate outlets', [...layout, '  outlet nav2 of navigation', '    logo image "/a.svg" title "A"', '  outlet nav3 of navigation', '    logo image "/b.svg" title "B"'], DiagnosticCodes.NavigationLogoNotAllowed]
];

describe('when validating outlets, hooks and contributions that are wrong', () => {
    for (const [description, lines, code] of cases) {
        it(code === '' ? `should accept ${description}` : `should report ${code} for ${description}`, () => {
            const codes = codesOf(lines);
            if (code === '') codes.should.deep.equal([]);
            else codes.should.include(code);
        });
    }
});
