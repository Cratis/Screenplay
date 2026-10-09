// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseForAuthoring } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { specialCategories } from '../Parsing/ConceptComplianceParser';
import { applyQuickFixEdits, findQuickFixes } from '../Authoring/QuickFixes';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const code = (source: string) => parse(source).diagnostics.map(diagnostic => diagnostic.code);

describe('when parsing compliance markers', () => {
    it.each([['pii', 'pii'], ['personal', 'pii'], ['secret', 'sensitive']])('should accept %s without changing its wire identity', (marker, wire) => {
        const result = parse(`concept Value : String ${marker}`);
        expect(result.diagnostics).toEqual([]);
        expect(result.value.concepts[0].attributes[0].name).toBe(wire);
    });
    it.each(['@pii', 'sensitive', '@sensitive', '@pii @sensitive'])('should report one information per legacy line %s', marker => {
        expect(parse(`concept Value : String ${marker}`).diagnostics).toMatchObject([{ code: 'PLAY0560', severity: 'information' }]);
    });
    it.each(['@encrypted', 'pi', '@personal', '@secret'])('should reject unknown marker %s', marker => {
        expect(code(`concept Value : String ${marker}`)).toEqual(['PLAY0561']);
    });
    it.each(['subject', 'namespace', 'global'])('should retain explicit scope %s', scope => {
        const result = parse(`concept Key : String secret\n  secret scope ${scope}`);
        expect(result.diagnostics).toEqual([]);
        expect(result.value.concepts[0].attributes[0].scope).toBe(scope);
    });
    it.each(specialCategories)('should retain special category %s alongside criminal data', category => {
        const result = parse(`concept Value : String personal\n  personal special ${category}\n  personal criminal`);
        expect(result.diagnostics).toEqual([]);
        expect(result.value.concepts[0].attributes[0]).toMatchObject({ name: 'pii', specialCategory: category, criminal: true });
    });
    it.each([
        ['pii', 'pii scope subject', 'PLAY0562'],
        ['secret', 'secret scope tenant', 'PLAY0562'],
        ['secret', 'secret scope subject\n  secret scope global', 'PLAY0563'],
        ['pii', 'pii special unknown', 'PLAY0565'],
        ['pii', 'pii special health\n  pii special genetic', 'PLAY0566'],
        ['', 'pii special health', 'PLAY0012'],
        ['', 'pii criminal', 'PLAY0012'],
        ['secret', 'secret special health', 'PLAY0565'],
        ['pii', 'pii criminal extra', 'PLAY0565'],
        ['pii', 'pii reason missing-quotes', 'PLAY0010'],
        ['pii', 'pii reason "first"\n  personal reason "second"', 'PLAY0013'],
        ['pii', 'unknown reason "note"', 'PLAY0561'],
        ['pii', '@personal special health', 'PLAY0561'],
    ])('should reject invalid settings %s / %s', (marker, body, expected) => {
        expect(code(`concept Value : String ${marker}`.trimEnd() + `\n  ${body}`)).toEqual([expected]);
    });
    it('should warn when combined markers make secret scope ineffective', () => {
        expect(parse('concept Value : String pii secret\n  secret scope global').diagnostics).toMatchObject([{ code: 'PLAY0564', severity: 'warning' }]);
    });
    it('should preserve old syntax bytes when no new settings were declared', () => {
        expect(toSyntaxJson(parse('concept Value : String pii').value)).not.toHaveProperty('concepts.0.attributes.0.criminal');
    });
});

describe('when migrating compliance spellings', () => {
    const source = '// café @pii\r\nconcept Value : String @pii @sensitive // keep\r\n  sensitive reason "Keep @pii and sensitive verbatim"';
    it('should offer verified per-line and document fixes without moving legal text', () => {
        const fixes = findQuickFixes(source, { line: 2, diagnosticCode: DiagnosticCodes.LegacyComplianceMarker });
        expect(fixes.map(fix => fix.scope)).toEqual(['occurrence', 'document']);
        const expected = source.replace('String @pii @sensitive', 'String pii secret').replace('  sensitive reason', '  secret reason');
        const document = fixes.find(fix => fix.scope === 'document')!;
        expect(applyQuickFixEdits(source, document.edits)).toBe(expected);
        expect(toSyntaxJson(parseForAuthoring(expected).value)).toEqual(toSyntaxJson(parseForAuthoring(source).value));
        expect(code(expected)).toEqual([]);
        const occurrence = fixes.find(fix => fix.scope === 'occurrence')!;
        expect(code(applyQuickFixEdits(source, occurrence.edits)!)).toEqual(['PLAY0560']);
    });
    it('should expose the reason line separately in a range request', () => {
        const fixes = findQuickFixes(source);
        expect(fixes).toHaveLength(1);
        const prepared = findQuickFixes(source, { line: 3, diagnosticCode: 'PLAY0560' });
        expect(applyQuickFixEdits(source, prepared[0].edits)).toBe(source.replace('  sensitive reason', '  secret reason'));
    });
    it('should refuse erroneous documents and leave canonical aliases alone', () => {
        expect(findQuickFixes('concept Value : String @encrypted')).toEqual([]);
        expect(findQuickFixes('concept Value : String personal')).toEqual([]);
    });
});
