// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { a_parsed_document } from './given/a_parsed_document';
import { EventVisibility } from '../Syntax/EventVisibility';
import { TranslationDirection } from '../Syntax/TranslationDirection';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { decodeExactSyntaxJson } from '../Syntax/StrictSyntaxJson';
import { InvalidSyntaxJson } from '../Syntax/InvalidSyntaxJson';
import { ApplicationSyntax } from '../Syntax/Structure';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { ImportSyntax } from '../Syntax/Declarations';
import { publicEventMetadataError } from '../Syntax/PublicEventInvariants';
import { validatePublicEventMetadata } from '../Parsing/PublicEventMetadataValidator';
import { ParserContext } from '../Parsing/ParserContext';
import { LineReader } from '../Parsing/LineReader';

const document = (...body: string[]) => a_parsed_document('module Integration', '    feature Contracts', ...body.map(line => `        ${line}`));

describe('when authoring public event metadata', () => {
    it('should preserve opaque origins and explicit generation markers', () => {
        const result = a_parsed_document('import Billing.Issued from "billing/Issued.play"', 'module Integration', '    feature Contracts', '        slice Translate Publish', '            direction outbound', '            public\tevent Issued generation 1 from "billing\\"contracts\\\\v1"', '                number String');
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0598']);
        const slice = result.value.modules[0].features[0].slices[0];
        [slice.direction, slice.events[0].visibility, slice.events[0].origin, slice.events[0].generation, slice.events[0].hasGenerationMarker].should.deep.equal([TranslationDirection.Outbound, EventVisibility.Public, 'billing"contracts\\v1', 1, true]);
        [result.value.imports[0].visibility, result.value.imports[0].origin].should.deep.equal([EventVisibility.Public, 'billing/Issued.play']);
    });

    it('should infer public visibility from an event origin', () => {
        const result = document('slice Translate Receive', '    direction\tinbound', '    event Issued from "billing"', '        number String');
        result.diagnostics.should.deep.equal([]);
        result.value.modules[0].features[0].slices[0].events[0].visibility!.should.equal(EventVisibility.Public);
    });

    it('should leave legacy structural bytes unchanged', () => {
        const result = document('slice StateChange Register', '    event Registered', '        number String');
        const json = JSON.stringify(toSyntaxJson(result.value));
        json.should.not.contain('visibility');
        json.should.not.contain('origin');
        json.should.not.contain('direction');
    });

    it('should reject invalid and duplicate direction directives', () => {
        for (const body of [ ['slice StateChange Register', '    direction inbound'], ['slice Translate Receive', '    direction sideways'], ['slice Translate Receive', '    direction inbound', '    direction outbound'] ]) {
            document(...body).diagnostics.map(diagnostic => diagnostic.code).should.contain('PLAY0027');
        }
    });

    it('should use native whitespace without normalizing origin text', () => {
        a_parsed_document('import Billing.Issued from "\u0085"').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0005', 'PLAY0005']);
        const result = a_parsed_document('import Billing.Issued from "\ufeff"');
        result.diagnostics.should.deep.equal([]);
        result.value.imports[0].origin!.should.equal('\ufeff');
    });

    it('should reject blank origins and malformed declarations', () => {
        for (const declaration of ['public event Issued from " "', 'event Issued from billing', 'public event Issued generation 2 from ""', 'public command Issue']) {
            document('slice Translate Receive', `    ${declaration}`).diagnostics.map(diagnostic => diagnostic.code).should.contain('PLAY0018');
        }
    });

    it('should enforce native authoring invariants on programmatic metadata', () => {
        const location = { line: 1, column: 1 };
        const cases = [
            { kind: 'EventSyntax', visibility: 'Unknown' },
            { kind: 'EventSyntax', visibility: null },
            { kind: 'EventSyntax', origin: ' ' },
            { kind: 'ImportSyntax', visibility: 'Private', origin: 'billing' },
            { kind: 'ImportSyntax', visibility: 'Public' },
            { kind: 'SliceSyntax', type: 'StateChange', direction: 'Inbound' },
            { kind: 'SliceSyntax', type: 'Translate', direction: 'Unknown' },
        ];
        for (const metadata of cases) {
            const node = { ...metadata, location } as SyntaxNode;
            (publicEventMetadataError(node) !== undefined).should.be.true;
            (() => toSyntaxJson(node)).should.throw(InvalidSyntaxJson);
        }
        const result = a_parsed_document('import Billing.Issued from " "', 'module M', '  feature F', '    slice Translate S', '      public event E from " "');
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0005', 'PLAY0018', 'PLAY0005', 'PLAY0018', 'PLAY0603']);
        const application = document('slice Translate S', '    event E').value;
        const module = application.modules[0];
        const feature = module.features[0];
        const slice = feature.slices[0];
        const context = new ParserContext(new LineReader([]));
        validatePublicEventMetadata({ ...application, modules: [{ ...module, features: [{ ...feature, slices: [{ ...slice, type: 'StateChange', direction: TranslationDirection.Inbound }] }] }] }, context);
        context.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0027']);
    });

    it('should validate authored transport before hiding default metadata', () => {
        const result = document('slice Translate Receive', '    public event Issued', '        number String');
        const event = result.value.modules[0].features[0].slices[0].events[0];
        const privateEventWithOrigin = { ...event, visibility: EventVisibility.Private, origin: 'billing' };
        const publicImportWithoutOrigin: ImportSyntax = { kind: 'ImportSyntax', qualifiedName: 'Billing.Issued', visibility: EventVisibility.Public, origin: null, location: event.location };
        (() => toSyntaxJson(privateEventWithOrigin)).should.throw(InvalidSyntaxJson);
        (() => toSyntaxJson(publicImportWithoutOrigin)).should.throw(InvalidSyntaxJson);
        const exact: ApplicationSyntax = { ...result.value, sourceOptions: { numericMode: 'exact' } };
        const json = JSON.stringify(toSyntaxJson(exact));
        const restored = decodeExactSyntaxJson(json) as ApplicationSyntax;
        restored.modules[0].features[0].slices[0].events[0].visibility!.should.equal(EventVisibility.Public);
        const normalized = JSON.stringify(toSyntaxJson(restored));
        JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(normalized))).should.equal(normalized);
    });
});
