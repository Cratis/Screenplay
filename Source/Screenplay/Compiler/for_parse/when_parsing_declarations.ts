// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing declarations', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let slice: SliceSyntax;

    beforeEach(() => {
        result = a_parsed_document(
            'concept Email : String @pii',
            '  pii reason "Identifies a person"',
            'concept Status : Enum',
            '  active',
            '  @validate',
            'type Address',
            '  street String',
            '  lines String[] optional',
            'module Projects',
            '  feature Registration',
            '    slice StateChange RegisterProject',
            '      event ProjectRegistered generation 2',
            '        name ProjectName',
            '        tag "projects"',
            '      readmodel ProjectSummary',
            '        description "A project at a glance"',
            '        id ProjectId',
        );
        slice = result.value.modules[0].features[0].slices[0];
    });

    it('should succeed', () => {
        result.diagnostics.should.deep.equal([]);
    });

    it('should read a concept with its attribute reason', () => {
        const email = result.value.concepts[0];
        [email.name, email.type, email.attributes[0].name, email.attributes[0].reason].should.deep.equal(['Email', 'String', 'pii', 'Identifies a person']);
    });

    it('should read enumeration values without their escape', () => {
        result.value.concepts[1].values.should.deep.equal(['active', 'validate']);
    });

    it('should read a type with collection and optional properties', () => {
        const lines = result.value.types[0].properties[1].type;
        [lines.name, lines.isCollection, lines.isOptional].should.deep.equal(['String', true, true]);
    });

    it('should read the event generation', () => {
        [slice.events[0].generation, slice.events[0].hasGenerationMarker].should.deep.equal([2, true]);
    });

    it('should not read a tag as a property', () => {
        slice.events[0].properties.map(property => property.name).should.deep.equal(['name']);
    });

    it('should read a read model with its description', () => {
        [slice.readModels[0].name, slice.readModels[0].description, slice.readModels[0].properties.length].should.deep.equal(['ProjectSummary', 'A project at a glance', 1]);
    });
});
