// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes, parseFolder, parsePlacedDocuments, toSyntaxJson } from '@cratis/screenplay-compiler';
import { compileEventModelApplication } from '../compileEventModelApplication';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const names = (files: { path: string; source: string }[]) => {
    const application = compileEventModelApplication(files);
    return toEventModelDocument(application.value, 'Board').collections.flatMap(collection => collection.modules).map(module => module.name);
};

describe('when selecting the board root', () => {
    it('should use application.play only for ranks and still show unimported scratch documents', () => {
        names([
            { path: 'scratch.play', source: 'module Scratch' },
            { path: 'other-scratch.play', source: 'module Other' },
            { path: 'application.play', source: 'import "z.play"\nimport "a.play"' },
            { path: 'a.play', source: 'module A' },
            { path: 'z.play', source: 'module Z' },
        ]).should.deep.equal(['Z', 'A', 'Other', 'Scratch']);
    });

    it('should keep an import-less application.play folder board identical to path compilation', () => {
        const files = [
            { path: 'application.play', source: 'concept ProjectId : Uuid' },
            { path: 'Projects/Projects.play', source: 'module Projects' },
            { path: 'Projects/Registration/Registration.play', source: 'module Projects\n  feature Registration' },
            { path: 'Projects/Registration/RegisterProject.play', source: 'module Projects\n  feature Registration\n    slice StateChange RegisterProject' },
        ];
        const baseline = parseFolder(files);
        const board = compileEventModelApplication(files);
        toEventModelDocument(board.value, 'Board').should.deep.equal(toEventModelDocument(baseline.value, 'Board'));
        board.documents.should.deep.equal(baseline.documents);
        board.diagnostics.should.deep.equal(baseline.diagnostics);
        names(files).should.deep.equal(['Projects']);
    });

    it('should keep folder syntax, duplicate diagnostics and first-producer ownership unchanged', () => {
        const files = [
            { path: 'application.play', source: 'import "z.play"\nimport "a.play"' },
            { path: 'z.play', source: 'concept Shared : Uuid\nmodule Z\n  feature F\n    slice StateChange ProducerZ\n      event Recorded\n        value Uuid' },
            { path: 'a.play', source: 'concept Shared : String\nmodule A\n  feature F\n    slice StateChange ProducerA\n      event Recorded\n        value String' },
        ];
        const baseline = parseFolder(files);
        const actual = compileEventModelApplication(files);
        actual.documents.should.deep.equal(baseline.documents);
        actual.diagnostics.should.deep.equal(baseline.diagnostics);
        actual.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.RepeatedDeclarationAcrossFiles).should.be.true;
        JSON.stringify(toSyntaxJson(actual.value)).should.equal(JSON.stringify(toSyntaxJson(baseline.value)));
        const events = (application: typeof actual.value) => toEventModelDocument(application, 'Board').collections[0].modules
            .flatMap(module => module.features).flatMap(feature => feature.slices).flatMap(slice => slice.events).map(event => event.id);
        events(actual.value).should.deep.equal(events(baseline.value));
    });

    it('should keep the first event owner from folder path order when imports reverse producers', () => {
        const files = [
            { path: 'application.play', source: 'import "z.play"\nimport "a.play"' },
            { path: 'z.play', source: 'module Z\n  feature F\n    slice StateChange ProducerZ\n      event Recorded\n        value Uuid' },
            { path: 'a.play', source: 'module A\n  feature F\n    slice StateChange ProducerA\n      event Recorded\n        value String' },
        ];
        const baseline = toEventModelDocument(parseFolder(files).value, 'Board');
        const actual = toEventModelDocument(compileEventModelApplication(files).value, 'Board');
        const ownerIds = (document: typeof baseline) => document.collections[0].modules
            .flatMap(module => module.features).flatMap(feature => feature.slices).flatMap(slice => slice.events).map(event => event.id);
        ownerIds(actual).should.deep.equal(ownerIds(baseline));
    });

    it('should rank declarations in a module own file before globbed restating slice files', () => {
        const files = [
            { path: 'application.play', source: 'import "**/*.play"' },
            { path: 'T/T.play', source: 'module T\n  feature Recording\n  feature Approval' },
            { path: 'T/Approval/A.play', source: 'module T\n  feature Approval\n    slice StateChange Approve' },
            { path: 'T/Recording/R.play', source: 'module T\n  feature Recording\n    slice StateChange Record' },
        ];
        const document = toEventModelDocument(compileEventModelApplication(files).value, 'Board');
        document.collections[0].modules[0].features.map(feature => feature.name).should.deep.equal(['Recording', 'Approval']);
    });

    it('should find a differently named importing root through nested imports', () => {
        names([
            { path: 'story.play', source: 'import "barrel.play"' },
            { path: 'barrel.play', source: 'import "z.play"\nimport "a.play"' },
            { path: 'a.play', source: 'module A' },
            { path: 'z.play', source: 'module Z' },
        ]).should.deep.equal(['Z', 'A']);
    });

    it('should let VS Code restrict ordering to application.play rather than discover another root', () => {
        const files = [
            { path: 'story.play', source: 'import "z.play"\nimport "a.play"' },
            { path: 'a.play', source: 'module A' },
            { path: 'z.play', source: 'module Z' },
        ];
        const compilation = compileEventModelApplication(files, 'application.play');
        toEventModelDocument(compilation.value, 'Board').should.deep.equal(toEventModelDocument(parsePlacedDocuments(compilation.documents).value, 'Board'));
        names(files).should.deep.equal(['Z', 'A']);
    });

    it('should keep path order when application.play is import-less even if another file imports', () => {
        const files = [
            { path: 'application.play', source: 'concept Name : String' },
            { path: 'story.play', source: 'import "z.play"\nimport "a.play"' },
            { path: 'a.play', source: 'module A' },
            { path: 'z.play', source: 'module Z' },
        ];
        names(files).should.deep.equal(['A', 'Z']);
    });

    it('should fall back to path order without an importing root', () => {
        names([{ path: 'z.play', source: 'module Z' }, { path: 'a.play', source: 'module A' }]).should.deep.equal(['A', 'Z']);
        names([]).should.deep.equal([]);
    });

    it('should fall back rather than guess between independent importing roots', () => {
        names([
            { path: 'z.play', source: 'module Z\nimport "shared.play"' },
            { path: 'a.play', source: 'module A\nimport "shared.play"' },
            { path: 'shared.play', source: 'concept Name : String' },
        ]).should.deep.equal(['A', 'Z']);
    });
});
