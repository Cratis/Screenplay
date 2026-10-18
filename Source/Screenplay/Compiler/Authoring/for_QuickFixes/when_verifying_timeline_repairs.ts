// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { parseForAuthoring } from '../../ScreenplayCompiler';
import { applyQuickFixEdits, findQuickFixes, prepareQuickFixes } from '../QuickFixes';
import { TimelineQuickFixContext } from '../TimelineQuickFixContext';
import { QuickFixCandidate } from '../QuickFixCandidate';

function slice(name: string, produces: readonly string[], consumes: readonly string[] = []): string {
    return `slice StateChange ${name}\n` + produces.map(event => `  event ${event}\n`).join('') + (consumes.length === 0 ? '' : `  projection ${name}Projection\n` + consumes.map(event => `    from ${event}\n`).join(''));
}
function files(parts: readonly [string, readonly string[], readonly string[]][], glob = true): Map<string, string> {
    return new Map([['application.play', 'module M\n  feature F\n' + (glob ? '    import "steps/*.play" // future files\n' : parts.map(([name]) => `    import "steps/${name}.play"\n`).join(''))],
        ...parts.map(([name, produces, consumes], index): [string, string] => [`steps/${name}.play`, slice(`S${index}`, produces, consumes)])]);
}
const referenceLine = (documents: ReadonlyMap<string, string>, path: string) => documents.get(path)!.split('\n').findIndex(line => line.trim() === 'from E') + 1;

describe('when verifying timeline repairs', () => {
    it('should pin the minimal safe prefix instead of introducing a new dependency', () => {
        const documents = files([['a', [], ['E']], ['b', ['X'], []], ['c', ['E'], ['X']]]);
        const fix = findQuickFixes(documents.get('steps/a.play')!, { documents, path: 'steps/a.play', line: referenceLine(documents, 'steps/a.play'), diagnosticCode: 'PLAY0516' })[0];
        expect(fix).toBeDefined();
        const text = applyQuickFixEdits(documents.get('application.play')!, fix.edits)!;
        expect(text.match(/import "[^"]+"/g)).toEqual(['import "steps/b.play"', 'import "steps/c.play"', 'import "steps/*.play"']);
        expect(text).toContain('// future files');
    });

    it('should move the consumer after the producer when moving the producer is unsafe', () => {
        const documents = files([['a', [], ['E']], ['b', ['X'], []], ['c', ['E'], ['X']]], false);
        const fix = findQuickFixes(documents.get('steps/a.play')!, { documents, path: 'steps/a.play', line: referenceLine(documents, 'steps/a.play'), diagnosticCode: 'PLAY0516' })[0];
        expect(fix).toBeDefined();
        const text = applyQuickFixEdits(documents.get('application.play')!, fix.edits)!;
        expect(text.match(/import "[^"]+"/g)).toEqual(['import "steps/b.play"', 'import "steps/c.play"', 'import "steps/a.play"']);
    });

    it.each([true, false])('should refuse when neither adjacent move is safe (glob=%s)', glob => {
        const documents = files([['a', ['Y'], ['E']], ['b', ['X'], []], ['c', [], ['Y']], ['d', ['E'], ['X']]], glob);
        expect(findQuickFixes(documents.get('steps/a.play')!, { documents, path: 'steps/a.play', line: referenceLine(documents, 'steps/a.play'), diagnosticCode: 'PLAY0516' })).toEqual([]);
    });

    it('should refuse pins whose literal file names contain glob metacharacters', () => {
        const documents = files([['a', [], ['E']], ['z[one]', ['E'], []]]);
        expect(findQuickFixes(documents.get('steps/a.play')!, { documents, path: 'steps/a.play', line: 3, diagnosticCode: 'PLAY0516' })).toEqual([]);
    });

    it('should resolve parent-relative pins from a nested importing document', () => {
        const documents = new Map([['application.play', 'module M\n  feature F\n    import "parts/imports.play"\n'], ['parts/imports.play', 'import "../steps/*.play"\n'], ['steps/a.play', slice('View', [], ['E'])], ['steps/z.play', slice('Write', ['E'])]]);
        const fix = findQuickFixes(documents.get('steps/a.play')!, { documents, path: 'steps/a.play', line: 3, diagnosticCode: 'PLAY0516' })[0];
        expect(fix.edits[0].path).toBe('parts/imports.play');
        expect(applyQuickFixEdits(documents.get('parts/imports.play')!, fix.edits)).toContain('import "../steps/z.play"');
    });

    it('should express pins relative to a nested importer sharing the target folder prefix', () => {
        const documents = new Map([['application.play', 'module M\n  feature F\n    import "parts/imports.play"\n'], ['parts/imports.play', 'import "steps/*.play"\n'], ['parts/steps/a.play', slice('View', [], ['E'])], ['parts/steps/z.play', slice('Write', ['E'])]]);
        const fix = findQuickFixes(documents.get('parts/steps/a.play')!, { documents, path: 'parts/steps/a.play', line: 3, diagnosticCode: 'PLAY0516' })[0];
        expect(fix.edits[0].path).toBe('parts/imports.play');
        expect(applyQuickFixEdits(documents.get('parts/imports.play')!, fix.edits)).toContain('import "steps/z.play"');
    });

    it('should refuse invalid source and ambiguous placement instead of guessing an application', () => {
        expect(findQuickFixes('not a model', { line: 1, diagnosticCode: 'PLAY0516' })).toEqual([]);
        const documents = new Map([['application.play', 'module A\n  feature F\n    import "step.play"\nmodule B\n  feature G\n    import "step.play"\n'], ['step.play', slice('View', [], ['E'])]]);
        expect(findQuickFixes(documents.get('step.play')!, { documents, path: 'step.play', line: 3, diagnosticCode: 'PLAY0516' })).toEqual([]);
    });

    it('should reject out-of-bounds edits, content loss, incorrect ranks and unchanged backward edges', () => {
        const documents = files([['a', [], ['E']], ['z', ['E'], []]]);
        const context = new TimelineQuickFixContext(documents, compileApplication(documents));
        const finding = context.findings[0];
        const candidate: QuickFixCandidate = { line: 3, fix: { diagnosticCode: 'PLAY0516', title: 'Untrusted edits', scope: 'occurrence', edits: [] } };
        expect(context.verify({ ...candidate, fix: { ...candidate.fix, edits: [{ start: -1, length: 0, text: '' }] } }, 'application.play', context.ranks, finding)).toBe(false);
        expect(context.verify({ ...candidate, fix: { ...candidate.fix, edits: [{ start: 0, length: documents.get('application.play')!.length, text: 'module Other\n' }] } }, 'application.play', context.ranks, finding)).toBe(false);
        expect(context.verify(candidate, 'application.play', new Map(), finding)).toBe(false);
        expect(context.verify(candidate, 'application.play', context.ranks, finding)).toBe(false);
        expect(context.move([], [...context.ranks.keys()], false)).toBeUndefined();
        expect(context.move([...context.ranks.keys()], [...context.ranks.keys()], false)).toBeUndefined();
    });

    it('should reject a shape-matched edit that changes placement or introduces a warning', () => {
        const documents = files([['a', [], ['E']], ['z', ['E'], []]]);
        const context = new TimelineQuickFixContext(documents, compileApplication(documents));
        const original = documents.get('application.play')!;
        for (const text of [original + 'import "missing/*.play"\n', 'module M\n  feature F\n']) {
            const candidate: QuickFixCandidate = { line: 3, change: { node: context.parsed('application.play').value, replacement: parseForAuthoring(text, 'application.play', [], false).value },
                fix: { diagnosticCode: 'PLAY0516', title: 'Untrusted replacement', scope: 'occurrence', edits: [{ start: 0, length: original.length, text }] } };
            expect(context.verify(candidate, 'application.play', context.ranks, context.findings[0])).toBe(false);
        }
    });

    it('should preserve existing warnings while checking that the move adds no new problem', () => {
        const documents = files([['a', [], ['E']], ['z', ['E'], []]]);
        documents.set('application.play', 'import "missing/*.play"\n' + documents.get('application.play'));
        const before = compileApplication(documents);
        expect(before.diagnostics.some(diagnostic => diagnostic.severity === 'warning')).toBe(true);
        const fix = findQuickFixes(documents.get('steps/a.play')!, { documents, path: 'steps/a.play', line: 3, diagnosticCode: 'PLAY0516' })[0];
        expect(fix).toBeDefined();
        const after = compileApplication(new Map(documents).set('application.play', applyQuickFixEdits(documents.get('application.play')!, fix.edits)!));
        expect(after.diagnostics.filter(diagnostic => diagnostic.severity === 'warning')).toEqual(before.diagnostics.filter(diagnostic => diagnostic.severity === 'warning'));
    });

    it('should refuse overlapping import expansions instead of duplicating reached files', () => {
        const documents = new Map([
            ['application.play', 'module M\n  feature F\n    import "left/*.play"\n    import "left/a.play"\n    import "right/*.play"\n'],
            ['left/a.play', slice('First', ['X'])], ['left/b.play', slice('Consumer', [], ['E'])],
            ['right/y.play', slice('Producer', ['E'], ['X'])],
        ]);
        expect(findQuickFixes(documents.get('left/b.play')!, { documents, path: 'left/b.play', line: 3, diagnosticCode: 'PLAY0516' })).toEqual([]);
    });

    it('should bound cached cursor requests and recompute an evicted repair from the same snapshot', () => {
        const documents = files([['a', [], ['E']], ['z', ['E'], []]]);
        const prepared = prepareQuickFixes(documents.get('steps/a.play')!, { documents, path: 'steps/a.play' });
        const original = prepared(3, 'PLAY0516');
        for (let line = 10; line < 44; line++) expect(prepared(line, 'PLAY0516')).toEqual([]);
        expect(prepared(3, 'PLAY0516')).toEqual(original);
        expect(original).toHaveLength(1);
    });

    it('should keep a prepared snapshot stable when its input map changes', () => {
        const documents = files([['a', [], ['E']], ['z', ['E'], []]]);
        const prepare = prepareQuickFixes(documents.get('steps/a.play')!, { documents, path: 'steps/a.play' });
        const before = prepare(3, 'PLAY0516');
        documents.set('steps/z.play', '// removed the producer');
        expect(prepare(3, 'PLAY0516')).toEqual(before);
        expect(before).toHaveLength(1);
        expect(findQuickFixes(documents.get('steps/a.play')!, { documents, path: 'steps/a.play', line: 3, diagnosticCode: 'PLAY0516' })).toEqual([]);
    });
});
