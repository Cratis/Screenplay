// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { CaptureSyntax } from '../Syntax/Captures';
import { ReactionTriggerSyntax } from '../Syntax/Reactions';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing what automations and translations do', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let trigger: ReactionTriggerSyntax;
    let conditional: ReactionTriggerSyntax;
    let capture: CaptureSyntax;

    beforeEach(() => {
        result = a_parsed_document(
            'module Billing',
            '  feature Invoicing',
            '    slice Automation ChaseOverdue',
            '      reaction OverdueChaser',
            '        at 08:00',
            '          produces InvoiceMarkedOverdue',
            '            overdueAt = $context.occurred',
            '          invokes SendReminder',
            '            invoiceId = invoiceId',
            '          csharp',
            '            ```',
            '            return [];',
            '            ```',
            '        when InvoiceSent',
            '          produces when amount > 100',
            '            LargeInvoiceSent',
            '              invoiceId = invoiceId',
            '    slice Translate LegacySync',
            '      capture LegacyCapture',
            '        source api',
            '          api LegacyApi',
            '          poll 5m',
            '        key id',
            '        map',
            '          status = status translate',
            '            "sendt" => sent',
            '        append InvoiceStatusChanged',
            '          when status',
            '            invoiceId = $.id',
            '        children lines identified by lineNumber',
            '          map',
            '            productName = name',
            '          append InvoiceLineAdded',
            '            when added',
            '              invoiceId = $.id',
            '        nested contact',
            '          append ContactUpdated',
            '            when email',
            '              invoiceId = $.id');
        const feature = result.value.modules[0].features[0];
        [trigger, conditional] = feature.slices[0].reactions[0].triggers;
        capture = feature.slices[1].captures[0];
    });

    it('should read without diagnostics', () => result.diagnostics.should.deep.equal([]));
    it('should read the event a trigger produces', () => trigger.produces.map(produced => produced.event).should.deep.equal(['InvoiceMarkedOverdue']));
    it('should read the command a trigger invokes', () => trigger.invokes.map(invoked => invoked.command).should.deep.equal(['SendReminder']));
    it('should read the event a conditional produces names on the line below', () => conditional.produces.map(produced => produced.event).should.deep.equal(['LargeInvoiceSent']));
    it('should read the capture by name', () => capture.name.should.equal('LegacyCapture'));
    it('should read where the capture reads from', () => {
        capture.source!.syntaxKind.should.equal('api');
        capture.source!.settings.map(setting => `${setting.name} ${setting.value}`).should.deep.equal(['api LegacyApi', 'poll 5m']);
    });
    it('should read the capture key', () => capture.key!.should.equal('id'));
    it('should read the events the capture appends', () => capture.appends.map(append => append.event).should.deep.equal(['InvoiceStatusChanged']));
    it('should read the events its children append', () => {
        capture.children[0].property.should.equal('lines');
        capture.children[0].identifiedBy.should.equal('lineNumber');
        capture.children[0].appends.map(append => append.event).should.deep.equal(['InvoiceLineAdded']);
    });
    it('should read the events its nested objects append', () => capture.nested[0].appends.map(append => append.event).should.deep.equal(['ContactUpdated']));
});
