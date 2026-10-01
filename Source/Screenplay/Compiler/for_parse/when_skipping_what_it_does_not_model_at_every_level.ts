// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { DeclarativeValidateSyntax } from '../Syntax/Commands';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

// Each construct the compiler does not model is skipped whole - its body, fenced code included - and the
// construct after it is read as if the skipped one were not there.
describe('when skipping what it does not model at every level', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let slice: SliceSyntax;

    beforeEach(() => {
        result = a_parsed_document(
            'concept Email : String',
            '  file Concepts/Email.cs',
            '  validate',
            '    value not empty',
            'type Address',
            '  file Types/Address.cs',
            '  street String',
            'persona Clerk',
            '  description "Works the desk"',
            'module Customers',
            '  screen template Main',
            '    region body',
            '  form Details',
            '    field name',
            '  on save',
            '    confirm "Sure?"',
            '  feature Onboarding',
            '    authorize policy Clerks',
            '    uses Auditing',
            '    contribute menu',
            '      item Onboard',
            '    slice StateChange Register',
            '      file Slices/Register.cs',
            '      command Register',
            '        name String',
            '        handler',
            '          ```csharp',
            '    return;',
            '          ```',
            '        reads Customer by name',
            '        concurrency',
            '          eventSource',
            '        validate',
            '          ```csharp',
            '          return ValidationResult.Success;',
            '          ```',
            '        validate csharp',
            '          ```csharp',
            '          return ValidationResult.Success;',
            '          ```',
            '        validate',
            '          name rule KnownName',
            '            file Rules/KnownName.cs',
            '          require name != "x"',
            '      event Registered',
            '        file Events/Registered.cs',
            '        name String',
            '      readmodel Customer',
            '        file ReadModels/Customer.cs',
            '        name String',
            '      query ByName => Customer',
            '        authorize policy Clerks',
            '        performer',
            '          file Queries/ByName.cs',
            '      projection Customers => Customer',
            '        file Projections/Customers.cs',
            '        no automap',
            '        all',
            '          automap',
            '        children addresses identified by addressId',
            '          no automap',
            '          from AddressAdded',
            '      reaction Welcome',
            '        where name != "x"',
            '        when Registered',
            '          file Reactions/Welcome.cs',
            '          ```csharp',
            '          return;',
            '          ```',
            '        every 30 seconds',
            '        every 1 day',
            '        at 07:00',
            '      specification Registering',
            '        file Specifications/Registering.cs',
            '        given caller',
            '          role "clerk"',
            '        when Register',
            '          name = "Ada"',
            '        then denied',
            '        then events in any order',
            '      capture FromLegacy',
            '        source legacy',
            '      reducer Totals => Customer',
            '        on Registered',
            '      screen Register',
            '        ```tsx',
            'export const Register = () => <div/>;',
            '        ```',
            '      event Archived',
            '        reason String',
        );
        slice = result.value.modules[0].features[0].slices[0];
    });

    it('should report nothing', () => {
        result.diagnostics.should.deep.equal([]);
    });

    it('should read the type around its file directive', () => {
        result.value.types[0].properties.map(property => property.name).should.deep.equal(['street']);
    });

    it('should read the command around everything it skips', () => {
        [slice.commands[0].properties.map(property => property.name), slice.commands[0].validations.map(validate => validate.kind)].should.deep.equal([
            ['name'],
            ['CodeValidateSyntax', 'CodeValidateSyntax', 'DeclarativeValidateSyntax'],
        ]);
    });

    it('should read a named rule without its implementation', () => {
        (slice.commands[0].validations[2] as DeclarativeValidateSyntax).rules.map(rule => [rule.rule, rule.value?.kind]).should.deep.equal([['Rule', 'PathExpressionSyntax']]);
    });

    it('should read every trigger of the reaction', () => {
        slice.reactions[0].triggers.map(trigger => trigger.source).map(({ location: _, ...source }) => source).should.deep.equal([
            { kind: 'NamedTriggerSourceSyntax', name: 'Registered' },
            { kind: 'IntervalTriggerSourceSyntax', amount: 30, unit: 'Seconds' },
            { kind: 'IntervalTriggerSourceSyntax', amount: 1, unit: 'Days' },
            { kind: 'ScheduleTriggerSourceSyntax', time: '07:00:00.0000000', dayOfWeek: null, dayOfMonth: null },
        ]);
    });

    it('should read the projection around what it skips', () => {
        slice.projections[0].blocks.map(block => block.kind).should.deep.equal(['AllSyntax', 'ChildrenSyntax']);
    });

    it('should read the specification around the caller and the denial', () => {
        [slice.specifications[0].when!.commandType, slice.specifications[0].thenEventsInAnyOrder].should.deep.equal(['Register', true]);
    });

    it('should read the event after the skipped screen', () => {
        slice.events.map(event => event.name).should.deep.equal(['Registered', 'Archived']);
    });
});
