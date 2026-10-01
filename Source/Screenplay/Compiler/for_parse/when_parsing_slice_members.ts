// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { DeclarativeValidateSyntax } from '../Syntax/Commands';
import { UniquePropertyConstraintSyntax } from '../Syntax/Constraints';
import { ChildrenSyntax, FromSyntax, JoinSyntax } from '../Syntax/Projections';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing slice members', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let registration: SliceSyntax;
    let lookup: SliceSyntax;
    let reminders: SliceSyntax;

    beforeEach(() => {
        result = a_parsed_document(
            'module Projects',
            '  feature Registration',
            '    slice StateChange RegisterProject',
            '      command RegisterProject',
            '        projectId ProjectId identifier',
            '        name ProjectName',
            '        description String',
            '        authorize Admins',
            '        validate',
            '          name not empty message "Project name is required"',
            '          name max 40 severity warning',
            '        produces ProjectRegistered',
            '          for projectId',
            '          name = name',
            '      event ProjectRegistered',
            '        name ProjectName',
            '      constraint UniqueName',
            '        unique name, owner on ProjectRegistered',
            '        released by ProjectRemoved',
            '        message "Name taken"',
            '        ignore casing',
            '    slice StateView ProjectLookup',
            '      readmodel ProjectSummary',
            '        name ProjectName',
            '      query ProjectById => observable ProjectSummary?',
            '        by projectId ProjectId',
            '        filter name ProjectName',
            '      projection ProjectSummaryProjection => ProjectSummary',
            '        from ProjectRegistered, ProjectRenamed key projectId',
            '          key Composite {',
            '            id = projectId',
            '          }',
            '          name = name',
            '        join owner on ownerId',
            '          with OwnerAssigned',
            '            ownerName = name',
            '        children members identified by memberId',
            '          from MemberAdded',
            '          remove with MemberRemoved',
            '    slice Automation Reminders',
            '      reaction RemindOwner',
            '        when ProjectRegistered',
            '          invokes NotifyOwner',
            '            projectId = projectId',
            '        every 2 hours',
            '        at 09:30 on Monday',
        );
        [registration, lookup, reminders] = result.value.modules[0].features[0].slices;
    });

    it('should report nothing', () => {
        result.diagnostics.should.deep.equal([]);
    });

    it('should read the command properties, including one named like a directive', () => {
        registration.commands[0].properties.map(property => [property.name, property.isIdentifier]).should.deep.equal([
            ['projectId', true],
            ['name', false],
            ['description', false],
        ]);
    });

    it('should read the declarative validation rules', () => {
        const rules = (registration.commands[0].validations[0] as DeclarativeValidateSyntax).rules;
        rules.map(rule => [rule.property, rule.rule, rule.message, rule.severity]).should.deep.equal([
            ['name', 'NotEmpty', 'Project name is required', 'Error'],
            ['name', 'Max', null, 'Warning'],
        ]);
    });

    it('should read a rule operand as a literal', () => {
        (registration.commands[0].validations[0] as DeclarativeValidateSyntax).rules[1].value!.should.include({ kind: 'LiteralExpressionSyntax', value: 40 });
    });

    it('should still read the event declared after produces', () => {
        registration.events.map(event => event.name).should.deep.equal(['ProjectRegistered']);
    });

    it('should read a composite unique constraint with its options', () => {
        const constraint = registration.constraints[0] as UniquePropertyConstraintSyntax;
        [constraint.property, constraint.additionalProperties, constraint.event, constraint.releasedBy, constraint.message, constraint.ignoreCasing]
            .should.deep.equal(['name', ['owner'], 'ProjectRegistered', ['ProjectRemoved'], 'Name taken', true]);
    });

    it('should read the query return type, observability and parameters', () => {
        const query = lookup.queries[0];
        [query.returnType.name, query.returnType.isOptional, query.isObservable, query.by!.name, query.filters[0].name]
            .should.deep.equal(['ProjectSummary', true, true, 'projectId', 'name']);
    });

    it('should read the read model the projection builds', () => {
        lookup.projections[0].readModel!.should.equal('ProjectSummary');
    });

    it('should read every event a from block consumes', () => {
        (lookup.projections[0].blocks[0] as FromSyntax).events.map(spec => spec.event).should.deep.equal(['ProjectRegistered', 'ProjectRenamed']);
    });

    it('should read the events a join consumes', () => {
        (lookup.projections[0].blocks[1] as JoinSyntax).events.map(event => event.event).should.deep.equal(['OwnerAssigned']);
    });

    it('should read the blocks of a children block', () => {
        (lookup.projections[0].blocks[2] as ChildrenSyntax).blocks.map(block => block.kind).should.deep.equal(['FromSyntax', 'RemoveWithSyntax']);
    });

    it('should read every kind of trigger source', () => {
        reminders.reactions[0].triggers.map(trigger => trigger.source).map(({ location: _, ...source }) => source).should.deep.equal([
            { kind: 'NamedTriggerSourceSyntax', name: 'ProjectRegistered' },
            { kind: 'IntervalTriggerSourceSyntax', amount: 2, unit: 'Hours' },
            { kind: 'ScheduleTriggerSourceSyntax', time: '09:30:00.0000000', dayOfWeek: 'Monday', dayOfMonth: null },
        ]);
    });
});
