// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { SpecificationSyntax } from '../Syntax/Specifications';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing specifications', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let registering: SpecificationSyntax;
    let rejecting: SpecificationSyntax;

    beforeEach(() => {
        result = a_parsed_document(
            'module Projects',
            '  feature Registration',
            '    slice StateChange RegisterProject',
            '      specification RegisteringAProject',
            '        given caller',
            '          authenticated',
            '        given ProjectArchived',
            '          name = "Old"',
            '        when RegisterProject',
            '          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"',
            '          name = "Screenplay"',
            '        then ProjectRegistered',
            '          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"',
            '          name = name',
            '        then readmodel ProjectSummary exactly',
            '          count = 1',
            '        then query ProjectById',
            '          arguments',
            '            projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"',
            '        then no readmodel Draft for "x"',
            '      specification RejectingAnEmptyProjectName',
            '        when RegisterProject',
            '          name = ""',
            '        then error "Project name is required"',
            '        then error',
        );
        [registering, rejecting] = result.value.modules[0].features[0].slices[0].specifications;
    });

    it('should report nothing', () => {
        result.diagnostics.should.deep.equal([]);
    });

    it('should read the given events with their values', () => {
        registering.given.map(given => [given.eventType, given.values[0].source]).map(([type, source]) => [type, (source as { value: unknown }).value])
            .should.deep.equal([['ProjectArchived', 'Old']]);
    });

    it('should read the command it executes', () => {
        [registering.when!.commandType, registering.when!.values.map(value => value.property)].should.deep.equal(['RegisterProject', ['projectId', 'name']]);
    });

    it('should read the expected event and its event source', () => {
        [registering.thenEvents[0].eventType, registering.thenEvents[0].for!.kind].should.deep.equal(['ProjectRegistered', 'LiteralExpressionSyntax']);
    });

    it('should read a value that names a property as a path', () => {
        registering.thenEvents[0].values[0].source.kind.should.equal('PathExpressionSyntax');
    });

    it('should read the expected read model', () => {
        [registering.thenReadModels[0].name, registering.thenReadModels[0].exactly].should.deep.equal(['ProjectSummary', true]);
    });

    it('should read the expected errors, named or not', () => {
        rejecting.thenErrors.map(error => error.name).should.deep.equal(['Project name is required', null]);
    });
});
