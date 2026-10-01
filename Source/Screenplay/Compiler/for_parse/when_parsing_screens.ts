// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import {
    ScreenActionSyntax, ScreenCodeSyntax, ScreenDataSyntax, ScreenSectionSyntax, ScreenSummarySyntax, ScreenSyntax,
    ScreenTableSyntax, ScreenTemplateReferenceSyntax, ScreenTitleSyntax,
} from '../Syntax/Screens';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing screens', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let screens: readonly ScreenSyntax[];

    beforeEach(() => {
        result = a_parsed_document(
            'module Invoicing',
            '  feature Invoices',
            '    slice StateView InvoiceList',
            '      screen Invoices',
            '        title "All \\"open\\" invoices"',
            '        data Invoice[] via query AllInvoices',
            '        action Register',
            '          label $strings.invoices.register',
            '          navigate to Details by invoiceId',
            '        table invoices',
            '          column number label "#"',
            '          column total',
            '          on row-click navigate to Details by invoiceId',
            '          uses Highlighting',
            '        on load',
            '          refresh AllInvoices',
            '        uses Confirming',
            '      screen Details',
            '        file Screens/Details.tsx',
            '        template MasterDetail',
            '          sidebar',
            '            summary invoice',
            '              field number label "Number"',
            '          main',
            '            section help',
            '              navigate to Invoices',
            '              ```html',
            '<p>Help</p>',
            '              ```',
            '      event InvoiceListed',
            '        number String',
        );
        screens = result.value.modules[0].features[0].slices[0].screens;
    });

    it('should report nothing', () => result.diagnostics.should.deep.equal([]));
    it('should read both screens', () => screens.map(screen => screen.name).should.deep.equal(['Invoices', 'Details']));
    it('should read the directives in order', () =>
        screens[0].directives.map(directive => directive.kind).should.deep.equal([
            'ScreenTitleSyntax', 'ScreenDataSyntax', 'ScreenActionSyntax', 'ScreenTableSyntax', 'ScreenBehaviorSyntax', 'ScreenUsesBehaviorSyntax',
        ]));
    it('should unescape the title', () => (screens[0].directives[0] as ScreenTitleSyntax).text.should.equal('All "open" invoices'));
    it('should read the data as a collection of the read model', () => {
        const data = screens[0].directives[1] as ScreenDataSyntax;
        [data.type.name, data.type.isCollection, data.query, data.by].should.deep.equal(['Invoice', true, 'AllInvoices', null]);
    });
    it('should read the action with its label and navigation', () => {
        const action = screens[0].directives[2] as ScreenActionSyntax;
        [action.command, action.label, action.navigate?.screen, action.navigate?.by].should.deep.equal(['Register', '$strings.invoices.register', 'Details', 'invoiceId']);
    });
    it('should read the table columns and row click', () => {
        const table = screens[0].directives[3] as ScreenTableSyntax;
        table.columns.map(column => [column.property, column.label]).should.deep.equal([['number', '#'], ['total', null]]);
        table.rowClick!.screen.should.equal('Details');
    });
    it('should not model the file reference', () => screens[1].directives.length.should.equal(1));
    it('should read the template slots', () => {
        const template = screens[1].directives[0] as ScreenTemplateReferenceSyntax;
        [template.name, ...template.slots.map(slot => slot.name)].should.deep.equal(['MasterDetail', 'sidebar', 'main']);
    });
    it('should read the summary fields in the slot', () => {
        const summary = (screens[1].directives[0] as ScreenTemplateReferenceSyntax).slots[0].directives[0] as ScreenSummarySyntax;
        [summary.target, summary.fields[0].property, summary.fields[0].label].should.deep.equal(['invoice', 'number', 'Number']);
    });
    it('should read the code in the section', () => {
        const section = (screens[1].directives[0] as ScreenTemplateReferenceSyntax).slots[1].directives[0] as ScreenSectionSyntax;
        const code = section.directives[1] as ScreenCodeSyntax;
        [section.name, section.directives[0].kind, code.code.language, code.code.code].should.deep.equal(['help', 'ScreenNavigateSyntax', 'html', '<p>Help</p>']);
    });
    it('should read what follows the screens', () => result.value.modules[0].features[0].slices[0].events[0].name.should.equal('InvoiceListed'));
});
