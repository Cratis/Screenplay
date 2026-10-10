// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { LiteralExpressionSyntax } from '../Syntax/Expressions';
import { ScreenNavigateSyntax, ScreenSyntax } from '../Syntax/Screens';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing screen composition', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let screen: ScreenSyntax;

    beforeEach(() => {
        result = a_parsed_document(
            'exposure for Shell',
            '  property "shell:header".title label "Header title"',
            '  property shellHeader.actions operations add, reorder fields label, icon',
            '  property shellHeader.items operations none fields none reexposes Outer',
            '',
            'instance Browse',
            '  set "shell:header".title = "Invoices"',
            '  items shellHeader.actions',
            '    item "export:csv"',
            '      label = "Export"',
            '      icon = null',
            '',
            'module Sales',
            '  feature Invoices',
            '    slice StateView Browse',
            '      screen Browse',
            '        navigate to Browse',
            '          outlet detail',
            '        contribute to Actions order 10',
            '          title "Register"',
            '        contribute to Navigation',
            '          title "Filters"',
        );
        screen = result.value.modules[0].features[0].slices[0].screens[0];
    });

    it('should parse without diagnostics', () => result.diagnostics.should.be.empty);
    it('should read the exposure owner', () => result.value.exposures![0].owner.should.equal('Shell'));
    it('should read exact quoted component ids', () => result.value.exposures![0].properties[0].component.should.equal('shell:header'));
    it('should read exposed labels', () => result.value.exposures![0].properties[0].label!.should.equal('Header title'));
    it('should read single-value exposures', () => result.value.exposures![0].properties[0].isCollection.should.be.false);
    it('should read collection operations', () => result.value.exposures![0].properties[1].operations.should.deep.equal(['add', 'reorder']));
    it('should read editable fields', () => result.value.exposures![0].properties[1].editableFields.should.deep.equal(['label', 'icon']));
    it('should read an empty operation grant', () => result.value.exposures![0].properties[2].should.deep.include({ isCollection: true, operations: [], restrictsFields: true, editableFields: [] }));
    it('should read re-exposures', () => result.value.exposures![0].properties[2].reExposes!.should.equal('Outer'));
    it('should read the instance', () => result.value.instanceContributions![0].instance.should.equal('Browse'));
    it('should read typed set values', () => (result.value.instanceContributions![0].contributions[0].value as LiteralExpressionSyntax).value!.should.equal('Invoices'));
    it('should read contributed items', () => result.value.instanceContributions![0].contributions[1].items[0].id.should.equal('export:csv'));
    it('should read null item values', () => (result.value.instanceContributions![0].contributions[1].items[0].values[1].value as LiteralExpressionSyntax).should.deep.include({ value: null }));
    it('should read screen contributions', () => screen.contributions!.map(contribution => contribution.contributionPoint).should.deep.equal(['Actions', 'Navigation']));
    it('should read contribution order', () => screen.contributions![0].order!.should.equal(10));
    it('should read contributions without order', () => (screen.contributions![1].order === null).should.be.true);
    it('should read contributed content', () => screen.contributions![0].directives[0].kind.should.equal('ScreenTitleSyntax'));
    it('should read navigation outlets', () => (screen.directives[0] as ScreenNavigateSyntax).outlet!.should.equal('detail'));
});

describe('when parsing malformed screen composition', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'exposure Shell',
            '  property title',
            '  property shellHeader.items operations shuffle',
            '',
            'instance',
            '  put shellHeader.title = 1',
            '  items shellHeader.actions',
            '    entry first',
            '    item first',
            '      label "First"',
            '',
            'module Sales',
            '  feature Invoices',
            '    slice StateView Browse',
            '      screen Browse',
            '        contribute Actions',
            '          title "Lost"',
            '        navigate to Browse',
            '          outlet',
        );
    });

    it('should report each malformed line in source order', () => result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([
        'PLAY0621', 'PLAY0621', 'PLAY0621', 'PLAY0625', 'PLAY0625', 'PLAY0625', 'PLAY0625', 'PLAY0103', 'PLAY0107',
    ]));
    it('should keep the malformed exposure visible', () => result.value.exposures![0].owner.should.equal('exposure Shell'));
    it('should keep the malformed instance visible', () => result.value.instanceContributions![0].instance.should.equal('instance'));
    it('should keep the well-formed items', () => result.value.instanceContributions![0].contributions[0].items.map(item => item.id).should.deep.equal(['first']));
});
