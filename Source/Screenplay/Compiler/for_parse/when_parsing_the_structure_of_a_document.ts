// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing the structure of a document', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'domain Cratis.Projects',
            'import Cratis.Identity',
            'concept ProjectId : Uuid',
            'module Projects',
            '  description "Everything about projects"',
            '  feature Registration',
            '    feature Drafts',
            '      slice StateChange SaveDraft',
            '    slice StateChange RegisterProject',
            '    slice StateView ProjectLookup',
            '  feature Archiving',
            'module Billing',
        );
    });

    it('should succeed', () => {
        result.success.should.be.true;
    });

    it('should read the domain', () => {
        result.value.domain!.name.should.equal('Cratis.Projects');
    });

    it('should read the import', () => {
        result.value.imports.map(i => i.qualifiedName).should.deep.equal(['Cratis.Identity']);
    });

    it('should read every module in order', () => {
        result.value.modules.map(module => module.name).should.deep.equal(['Projects', 'Billing']);
    });

    it('should read the module description', () => {
        result.value.modules[0].description!.should.equal('Everything about projects');
    });

    it('should read the features of a module', () => {
        result.value.modules[0].features.map(feature => feature.name).should.deep.equal(['Registration', 'Archiving']);
    });

    it('should read a nested feature', () => {
        result.value.modules[0].features[0].features[0].slices[0].name.should.equal('SaveDraft');
    });

    it('should read the slices of a feature with their types', () => {
        result.value.modules[0].features[0].slices.map(slice => [slice.type, slice.name]).should.deep.equal([
            ['StateChange', 'RegisterProject'],
            ['StateView', 'ProjectLookup'],
        ]);
    });

    it('should locate a slice at its first significant character', () => {
        result.value.modules[0].features[0].slices[0].location.should.deep.equal({ line: 9, column: 5, path: 'Document.play' });
    });
});
