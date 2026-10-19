// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { removeSpecFixtureDirectory, specFixtureDirectory } from '../tests/specFixtureDirectory';
import * as vscode from 'vscode';
import { applicationRootFile, VerdictCollection, verdictSource } from '../VerdictCollection';
import { editor, FakeDocument } from '../for_RegisterVerdicts/given/an_editor';
import { finding } from '../for_VerdictReader/given/a_diagnostics_server';
import { diagnostic } from '../RepairSession';

vi.mock('vscode', async importOriginal => (await import('../for_RegisterVerdicts/given/an_editor')).createVscode(await importOriginal<typeof import('../vscode.stub')>()));

const collection = 'screenplay-csharp-verdicts';
const file = (name: string) => vscode.Uri.file(`/model/${name}`);
const open = (name: string, isDirty = false): FakeDocument => ({ uri: file(name), languageId: 'screenplay', isDirty, lineCount: 10, lineAt: () => ({ text: 'screen Orders for the line' }) });

describe('when publishing verdicts', () => {
    let verdicts: VerdictCollection;
    beforeEach(() => { editor.reset(); verdicts = new VerdictCollection(); });

    it('shows them in their own collection, labelled as C# checks, on the reported line', () => {
        verdicts.publish('/model', [diagnostic(finding({ code: 'PLAY0532', path: 'app.play', line: 3 }))]);
        const [marker] = editor.diagnosticsOf(collection, file('app.play'));
        expect(marker.source).toBe(verdictSource);
        expect(verdictSource).toContain('C#');
        expect(marker.code).toBe('PLAY0532');
        expect(marker.severity).toBe(vscode.DiagnosticSeverity.Warning);
        expect(marker.range).toMatchObject({ start: { line: 2, character: 4 } });
        expect([...editor.collections.keys()]).toEqual([collection]);
    });
    it('skips documents with unsaved edits, because the verdict describes the saved file', () => {
        editor.documents = [open('app.play', true)];
        verdicts.publish('/model', [diagnostic(finding({ code: 'PLAY0532' })), diagnostic(finding({ code: 'PLAY0533', path: 'other.play' }))]);
        expect(editor.diagnosticsOf(collection, file('app.play'))).toEqual([]);
        expect(editor.diagnosticsOf(collection, file('other.play'))).toHaveLength(1);
    });
    it('replaces earlier verdicts', () => {
        verdicts.publish('/model', [diagnostic(finding({ code: 'PLAY0532' }))]);
        verdicts.publish('/model', [diagnostic(finding({ code: 'PLAY0533', path: 'other.play' }))]);
        expect(editor.count(collection)).toBe(1);
    });
    it('forgets one document when it changes', () => {
        verdicts.publish('/model', [diagnostic(finding({ code: 'PLAY0532' })), diagnostic(finding({ code: 'PLAY0533', path: 'other.play' }))]);
        verdicts.clearDocument(file('app.play'));
        expect(editor.diagnosticsOf(collection, file('app.play'))).toEqual([]);
        expect(editor.diagnosticsOf(collection, file('other.play'))).toHaveLength(1);
    });
});

describe('when publishing an application-level verdict', () => {
    let root: string;
    const applicationLevel = () => ({ code: 'PLAY0537', message: 'No entry points', severity: 'Warning', location: { line: 0, column: 0, path: null } });
    beforeEach(() => { editor.reset(); root = specFixtureDirectory('verdicts-'); });
    afterEach(() => removeSpecFixtureDirectory(root));

    it('shows it on the first line of the model root file, the alphabetically first .play file', () => {
        fs.mkdirSync(path.join(root, 'sub'));
        fs.writeFileSync(path.join(root, 'b.play'), ''); fs.writeFileSync(path.join(root, 'a.play'), ''); fs.writeFileSync(path.join(root, 'sub', 'c.play'), '');
        new VerdictCollection().publish(root, [applicationLevel()]);
        const [marker] = editor.diagnosticsOf(collection, vscode.Uri.file(path.join(root, 'a.play')));
        expect(marker.code).toBe('PLAY0537');
        expect(marker.range).toMatchObject({ start: { line: 0, character: 0 } });
        expect(editor.count(collection)).toBe(1);
    });
    it('finds the root file in a subfolder when the root holds none, and shows nothing when there is no model', () => {
        expect(applicationRootFile(root)).toBeUndefined();
        fs.mkdirSync(path.join(root, 'sub'));
        fs.writeFileSync(path.join(root, 'sub', 'c.play'), '');
        expect(applicationRootFile(root)).toBe('sub/c.play');
        const empty = specFixtureDirectory('verdicts-empty-');
        try {
            new VerdictCollection().publish(empty, [applicationLevel()]);
            expect(editor.count(collection)).toBe(0);
        } finally { removeSpecFixtureDirectory(empty); }
    });
    it('does not guess a file for any other unattributed finding', () => {
        fs.writeFileSync(path.join(root, 'a.play'), '');
        new VerdictCollection().publish(root, [{ code: 'PLAY0532', message: 'x', severity: 'Warning', location: { line: 2, column: 3, path: null } }]);
        expect(editor.count(collection)).toBe(0);
    });
});

