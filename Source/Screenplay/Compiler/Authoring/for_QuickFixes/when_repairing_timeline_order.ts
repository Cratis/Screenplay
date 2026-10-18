// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import vectors from '../../Conformance/authored-order.json';
import { authoredOrderOf, authoredOriginsOf } from '../../Files/AuthoredOrder';
import { timelineOrderDiagnostics, timelineOrderFindings } from '../../Files/TimelineOrder';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { applyQuickFixEdits, findQuickFixes, prepareQuickFixes } from '../QuickFixes';

const source = 'module M\n  feature F\n    slice StateView View\n      projection P\n        from E\n    slice StateChange Write\n      event E\n';

describe('when repairing timeline order', () => {
    it('should move the producer before its consumer without altering either block', () => {
        const fixes = findQuickFixes(source, { line: 5, diagnosticCode: 'PLAY0516' });
        expect(fixes).toHaveLength(1);
        const after = applyQuickFixEdits(source, fixes[0].edits)!;
        expect(after.indexOf('slice StateChange Write')).toBeLessThan(after.indexOf('slice StateView View'));
        expect(compileApplication(new Map([['application.play', after]])).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0516')).toEqual([]);
    });

    it('should keep every line intact when the document has no final newline', () => {
        const unterminated = 'module M\n  feature F\n    slice StateView View\n      projection P\n        from E\n    slice StateChange Write\n      event E\n    slice StateView Other\n      readmodel O';
        const fixes = findQuickFixes(unterminated, { line: 5, diagnosticCode: 'PLAY0516' });
        expect(fixes.length).toBeGreaterThan(0);
        for (const fix of fixes) {
            const after = applyQuickFixEdits(unterminated, fix.edits)!;
            expect(after.split(/\r?\n/).map(line => line.trim()).filter(line => line.length > 0).sort()).toEqual(unterminated.split('\n').map(line => line.trim()).sort());
        }
    });

    it('should offer range requests independently and deduplicate the same finding', () => {
        const fixes = prepareQuickFixes(source)([{ line: 5, diagnosticCode: 'PLAY0516' }, { line: 5, diagnosticCode: 'PLAY0516' }]);
        expect(fixes).toHaveLength(1);
    });

    it('should mirror the C# order for shared imported reader and reducer fixtures', () => {
        let checked = 0;
        for (const vector of vectors.cases) {
            if (!('repairs' in vector)) continue;
            const documents = new Map(Object.entries(vector.files) as [string, string][]);
            for (const reference of vector.repairs!) {
                const fixes = findQuickFixes(documents.get(reference.path)!, { path: reference.path, documents, line: reference.line, diagnosticCode: 'PLAY0516' });
                expect(fixes).toHaveLength(1);
                const after = new Map(documents);
                for (const path of new Set(fixes[0].edits.map(edit => edit.path ?? reference.path))) {
                    after.set(path, applyQuickFixEdits(after.get(path)!, fixes[0].edits.filter(edit => (edit.path ?? reference.path) === path))!);
                }
                const compiled = compileApplication(after);
                expect([...authoredOrderOf(compiled.value).keys()].map(key => JSON.parse(key))).toEqual(reference.order);
                expect(compiled.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0516')).toHaveLength(1);
                checked++;
            }
        }
        expect(checked).toBe(2);
    });

    it('should retain a glob when a producer pin suffices', () => {
        const documents = new Map([['application.play', 'module M\n  feature F\n    import "steps/*.play"\n'], ['steps/a.play', 'slice StateView View\n  projection P\n    from E\n'], ['steps/z.play', 'slice StateChange Write\n  event E\n']]);
        const fix = findQuickFixes(documents.get('steps/a.play')!, { path: 'steps/a.play', documents, line: 3, diagnosticCode: 'PLAY0516' })[0];
        expect(fix).toBeDefined();
        const text = applyQuickFixEdits(documents.get('application.play')!, fix.edits)!;
        expect(text).toContain('import "steps/z.play"');
        expect(text).toContain('import "steps/*.play"');
    });

    it('should expose additive physical origins without changing diagnostic output or authored ranks', () => {
        const documents = new Map([['application.play', 'module M\n  feature F\n    import "steps/*.play"\n'], ['steps/a.play', 'slice StateView View\n  projection P\n    from E\n'], ['steps/z.play', 'slice StateChange Write\n  event E\n']]);
        const compiled = compileApplication(documents);
        const order = [...authoredOrderOf(compiled.value)];
        const findings = timelineOrderFindings(compiled.value);
        expect(findings.map(finding => finding.diagnostic)).toEqual(timelineOrderDiagnostics(compiled.value));
        expect(findings[0]).toMatchObject({ consumerScope: ['M', 'F', 'View'], producerScope: ['M', 'F', 'Write'] });
        const origins = authoredOriginsOf(compiled.value).get(JSON.stringify(['M', 'F', 'View']))!;
        expect(origins.map(step => [step.path, step.node.kind, step.targetPath])).toEqual([
            ['application.play', 'ModuleSyntax', undefined], ['application.play', 'FeatureSyntax', undefined],
            ['application.play', 'FileImportSyntax', 'steps/a.play'], ['steps/a.play', 'SliceSyntax', undefined],
        ]);
        expect([...authoredOrderOf(compiled.value)]).toEqual(order);
    });

    it('should expand unsafe glob blocks without losing rankless documents or comments', () => {
        const documents = new Map([
            ['application.play', 'module M\r\n  feature F\r\n    import "left/*.play" // left\r\n    // between imports\r\n    import "right/*.play" // right\r\n'],
            ['left/a.play', 'slice StateChange First\n  event X\n'],
            ['left/b.play', 'slice StateView Consumer\n  projection P\n    from E\n'],
            ['right/y.play', 'slice StateChange Producer\n  event E\n  projection Q\n    from X\n'],
            ['right/z.play', '// no timeline declaration\n'],
        ]);
        const fix = findQuickFixes(documents.get('left/b.play')!, { path: 'left/b.play', documents, line: 3, diagnosticCode: 'PLAY0516' })[0];
        expect(fix).toBeDefined();
        expect(fix.title).toContain('new files need explicit imports');
        const after = applyQuickFixEdits(documents.get('application.play')!, fix.edits)!;
        expect(after.match(/import "[^"]+"/g)).toEqual(['import "left/a.play"', 'import "right/y.play"', 'import "left/b.play"', 'import "right/z.play"']);
        for (const comment of ['// left', '// between imports', '// right']) expect(after.split(comment)).toHaveLength(2);
        expect(after.replaceAll('\r\n', '')).not.toContain('\n');
        const compiled = compileApplication(new Map(documents).set('application.play', after));
        expect(compiled.documents.map(document => document.path).sort()).toEqual([...documents.keys()].sort());
        expect(compiled.diagnostics.filter(diagnostic => ['PLAY0516', 'PLAY0517'].includes(diagnostic.code))).toEqual([]);
    });

    it('should preserve CRLF when moving intact sibling blocks and refuse own-sub-feature moves', () => {
        const crlf = source.replaceAll('\n', '\r\n');
        const fix = findQuickFixes(crlf, { line: 5, diagnosticCode: 'PLAY0516' })[0];
        const after = applyQuickFixEdits(crlf, fix.edits)!;
        expect(after.replaceAll('\r\n', '')).not.toContain('\n');
        const nested = source.replace('    slice StateChange Write', '    feature Child\n      slice StateChange Write').replace('      event E', '        event E');
        expect(findQuickFixes(nested, { line: 5, diagnosticCode: 'PLAY0516' })).toEqual([]);
    });

    it('should refuse cycles, unknown order and a stale application snapshot', () => {
        expect(findQuickFixes(source, { line: 4, diagnosticCode: 'PLAY0516' })).toEqual([]);
        expect(findQuickFixes(source, { line: 5, diagnosticCode: 'PLAY0517' })).toEqual([]);
        expect(findQuickFixes(source, { line: 5, diagnosticCode: 'PLAY0516', path: 'application.play', documents: new Map([['application.play', source + '// changed']]) })).toEqual([]);
        const cycle = source + '      projection Q\n        from X\n';
        expect(findQuickFixes(cycle.replace('      projection P', '      event X\n      projection P'), { line: 6, diagnosticCode: 'PLAY0516' })).toEqual([]);
    });
});
