// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, beforeEach, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { diagnosticCodes } from '../../diagnostic-codes';
import { mergeSymbols, scanDocument } from '../../symbols';
import { ValidationIssue, validateLines } from '../../validation';

// One document holding every condition the editor and the compiler both check, so a condition added
// later without a code fails here rather than reaching a user as a codeless squiggle.
const document = [
    'type OptionalExamples',
    '  value String?',
    '  id Uuid identifier optional',
    'module OptionalExamples',
    '  feature F',
    '    slice StateChange S',
    '      command C',
    '        reads View optional as existing',
    'concept InvoiceId : Guid',
    'concept InvoiceId : String',
    'concept Amount : Wat',
    '',
    'module Invoicing',
    '  import Customers.CustomerRegistered',
    '  feature Invoices',
    '    slice Wat Register',
    '      event InvoiceRegistered',
    '        invoiceId InvoiceId',
    '\t        amount Amount',
    '      command RegisterInvoice',
    '        invoiceId InvoiceId identifier',
    '        amount Amount identifier',
    '        reference Unknown',
    '        authorize CanRegister',
    '        produces InvoiceArchived',
    '          registeredBy = $context.wat',
    '          causedBy = $context.causedBy.wat',
    '          department = $context.identity.wat',
    '      command ArchiveInvoice',
    '        invoiceId InvoiceId identifier',
    '        produces InvoiceArchived',
    '      projection Statistics => StatisticsReadModel',
    '        from InvoiceRegistered',
    '          cause = $eventContext.causationId',
    '          typeName = $eventContext.eventType.name',
    '          firstCause = $eventContext.causation.occurred',
    '          nothing = $eventContext.',
    '          count byUser.$causedBy.subject',
    '      command Rename',
    '        invoiceId InvoiceId identifier',
    '        otherId InvoiceId',
    '        produces event Renamed generation 2',
    '          invoiceId InvoiceId = invoiceId',
    '          id "Renamed"',
    '          id "Older"',
    '          documentation "not fenced"',
    '        produces Other',
    '          for otherId',
    '      event Renamed',
    '      reaction React',
    '        every 1 day',
    '          produces event Unsupported',
    '      query Overdue => OverdueReadModel[]',
    '        performer',
    '          csharp',
    '            ```',
    '            return readModels;',
];

describe('when validating a document with a problem of every coded kind', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validateLines(document);
        const responses = [
            ['command C', 'example Fixture : C', '  no stream'],
            ['command C', '  handler', '    implementation', '      hint " "', '      unknown', '      file C.cs', '      file D.cs'],
            ['type Outside', '  id Uuid generated'],
            ['concept Id : Uuid', 'command C', '  id Id generated', '  generated String', '  value String generated', '  other Id identifier generated', '  returns', '    id String = id', '    id = unknown'],
            ['concept Id : Uuid', 'command C', '  id Id generated', '  returns @id', '  returns'],
            ['concept Id : Uuid', 'command C', '  id Id generated', '  returns @id', 'specification S', '  when C', '    id = "wrong input"', '    generated nope = "wrong fixture"', '  then returns', '    nope = "wrong shape"'],
        ];
        issues.push(...responses.flatMap(lines => validateLines(lines)));
        const sources = [
            ['eventsource Account', '  identifier Uuid optional'],
            ['eventsource Account', '  stream Other', 'command C', '  stream Account.Missing'],
            ['import Account.Transactions', 'type Transactions', '  value String', 'eventsource Account', '  stream Transactions', 'command C', '  stream Account.Transactions'],
            ['eventsource Account', '  stream Transactions', '    streamId Int'],
            ['eventsource Account', '  id "Account"'],
        ];
        issues.push(...sources.flatMap(lines => validateLines(lines)));
        const vectors = JSON.parse(readFileSync(new URL('../../../../Compiler/Conformance/diagnostics.json', import.meta.url), 'utf8')) as { cases: { source: string[]; diagnostics: string[] }[] };
        const authoring = ['PLAY0633', 'PLAY0634', 'PLAY0635', 'PLAY0636', 'PLAY0637', 'PLAY0638', 'PLAY0639', 'PLAY0640', 'PLAY0641', 'PLAY0642', 'PLAY0643', 'PLAY0644', 'PLAY0645', 'PLAY0646', 'PLAY0647', 'PLAY0648', 'PLAY0649', 'PLAY0596', 'PLAY0597', 'PLAY0598', 'PLAY0599', 'PLAY0600', 'PLAY0601', 'PLAY0010', 'PLAY0012', 'PLAY0013', 'PLAY0560', 'PLAY0561', 'PLAY0562', 'PLAY0563', 'PLAY0564', 'PLAY0565', 'PLAY0653', 'PLAY0566', 'PLAY0567', 'PLAY0568', 'PLAY0569', 'PLAY0570', 'PLAY0571', 'PLAY0572', 'PLAY0577', 'PLAY0581', 'PLAY0582', 'PLAY0583', 'PLAY0584', 'PLAY0586', 'PLAY0587', 'PLAY0590', 'PLAY0591', 'PLAY0592', 'PLAY0593', 'PLAY0594', 'PLAY0595', 'PLAY0558', 'PLAY0514', 'PLAY0515', 'PLAY0518', 'PLAY0519', 'PLAY0341', 'PLAY0342', 'PLAY0343', 'PLAY0344', 'PLAY0391', 'PLAY0453', 'PLAY0538', 'PLAY0539', 'PLAY0543', 'PLAY0545']
            .map(code => vectors.cases.find(vector => vector.diagnostics.some(diagnostic => diagnostic.startsWith(code + '@')))!.source);
        issues.push(...authoring.flatMap(lines => validateLines(lines)));
        const identity = ['identity', '  department String from claim "department"'];
        issues.push(...validateLines(identity, { application: mergeSymbols(scanDocument(['identity'])) }));
        issues.push(...validateLines(['concept C : String', '  description invalid', '  description', '    ```text', '    ```', '  description "One"', '  description "Two"']));
        // Catalogue entries for C#-only checks are not promises of local TypeScript validation.
        // A host may supply them; the adapter must preserve their codes rather than drop them.
        const nativeOnly = ['PLAY0650', 'PLAY0651', 'PLAY0652', 'PLAY0602', 'PLAY0603', 'PLAY0604', 'PLAY0605', 'PLAY0606', 'PLAY0559', 'PLAY0530', 'PLAY0531', 'PLAY0532', 'PLAY0533', 'PLAY0534', 'PLAY0535', 'PLAY0536', 'PLAY0537',
            'PLAY0345', 'PLAY0346', 'PLAY0347', 'PLAY0348', 'PLAY0540', 'PLAY0541', 'PLAY0542', 'PLAY0544', 'PLAY0546',
            'PLAY0661', 'PLAY0666', 'PLAY0573', 'PLAY0574', 'PLAY0575', 'PLAY0576', 'PLAY0578', 'PLAY0579', 'PLAY0580', 'PLAY0585', 'PLAY0588', 'PLAY0589',
            'PLAY0622', 'PLAY0623', 'PLAY0624', 'PLAY0626', 'PLAY0627', 'PLAY0628', 'PLAY0629', 'PLAY0630', 'PLAY0631', 'PLAY0632', 'PLAY0654', 'PLAY0655', 'PLAY0656'];
        // Malformed screen composition is a parse error both compilers report.
        issues.push(...validateLines(['exposure Shell', 'instance']));
        issues.push(...validateLines(['module M'], { compilerDiagnostics: nativeOnly.map(code => ({ code, severity: 'warning', message: 'C# finding', location: { line: 1, column: 1 } })) }));
        const routes = ['PLAY0660', 'PLAY0662', 'PLAY0663', 'PLAY0664', 'PLAY0665', 'PLAY0547', 'PLAY0548', 'PLAY0549', 'PLAY0550', 'PLAY0551'].map(code => vectors.cases.find(vector => vector.diagnostics.some(diagnostic => diagnostic.startsWith(code + '@')))!.source);
        issues.push(...routes.flatMap(lines => validateLines(lines, { compilerDiagnostics: parse(lines.join('\n')).diagnostics })));
        const authorizationRefusal = [
            'policy Access',
            '  require authenticated',
            'module M',
            '  feature F',
            '    slice Automation S',
            '      event Approved',
            '      command Claim',
            '        authorize Access',
            '      reaction R',
            '        when Approved',
            '          invokes Claim',
            '            on refused by authorization',
            '              acknowledge',
        ];
        issues.push(...validateLines(authorizationRefusal));
        // Timeline findings come from the compiler, which reads the whole document's order.
        const timelines = [
            ['module M', '  feature F', '    slice StateView View', '      projection P', '        from E', '    slice StateChange Write', '      event E'],
            ['module M', '  feature A', '    slice StateView ViewA', '      event EA', '      projection PA', '        from EB', '  feature B', '    slice StateView ViewB', '      event EB', '      projection PB', '        from EA'],
        ];
        issues.push(...timelines.flatMap(lines => validateLines(lines, { compilerDiagnostics: parse(lines.join('\n')).diagnostics })));
        const dependencies = [
            ['module A', '  depends on C', '  depends on C', '  depends on Nowhere', '  feature F', '    slice StateView V', '      projection P', '        from E', 'module B', '  feature G', '    slice StateChange W', '      event E', 'module C', '  depends on A'],
            ['module A', '  depends on Shared.F', 'module B', '  feature Shared', '    feature F', 'module C', '  feature Shared', '    feature F'],
        ];
        issues.push(...dependencies.flatMap(lines => validateLines(lines, { compilerDiagnostics: parse(lines.join('\n')).diagnostics })));
        const publicContracts = ['import Outside.Arrived from "other/store"', 'module Sales', '  feature Orders', '    slice StateChange Facts', '      event Changed', '      public event Published', '      public event Second'];
        const publicUses = [
            ['StateChange', 'command Send\n        produces Published'],
            ['Automation', 'reaction Receive\n        when Arrived\n          produces Published'],
            ['Translate', 'direction outbound'],
            ['Translate', 'direction outbound\n      reaction Send\n        when Published\n          produces Changed'],
            ['Translate', 'direction inbound\n      reaction Receive\n        when Changed\n          produces Arrived'],
            ['Translate', 'direction outbound\n      capture Data\n        append Published'],
            ['Translate', 'reaction Receive\n        when Arrived\n          produces Changed'],
            ['StateView', 'projection Publisher => Published\n        from Changed\n          id = $eventSourceId'],
            ['Translate', 'capture Feed\n        source events\n          from Arrived\n        key id'],
            ['Translate', 'direction inbound\n      capture Feed\n        source events\n        key id'],
        ];
        issues.push(...publicUses.flatMap(([kind, body]) => validateLines([...publicContracts, `    slice ${kind} Transfer`, ...(`      ${body}`).split('\n')])));
        issues.push(...validateLines(['import Invalid from " "', 'module M', '  feature F', '    slice Translate S', '      direction sideways', '      public event E from " "']));
    });

    it('should give every issue it reports a code', () => {
        issues.filter((issue) => !issue.code).should.be.empty;
    });

    it('should report each condition with the code the compiler reports it with', () => {
        const reported = new Set(issues.map((issue) => issue.code));
        // Keep the old constant in the exported API, but admitted v7 responses no longer emit it.
        Object.values(diagnosticCodes).filter(code => code !== diagnosticCodes.unavailableResponseExecution)
            .forEach((code) => reported.has(code).should.equal(true, `Missing diagnostic ${code}`));
        reported.has(diagnosticCodes.unavailableResponseExecution).should.be.false;
    });
});
