// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseForAuthoring } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';

const diagnosticsOf = (...lines: string[]) => parseForAuthoring(lines.join('\n'), undefined, undefined, false).diagnostics;

// Each malformed declaration reports the one diagnostic that names what is wrong with it.
const cases: [string, string[], string][] = [
    ['an arrangement that is neither flow nor freeform', ['layout L', '  arrangement stacked', '    row'], DiagnosticCodes.InvalidArrangementDeclaration],
    ['a variant in a flow arrangement', ['layout L', '  arrangement flow', '    variant width compact, height compact', '      place a hidden'], DiagnosticCodes.ArrangementDirectiveMismatch],
    ['a flow arrangement with a malformed override', ['layout L', '  arrangement flow', '    when depth tall', '      row'], DiagnosticCodes.InvalidArrangementOverride],
    ['a flow arrangement with a repeated override', ['layout L', '  arrangement flow', '    when width compact', '      row', '    when width compact', '      column'], DiagnosticCodes.DuplicateArrangementOverride],
    ['a flow arrangement with a malformed container', ['layout L', '  arrangement flow', '    row gap many', '      a'], DiagnosticCodes.InvalidArrangementContainer],
    ['a flow arrangement with a malformed slot', ['layout L', '  arrangement flow', '    row', '      A!'], DiagnosticCodes.InvalidArrangementSlotAttributes],
    ['a freeform arrangement without variants', ['layout L', '  arrangement freeform', '    row'], DiagnosticCodes.ArrangementDirectiveMismatch],
    ['a freeform arrangement with a repeated variant', ['layout L', '  arrangement freeform', '    variant width compact, height compact', '      place a hidden', '    variant width compact, height compact', '      place a hidden'], DiagnosticCodes.DuplicateVariant],
    ['a freeform arrangement with a malformed variant', ['layout L', '  arrangement freeform', '    variant wide', '      place a hidden'], DiagnosticCodes.InvalidVariantDeclaration],
    ['a variant that places a slot twice', ['layout L', '  arrangement freeform', '    variant width compact, height compact', '      place a hidden', '      place a hidden'], DiagnosticCodes.DuplicatePlaceInVariant],
    ['a variant with a malformed place', ['layout L', '  arrangement freeform', '    variant width compact, height compact', '      place a somewhere'], DiagnosticCodes.InvalidPlaceDeclaration],
    ['a layout that declares two arrangements', ['layout L', '  arrangement flow', '    row', '  arrangement flow', '    row'], DiagnosticCodes.DuplicateArrangement],
    ['a layout with a malformed header', ['layout 1'], DiagnosticCodes.InvalidLayoutDeclaration],
    ['a screen template with a malformed header', ['module M', '  screen template 1'], DiagnosticCodes.InvalidScreenTemplateDeclaration],
    ['a dialog template with a malformed header', ['module M', '  dialog template 1'], DiagnosticCodes.InvalidDialogTemplateDeclaration],
    ['a layout that fits a slot', ['layout L', '  fits slot main'], DiagnosticCodes.FitsSlotNotAllowed],
    ['a screen template with a malformed fits', ['module M', '  screen template T', '    fits main'], DiagnosticCodes.InvalidFitsSlotDeclaration],
    ['a screen template that fits twice', ['module M', '  screen template T', '    fits slot a', '    fits slot b'], DiagnosticCodes.DuplicateFitsSlot],
    ['a layout with a malformed exposed value', ['layout L', '  exposes'], DiagnosticCodes.UnknownLayoutDirective],
    ['a layout with a removed outlet form', ['layout L', '  outlet sidebar'], DiagnosticCodes.RemovedTemplateOutlet],
    ['a layout with a malformed outlet declaration', ['layout L', '  outlet sidebar of widgets'], DiagnosticCodes.InvalidOutletDeclaration],
    ['a layout that offers an outlet twice', ['layout L', '  outlet a of actions', '  outlet a of actions'], DiagnosticCodes.DuplicateOutlet],
    ['a layout slot with a body', ['layout L', '  main page', '    nested'], DiagnosticCodes.UnknownLayoutDirective],
    ['a layout slot that contributes', ['layout L', '  main page contributes nav'], DiagnosticCodes.RemovedSlotContribution],
    ['a layout that displays a name', ['layout L', '  display "Name"'], DiagnosticCodes.UnknownLayoutDirective],
    ['a template with malformed scopes', ['module M', '  screen template T', '    scopes planet'], DiagnosticCodes.UnknownLayoutDirective],
    ['a template with scopes none', ['module M', '  screen template T', '    scopes none'], ''],
    ['a template with a malformed display', ['module M', '  screen template T', '    display Name'], DiagnosticCodes.UnknownLayoutDirective],
    ['a template with a malformed description', ['module M', '  screen template T', '    description Name'], DiagnosticCodes.UnknownLayoutDirective],
    ['a template with a malformed content', ['module M', '  screen template T', '    content', '      title "A"'], DiagnosticCodes.UnknownLayoutDirective],
    ['a template providing content for a slot it does not declare', ['module M', '  screen template T', '    content body', '      title "A"'], DiagnosticCodes.UnknownTemplateContentSlot],
    ['a template with a content slot that is not lower case', ['module M', '  screen template T', '    content Body'], DiagnosticCodes.UnknownLayoutDirective],
    ['a template with a scope that has an unknown slice kind', ['module M', '  screen template T', '    scopes slice bogus'], DiagnosticCodes.UnknownLayoutDirective],
    ['a contribution with a malformed header', ['module M', '  contribute sidebar'], DiagnosticCodes.InvalidContributionDeclaration],
    ['a contribution with a repeated navigate', ['module M', '  contribute to a', '    navigate to X', '    navigate to Y'], DiagnosticCodes.DuplicateContributionNavigate],
    ['a contribution with a repeated label', ['module M', '  contribute to a', '    label "A"', '    label "B"'], DiagnosticCodes.DuplicateContributionLabel],
    ['a contribution with a repeated order', ['module M', '  contribute to a', '    order 1', '    order 2'], DiagnosticCodes.DuplicateContributionOrder],
    ['a contribution with a repeated icon', ['module M', '  contribute to a', '    icon a', '    icon b'], DiagnosticCodes.UnknownContributionDirective],
    ['a contribution with a malformed label', ['module M', '  contribute to a', '    label A'], DiagnosticCodes.InvalidContributionLabel],
    ['a contribution with a malformed order', ['module M', '  contribute to a', '    order first'], DiagnosticCodes.InvalidOrderDeclaration],
    ['a contribution with a malformed icon', ['module M', '  contribute to a', '    icon "a" b'], DiagnosticCodes.UnknownContributionDirective],
    ['a contribution with a malformed destination', ['module M', '  contribute to a', '    destination nowhere'], DiagnosticCodes.UnknownContributionDirective],
    ['a contribution with a removed destination', ['module M', '  contribute to a', '    destination outlet detail'], DiagnosticCodes.RemovedDestinationOutlet],
    ['a contribution with an unknown directive', ['module M', '  contribute to a', '    sparkle'], DiagnosticCodes.UnknownContributionDirective],
    ['a contribution with an external destination', ['module M', '  contribute to a', '    destination external "/x"'], ''],
    ['a contribution with a dialog destination', ['module M', '  contribute to a', '    destination dialog Edit'], ''],
    ['an outlet declaring a malformed default', ['layout L', '  outlet a of actions', '    default'], DiagnosticCodes.InvalidOutletItem],
    ['an outlet declaring something other than defaults', ['layout L', '  outlet a of actions', '    button x title "X"'], DiagnosticCodes.InvalidOutletItem],
    ['a navigation outlet declaring two logos', ['layout L', '  outlet n of navigation', '    logo image "/a.svg" title "A"', '    logo image "/b.svg" title "B"'], DiagnosticCodes.NavigationLogoNotAllowed],
    ['a navigation outlet declaring a malformed logo', ['layout L', '  outlet n of navigation', '    logo "/a.svg"'], DiagnosticCodes.InvalidNavigationLogo],
    ['an outlet declaration that inherits', ['layout L', '  outlet a inherits'], DiagnosticCodes.InvalidOutletDeclaration]
];

describe('when parsing layouts, templates and contributions that are wrong', () => {
    for (const [description, lines, code] of cases) {
        it(code === '' ? `should read ${description} without error` : `should report ${code} for ${description}`, () => {
            const diagnostics = diagnosticsOf(...lines);
            if (code === '') diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.deep.equal([]);
            else diagnostics.map(diagnostic => diagnostic.code).should.include(code);
        });
    }
});
