// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { should } from 'chai';
import { beforeEach, describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { applyQuickFixEdits, findQuickFixes } from '../Authoring/QuickFixes';

should();

describe('when repairing duplicates alongside legacy spellings on one header', () => {
    const source = 'concept Value : String @pii pii @pii secret secret // keep\n  pii reason "@pii pii"';
    let fixes: ReturnType<typeof findQuickFixes>;
    beforeEach(() => {
        fixes = findQuickFixes(source, { line: 1 });
    });
    it('should keep independent duplicate and spelling occurrence fixes', () => {
        fixes.filter(fix => fix.scope === 'occurrence').map(fix => fix.diagnosticCode).should.have.members(['PLAY0565', 'PLAY0653']);
    });
    it('should remove every later wire-equivalent marker in one edit', () => {
        const duplicate = fixes.find(fix => fix.diagnosticCode === 'PLAY0653')!;
        applyQuickFixEdits(source, duplicate.edits)!.should.equal(source.replace('@pii pii @pii secret secret', '@pii secret'));
    });
});

for (const [markers, wire, canonical, alias] of [
    ['pii personal', 'pii', 'pii', 'personal'],
    ['personal pii', 'pii', 'pii', 'personal'],
    ['pii pii', 'pii', 'pii', ''],
    ['secret secret', 'sensitive', 'secret', ''],
    ['@pii @pii', 'pii', 'pii', ''],
]) {
    describe(`when collapsing duplicate compliance markers ${markers}`, () => {
        let result: ReturnType<typeof parse>;
        beforeEach(() => { result = parse(`concept Value : String ${markers}\n  ${canonical} reason "Keep the note"`); });
        it('should retain one attribute with its body settings', () => {
            result.value.concepts[0].attributes.should.have.lengthOf(1);
            result.value.concepts[0].attributes[0].name.should.equal(wire);
            result.value.concepts[0].attributes[0].reason!.should.equal('Keep the note');
        });
        it('should report one warning on the concept header', () => {
            result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0653').should.deep.equal([{
                code: 'PLAY0653', severity: 'warning',
                message: `Concept 'Value' repeats the '${canonical}' marker${alias ? ` - '${alias}' is the same marker as '${canonical}'` : ''} - remove the duplicate`,
                location: result.value.concepts[0].location,
            }]);
        });
    });
    describe(`when repairing duplicate compliance markers ${markers}`, () => {
        const source = `// café\r\nconcept Café : String ${markers} // keep\r\n  ${canonical} reason "pii personal secret"`;
        let repaired: string | undefined;
        beforeEach(() => {
            const fix = findQuickFixes(source, { line: 2, diagnosticCode: 'PLAY0653' }).find(fix => fix.scope === 'occurrence');
            repaired = fix === undefined ? undefined : applyQuickFixEdits(source, fix.edits);
        });
        it('should remove only later markers without rewriting notes or trivia', () => {
            (repaired ?? '').should.equal(source.replace(markers, markers.split(' ')[0]));
        });
        it('should remove the duplicate warning', () => {
            (repaired === undefined ? ['missing repair'] : parse(repaired).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0653')).should.deep.equal([]);
        });
    });
}
