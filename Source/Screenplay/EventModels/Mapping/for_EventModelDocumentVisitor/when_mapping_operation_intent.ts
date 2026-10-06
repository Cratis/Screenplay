// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { toCommand } from '../toCommand';
import { SliceScope } from '../SliceScope';
import { SchemaSynthesizer } from '../../Schemas/SchemaSynthesizer';

const source = 'system Mailer\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n        recipient String optional\n        compensate\n      event Recorded\n      command C\n        produces Recorded\n        produces Send\n      specification T\n        given operation Send fails\n        when C\n        then operation Send\n          recipient = "Ada"\n        then compensated Send\n';

describe('when mapping operation intent', () => {
    it('should not fabricate operation event cards or event assertion identities', () => {
        const application = parse(source);
        expect(application.success).toBe(true);
        const document = toEventModelDocument(application.value, 'Example');
        const slice = document.collections[0].modules[0].features[0].slices[0];
        expect(slice.events.map(event => event.name)).toEqual(['Recorded']);
        expect(slice.specifications[0].given).toEqual([]);
        expect(slice.specifications[0].thenEvents).toEqual([]);
        expect(slice.description).toBe('');
        expect(slice.command?.logicDescription).toContain('not admitted by any supported executable model (ESM) version yet (PLAY0268) (#301)');
        expect(slice.command?.logicDescription).toContain('Send');
        expect(slice.command?.logicDescription).toContain('recipient');
        expect(slice.command?.logicDescription).toContain('Ada');
        expect(slice.command?.logicDescription).toContain('1. Event: Recorded');
        expect(slice.command?.logicDescription).toContain('2. Operation: Send');
        expect(slice.command?.logicDescription).toContain('execute: not declared');
        expect(slice.command?.logicDescription).toContain('compensate: pending');
        expect(slice.command?.logicDescription).toContain('T: given operation Send fails');
        expect(slice.command?.logicDescription).toContain('T: then operation Send\n  recipient = "Ada"');
        expect(slice.command?.logicDescription).toContain('T: then compensated Send');
        expect(slice.command?.logicDescription).not.toContain('SpecificationOperationSyntax');
        expect(slice.command?.schema.properties ?? {}).not.toHaveProperty('recipient');
    });
    it('should distinguish absent phases from authored intent-only and attached phases', () => {
        const prefix = 'system Mailer\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n';
        for (const [body, state] of [
            ['', 'not declared'],
            ['        execute\n        compensate\n', 'pending'],
            ['        execute\n          description "Do it"\n        compensate\n          description "Undo"\n', 'pending'],
            ['        execute\n          implementation\n            hint "Guide"\n        compensate\n          implementation\n            hint "Undo"\n', 'pending'],
            ['        execute\n          file Send.cs\n        compensate\n          file Send.cs\n', 'file Send.cs'],
            ['        execute\n          ```csharp\n          return;\n          ```\n        compensate\n          ```csharp\n          return;\n          ```\n', 'inline csharp']
        ]) {
            const application = parse(prefix + body + '      command C\n        produces Send\n');
            expect(application.success).toBe(true);
            const details = toEventModelDocument(application.value, 'Intent').collections[0].modules[0].features[0].slices[0].command!.logicDescription;
            expect(details).toContain(`execute: ${state}`);
            expect(details).toContain(`compensate: ${state}`);
        }
    });
    it('should display mapping paths, escaped strings, lists and composites as safe authored text', () => {
        const source = 'system Mailer\ntype Body\n  text String\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n        recipient String\n        body Body\n        tags String[] optional\n        @execute String optional\n        compensate\n      command C\n        text String\n        produces Send\n          recipient = text\n          body = { "text": "Ada" }\n          tags = ["a", "b"]\n          execute = "<img src=x onerror=alert(1)>"\n      specification T\n        given operation Send fails\n        when C\n          text = "Ada"\n        then operation Send\n          body = { "text": "Ada" }\n          tags = ["a", "b"]\n          execute = "A\\\\\\"B"\n        then compensated Send\n';
        const application = parse(source);
        expect(application.diagnostics.filter(diagnostic => diagnostic.severity === 'error')).toEqual([]);
        expect(application.success).toBe(true);
        const slice = toEventModelDocument(application.value, 'Intent').collections[0].modules[0].features[0].slices[0];
        const details = slice.command!.logicDescription;
        expect(details).toContain('recipient = text');
        expect(details).toContain('body = { "text": "Ada" }');
        expect(details).toContain('tags = ["a", "b"]');
        expect(details).toContain('execute: String optional');
        expect(details).toContain('execute = "A\\\\\\"B"');
        expect(details).toContain('&lt;img src=x onerror=alert(1)&gt;');
        expect(details).not.toContain('<img');
        expect(details).not.toContain('PathExpressionSyntax');
        expect(slice.events).toEqual([]);
        expect(slice.specifications[0].thenEvents).toEqual([]);
    });
    it('should preserve the old command-only mapper when no assembled inventory is supplied', () => {
        const application = parse(source).value;
        const command = application.modules[0].features[0].slices[0].commands[0];
        expect(toCommand(command, SliceScope.module('M').feature('F').slice('S'), new SchemaSynthesizer(application)).logicDescription).toBe('');
    });
    it('should describe full typed operation contracts, phase attachments and system intent in command details', () => {
        const source = readFileSync(new URL('../../../../../Documentation/screenplay/fixtures/operations.play', import.meta.url), 'utf8');
        const application = parse(source);
        expect(application.success).toBe(true);
        const document = toEventModelDocument(application.value, 'Operations');
        const slices = document.collections[0].modules[0].features[0].slices;
        const details = slices[0].command!.logicDescription;
        expect(details).toContain('recipient: EmailAddress');
        expect(details).toContain('tags: String[] optional');
        expect(details).toContain('Uses Mailer — Sends email outside this application');
        expect(details).toContain('file Adapters/OpenCostCenter.cs');
        expect(details).toContain('hint: Use the existing accounting adapter');
        expect(details).toContain('Close the provisional cost center');
        expect(details).toContain('owner.email');
        expect(slices[1].command!.logicDescription).toContain('Operation NotifyAccounting');
        expect(slices[0].events.map(event => event.name)).toEqual(['ProjectRegistered']);
        const inline = parse(source.replace('file Adapters/OpenCostCenter.cs', '```csharp\n            return;\n            ```'));
        expect(inline.success).toBe(true);
        expect(toEventModelDocument(inline.value, 'Inline').collections[0].modules[0].features[0].slices[0].command!.logicDescription).toContain('inline csharp');
    });
    it('should describe ambiguous command productions without choosing an operation or event', () => {
        const application = parse('system Mailer\nmodule M\n  feature F\n    slice StateChange A\n      operation Send\n        uses Mailer\n    slice StateChange B\n      event Send\n    slice StateChange Ask\n      command C\n        produces Send\n');
        const slice = toEventModelDocument(application.value, 'Ambiguous').collections[0].modules[0].features[0].slices[2];
        expect(slice.command!.logicDescription).toContain('ambiguous declaration kind; no operation selected');
        expect(slice.events).toEqual([]);
    });
    it('should not fabricate event identities from mixed-kind ambiguous productions', () => {
        const application = parse('system Mailer\nmodule M\n  feature F\n    slice StateChange A\n      operation Send\n        uses Mailer\n    slice StateChange B\n      operation Send\n        uses Mailer\n    slice Automation Observe\n      reaction R\n        every 1 day\n          produces Send\n');
        expect(application.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0497');
        const document = toEventModelDocument(application.value, 'Example');
        expect(document.collections[0].modules[0].features[0].slices[2].events).toEqual([]);
    });
    it('should exclude even invalid reaction operations before constructing produced event cards', () => {
        const application = parse(source + '    slice Automation Observe\n      reaction R\n        every 1 day\n          produces Send\n');
        expect(application.success).toBe(false);
        const document = toEventModelDocument(application.value, 'Example');
        expect(document.collections[0].modules[0].features[0].slices[1].events).toEqual([]);
    });
});
