// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '@cratis/screenplay-compiler';
import { AnalysisDiagnostic } from '../../response-analysis';
import { ValidationIssue, validateLines } from '../../validation';

const compilerDiagnostic = (code: string, line: number, column: number, message: string, severity: AnalysisDiagnostic['severity'] = 'warning'): AnalysisDiagnostic =>
    ({ code, message, severity, location: { line, column } } as AnalysisDiagnostic);

const tuple = (issue: ValidationIssue) => [issue.code, issue.severity, issue.message, issue.line, issue.startColumn, issue.endColumn];
const ofCode = (issues: ValidationIssue[], ...codes: string[]) => issues.filter(issue => codes.includes(issue.code ?? ''));

describe('when forwarding compiler diagnostics', () => {
    describe('and the caller supplies an empty diagnostic list', () => {
        const lines = ['command C', '  value Missing?'];

        it('should report the fallback analysis when nothing is supplied', () => {
            ofCode(validateLines(lines), DiagnosticCodes.LegacyOptionalSuffix).length.should.equal(1);
        });

        it('should report nothing from the compiler when an empty list is supplied', () => {
            ofCode(validateLines(lines, { compilerDiagnostics: [] }), DiagnosticCodes.LegacyOptionalSuffix).length.should.equal(0);
        });
    });

    describe('and the compiler reports the same unknown event twice', () => {
        const lines = ['command C', '  produces Ghost', '  produces Phantom'];
        const first = compilerDiagnostic(DiagnosticCodes.UnknownEvent, 2, 3, 'first');
        const repeated = compilerDiagnostic(DiagnosticCodes.UnknownEvent, 2, 12, 'second');
        const otherLine = compilerDiagnostic(DiagnosticCodes.UnknownEvent, 3, 3, 'third');

        it('should forward only one marker per line and drop the heuristic one on those lines', () => {
            ofCode(validateLines(lines, { compilerDiagnostics: [first, repeated, otherLine] }), DiagnosticCodes.UnknownEvent)
                .map(tuple).should.deep.equal([
                    [DiagnosticCodes.UnknownEvent, 'warning', 'first', 1, 3, 17],
                    [DiagnosticCodes.UnknownEvent, 'warning', 'third', 2, 3, 19],
                ]);
        });

        it('should suppress the heuristic check for the line under a conditional production', () => {
            const conditional = ['command C', '  produces when true == true', '    Ghost'];
            ofCode(validateLines(conditional, { compilerDiagnostics: [compilerDiagnostic(DiagnosticCodes.UnknownEvent, 3, 5, 'compiler')] }), DiagnosticCodes.UnknownEvent)
                .map(tuple).should.deep.equal([[DiagnosticCodes.UnknownEvent, 'warning', 'compiler', 2, 5, 10]]);
        });

        it('should still report the heuristic check when the compiler says nothing about the line', () => {
            ofCode(validateLines(['command C', '  produces Ghost'], { compilerDiagnostics: [] }), DiagnosticCodes.UnknownEvent)
                .map(issue => issue.message).should.deep.equal(["Unknown event type 'Ghost' — declare it in a slice or import it."]);
        });
    });

    describe('and the compiler reports an omitted production destination', () => {
        const omitted = (line: number) => compilerDiagnostic(DiagnosticCodes.OmittedProductionDestination, line, 3, `omitted on ${line}`, 'information');

        it('should forward a single marker for two diagnostics on one line without advice', () => {
            ofCode(validateLines(['command C', '  produces Recorded'], { compilerDiagnostics: [omitted(2), omitted(2)] }), DiagnosticCodes.OmittedProductionDestination)
                .map(tuple).should.deep.equal([
                    [DiagnosticCodes.OmittedProductionDestination, 'information', 'omitted on 2', 1, 3, 20],
                ]);
        });

        it('should keep the markers of different lines', () => {
            ofCode(validateLines(['command C', '  produces Recorded', '  produces Updated'], { compilerDiagnostics: [omitted(2), omitted(3)] }), DiagnosticCodes.OmittedProductionDestination)
                .map(issue => issue.line).should.deep.equal([1, 2]);
        });

        it('should not repeat the editor advice already given for that line', () => {
            const lines = ['command C', '  id Uuid identifier', '  produces Recorded'];
            const advice = ofCode(validateLines(lines, { compilerDiagnostics: [] }), DiagnosticCodes.OmittedProductionDestination);
            advice.map(issue => issue.line).should.deep.equal([2]);
            ofCode(validateLines(lines, { compilerDiagnostics: [omitted(3)] }), DiagnosticCodes.OmittedProductionDestination)
                .map(tuple).should.deep.equal(advice.map(tuple));
        });
    });

    describe('and the compiler reports a member type problem', () => {
        const lines = ['command C', '  value Missing'];
        const memberType = compilerDiagnostic(DiagnosticCodes.InvalidSemanticBinding, 2, 3, "Read model 'M' references unsupported type 'Missing'.", 'error');

        it('should forward the member type error', () => {
            ofCode(validateLines(lines, { compilerDiagnostics: [memberType] }), DiagnosticCodes.InvalidSemanticBinding)
                .map(tuple).should.deep.equal([[DiagnosticCodes.InvalidSemanticBinding, 'error', memberType.message, 1, 3, 16]]);
        });

        it('should drop the heuristic unknown type on that line only', () => {
            ofCode(validateLines(lines), DiagnosticCodes.UnknownType).length.should.equal(1);
            ofCode(validateLines(lines, { compilerDiagnostics: [memberType] }), DiagnosticCodes.UnknownType).length.should.equal(0);
            ofCode(validateLines(['command C', '  value Missing', '  other Absent'], { compilerDiagnostics: [memberType] }), DiagnosticCodes.UnknownType)
                .map(issue => issue.line).should.deep.equal([2]);
        });

        it('should not treat an unrelated message of that code as a member type problem', () => {
            const unrelated = compilerDiagnostic(DiagnosticCodes.InvalidSemanticBinding, 2, 3, 'Something else.', 'error');
            ofCode(validateLines(lines, { compilerDiagnostics: [unrelated] }), DiagnosticCodes.InvalidSemanticBinding).length.should.equal(0);
            ofCode(validateLines(lines, { compilerDiagnostics: [unrelated] }), DiagnosticCodes.UnknownType).length.should.equal(1);
        });
    });

    describe('and the compiler reports duplicates and identity detail types', () => {
        it('should forward a repeated identity or context block but not other repeated singular declarations', () => {
            const markers = (message: string) => ofCode(validateLines(['command C'], { compilerDiagnostics: [compilerDiagnostic(DiagnosticCodes.RepeatedSingularDeclarationAcrossFiles, 1, 1, message, 'error')] }), DiagnosticCodes.RepeatedSingularDeclarationAcrossFiles).length;
            markers('Another identity block exists.').should.equal(1);
            markers('Another context block exists.').should.equal(1);
            markers('Another persona block exists.').should.equal(0);
        });

        it('should forward a duplicate policy declaration but not other duplicate declarations of the compiler', () => {
            const markers = (message: string) => ofCode(validateLines(['command C'], { compilerDiagnostics: [compilerDiagnostic(DiagnosticCodes.DuplicateDeclaration, 1, 1, message, 'error')] }), DiagnosticCodes.DuplicateDeclaration).length;
            markers('Duplicate declaration of policy Admin.').should.equal(1);
            markers('Duplicate declaration of event Admin.').should.equal(0);
        });

        it('should forward an unknown type on an identity detail line and not elsewhere', () => {
            const lines = ['identity', '  department String from claim "department"', 'command C'];
            const markers = (line: number) => ofCode(validateLines(lines, { compilerDiagnostics: [compilerDiagnostic(DiagnosticCodes.UnknownType, line, 10, 'unknown', 'error')] }), DiagnosticCodes.UnknownType).filter(issue => issue.message === 'unknown').length;
            markers(2).should.equal(1);
            markers(3).should.equal(0);
        });
    });

    describe('and the compiler reports codes at the edge of a forwarded range', () => {
        const forwarded = (code: string) => ofCode(validateLines(['command C'], { compilerDiagnostics: [compilerDiagnostic(code, 1, 1, 'edge', 'error')] }), code).length;

        it('should forward every code of a range including both endpoints', () => {
            ['PLAY0045', 'PLAY0046', 'PLAY0482', 'PLAY0546', 'PLAY0890', 'PLAY0939', 'PLAY0489', 'PLAY0490', 'PLAY0499', 'PLAY0500', 'PLAY0507', 'PLAY0547', 'PLAY0551', 'PLAY0557', 'PLAY0607', 'PLAY0652', 'PLAY0654', 'PLAY0656', 'PLAY0720', 'PLAY0722', 'PLAY0741', 'PLAY0744', 'PLAY0955', 'PLAY0964', 'PLAY0982', 'PLAY1142']
                .map(forwarded).should.deep.equal(Array(26).fill(1));
        });

        it('should drop the codes adjacent to those ranges', () => {
            ['PLAY0508', 'PLAY0529', 'PLAY0889', 'PLAY0940', 'PLAY0044', 'PLAY0047', 'PLAY0340', 'PLAY0349', 'PLAY0657', 'PLAY0723', 'PLAY0742', 'PLAY0743', 'PLAY0745', 'PLAY0952', 'PLAY0956', 'PLAY0963', 'PLAY1141']
                .map(forwarded).should.deep.equal(Array(17).fill(0));
        });
    });

    describe('and the compiler reports a legacy optional suffix', () => {
        it('should cover the exact type spelling', () => {
            const lines = ['command C', '  value Missing?'];
            ofCode(validateLines(lines, { compilerDiagnostics: [compilerDiagnostic(DiagnosticCodes.LegacyOptionalSuffix, 2, 9, 'legacy')] }), DiagnosticCodes.LegacyOptionalSuffix)
                .map(tuple).should.deep.equal([[DiagnosticCodes.LegacyOptionalSuffix, 'warning', 'legacy', 1, 9, 17]]);
        });

        it('should cover the rest of the line for any other code', () => {
            const lines = ['command C', '  value Missing?'];
            ofCode(validateLines(lines, { compilerDiagnostics: [compilerDiagnostic(DiagnosticCodes.InvalidOptionalModifierOrder, 2, 9, 'order')] }), DiagnosticCodes.InvalidOptionalModifierOrder)
                .map(tuple).should.deep.equal([[DiagnosticCodes.InvalidOptionalModifierOrder, 'warning', 'order', 1, 9, 17]]);
        });
    });
});
