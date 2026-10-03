// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from './given/the_constructs_document';

const fixture = readFileSync(new URL('../../../../../Documentation/screenplay/fixtures/generated-responses.play', import.meta.url), 'utf8');

describe('when mapping syntax-only responses', () => {
    it('should exclude generated values from request schemas and disclose response details', () => {
        const document = toEventModelDocument(parse(fixture).value, 'Projects');
        const slice = slice_named(document, 'Register');
        expect(Object.keys(slice.command!.schema.properties ?? {})).toEqual(['name']);
        expect(slice.command!.schema.required).toEqual(['name']);
        expect(slice.command!.logicDescription).toContain('Generated values (not request inputs)');
        expect(slice.command!.logicDescription).toContain('receiptId: ReceiptId = receiptId');
        expect(slice.command!.logicDescription).toContain('execution unavailable');
        expect(slice.command!.stateSchema).toEqual({});
        expect(slice.events.map(event => event.name)).toEqual(['ProjectRegistered']);
    });
    it('should leave old command output unchanged when additive members are absent or defaulted', () => {
        const legacy = fixture.replaceAll(' generated', '').replace(/ {8}returns\n(?: {10}.*\n)+/g, '').replace(/ {8}returns projectId\n/g, '').replace(/ {6}specification[\s\S]*?(?= {4}slice|$)/g, '');
        const parsed = parse(legacy).value;
        const before = toEventModelDocument(parsed, 'Projects');
        for (const module of parsed.modules) for (const feature of module.features) for (const slice of feature.slices) for (const command of slice.commands) {
            Object.assign(command, { response: null });
            for (const property of command.properties) Object.assign(property, { isGenerated: false });
        }
        expect(toEventModelDocument(parsed, 'Projects')).toEqual(before);
        expect(slice_named(before, 'Register').command!.logicDescription).toBe('');
    });
});
