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

describe('when parsing screen release UI bindings', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let component: import('../Syntax/Screens').ScreenComponentSyntax;
    let toolbar: import('../Syntax/Screens').ScreenToolbarSyntax;

    beforeEach(() => {
        result = a_parsed_document(
            'module Sales',
            '  feature Invoices',
            '    slice StateView BrowseInvoices',
            '      screen BrowseInvoices',
            '        toolbar main',
            '          item details navigate to InvoiceDetails',
            '            parameter invoiceId from component invoices.selectedItem.id',
            '          item edit action EditInvoice',
            '            label "Edit"',
            '            icon edit',
            '            presentation placement "primary"',
            '        component scene.web.DataGrid invoices',
            '          id "invoices:list"',
            '          context from query AllInvoices.items',
            '          property selectedItem from component "invoices:list".selectedItem null preserve',
            '          property pageSize from literal 25',
            '          property title = "Invoices"',
            '          icon table',
            '          presentation density "compact"',
            '          exposes selectedInvoice from component invoices.selectedItem',
            '          outlet detail',
            '            summary selectedInvoice',
            '              field invoiceId label "Invoice"',
        );
        toolbar = result.value.modules[0].features[0].slices[0].screens[0].directives[0] as import('../Syntax/Screens').ScreenToolbarSyntax;
        component = result.value.modules[0].features[0].slices[0].screens[0].directives[1] as import('../Syntax/Screens').ScreenComponentSyntax;
    });

    it('should report nothing', () => result.diagnostics.should.deep.equal([]));
    it('should read toolbar parameters as component bindings', () => toolbar.items[0].parameters[0].binding.should.deep.include({ bindingKind: 'ComponentProperty', componentId: 'invoices', path: 'selectedItem.id' }));
    it('should read toolbar presentation', () => toolbar.items[1].presentation[0].value!.should.equal('primary'));
    it('should read component query context', () => component.context!.should.deep.include({ bindingKind: 'QueryResult', query: 'AllInvoices', path: 'items' }));
    it('should read exact component stable ids', () => component.stableId!.should.equal('invoices:list'));
    it('should read component property null behavior', () => component.properties[0].binding!.should.deep.include({ componentId: 'invoices:list', nullBehavior: 'Preserve' }));
    it('should read literal bindings', () => component.properties[1].binding!.should.deep.include({ bindingKind: 'Literal', literal: { kind: 'LiteralExpressionSyntax', value: 25, location: { line: 16, column: 11, path: 'Document.play' } } }));
    it('should read component literals', () => component.properties[2].value!.should.deep.include({ kind: 'LiteralExpressionSyntax', value: 'Invoices' }));
    it('should read component exposed bindings', () => component.exposes[0].binding.componentId!.should.equal('invoices'));
    it('should read component outlets', () => component.outlets[0].directives[0].kind.should.equal('ScreenSummarySyntax'));
});

describe('when parsing invalid screen release UI bindings', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let binding: import('../Syntax/Screens').UiBindingSyntax;

    beforeEach(() => {
        result = a_parsed_document(
            'module Sales',
            '  feature Invoices',
            '    slice StateView BrowseInvoices',
            '      screen BrowseInvoices',
            '        component scene.web.DataGrid invoices',
            '          property selectedItem from component invoices',
            '          property title from data name carry forever',
        );
        const component = result.value.modules[0].features[0].slices[0].screens[0].directives[0] as import('../Syntax/Screens').ScreenComponentSyntax;
        binding = component.properties[0].binding!;
    });

    it('should keep invalid binding text', () => binding.rawText!.should.equal('from component invoices'));
    it('should report invalid and unsupported binding diagnostics', () => result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0103', 'PLAY0103']));
});

describe('when parsing every UI binding source form', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let properties: readonly import('../Syntax/Screens').ComponentPropertySyntax[];

    beforeEach(() => {
        result = a_parsed_document(
            'module Sales',
            '  feature Invoices',
            '    slice StateView BrowseInvoices',
            '      screen BrowseInvoices',
            '        component scene.web.DataGrid invoices',
            '          context selectedInvoice',
            '          property dataValue from data current.name mode oneWay null clear expected String',
            '          property queryValue from query AllInvoices mode twoWay null propagate',
            '          property componentValue from component other.selected null preserve',
            '          property exactComponentValue from component "other:list".selected null preserve',
            '          property literalValue from literal true mode oneWay',
            '          property objectLiteral from literal {"label":"Done","count":2}',
            '          property arrayLiteral from literal ["one",2,false,null]',
            '          property invalidLiteral from literal true mode sideways',
            '          property invalidQuery from query 123',
            '          property invalidComponent from component other',
        );
        const component = result.value.modules[0].features[0].slices[0].screens[0].directives[0] as import('../Syntax/Screens').ScreenComponentSyntax;
        properties = component.properties;
    });

    it('should lower legacy context to data context', () => {
        const component = result.value.modules[0].features[0].slices[0].screens[0].directives[0] as import('../Syntax/Screens').ScreenComponentSyntax;
        component.context!.should.deep.include({ bindingKind: 'DataContext', path: 'selectedInvoice' });
    });
    it('should read data binding modifiers', () => properties[0].binding!.should.deep.include({ bindingKind: 'DataContext', path: 'current.name', mode: 'OneWay', nullBehavior: 'Clear', expectedValueType: 'String' }));
    it('should read query bindings without a path', () => properties[1].binding!.should.deep.include({ bindingKind: 'QueryResult', query: 'AllInvoices', path: '', mode: 'TwoWay', nullBehavior: 'Propagate' }));
    it('should read component bindings with null preserve', () => properties[2].binding!.should.deep.include({ bindingKind: 'ComponentProperty', componentId: 'other', componentPropertyPath: 'selected', nullBehavior: 'Preserve' }));
    it('should read quoted component bindings', () => properties[3].binding!.should.deep.include({ bindingKind: 'ComponentProperty', componentId: 'other:list', componentPropertyPath: 'selected', nullBehavior: 'Preserve' }));
    it('should read typed literal bindings', () => properties[4].binding!.should.deep.include({ bindingKind: 'Literal', mode: 'OneWay' }));
    it('should read object literal bindings', () => properties[5].binding!.literal!.kind.should.equal('ObjectExpressionSyntax'));
    it('should read array literal bindings', () => properties[6].binding!.literal!.kind.should.equal('ListExpressionSyntax'));
    it('should preserve invalid literal modifier text', () => properties[7].binding!.rawText!.should.equal('mode sideways'));
    it('should preserve invalid query text', () => properties[8].binding!.rawText!.should.equal('from query 123'));
    it('should preserve invalid component text', () => properties[9].binding!.rawText!.should.equal('from component other'));
    it('should report invalid typed binding diagnostics', () => result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0103', 'PLAY0103', 'PLAY0103']));
});

describe('when parsing screen release navigation and diagnostics', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let navigateDirective: import('../Syntax/Screens').ScreenNavigateSyntax;
    let toolbarDirective: import('../Syntax/Screens').ScreenToolbarSyntax;

    beforeEach(() => {
        result = a_parsed_document(
            'module Sales',
            '  feature Invoices',
            '    slice StateView BrowseInvoices',
            '      screen BrowseInvoices',
            '        navigate to Details',
            '          route "/invoices/{invoiceId}"',
            '          parameter invoiceId from data selected.id',
            '        toolbar secondary',
            '          item openDialog dialog EditDialog',
            '          bad item',
            '        component scene.web.DataGrid invoices',
            '          presentation broken',
            '          unknown child',
        );
        navigateDirective = result.value.modules[0].features[0].slices[0].screens[0].directives[0] as import('../Syntax/Screens').ScreenNavigateSyntax;
        toolbarDirective = result.value.modules[0].features[0].slices[0].screens[0].directives[1] as import('../Syntax/Screens').ScreenToolbarSyntax;
    });

    it('should read navigation route metadata', () => navigateDirective.route!.should.equal('/invoices/{invoiceId}'));
    it('should read navigation parameters as data bindings', () => navigateDirective.parameters[0].binding.path.should.equal('selected.id'));
    it('should read dialog toolbar items', () => toolbarDirective.items[0].syntaxKind.should.equal('Dialog'));
    it('should report invalid toolbar and component children', () => result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0103', 'PLAY0103', 'PLAY0103']));
});

describe('when parsing invalid screen release component children', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'module Sales',
            '  feature Invoices',
            '    slice StateView BrowseInvoices',
            '      screen BrowseInvoices',
            '        component scene.web.DataGrid invoices',
            '          id',
            '          uses SharedBehavior',
            '            argument selected from data selectedItem',
            '          property',
            '          exposes selectedInvoice',
        );
    });

    it('should report invalid id, property and expose diagnostics', () => result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0103', 'PLAY0103', 'PLAY0103']));
});

describe('when parsing invalid screen release component declarations', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'module Sales',
            '  feature Invoices',
            '    slice StateView BrowseInvoices',
            '      screen BrowseInvoices',
            '        component',
            '          property title = "Ignored"',
            '        component scene.web.DataGrid invoices',
            '          on changed',
            '            set state selected from data selectedItem',
        );
    });

    it('should report the invalid component declaration', () => result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0103']));
    it('should continue with the valid component', () => result.value.modules[0].features[0].slices[0].screens[0].directives[0].kind.should.equal('ScreenComponentSyntax'));
});
