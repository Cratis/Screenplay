// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The members the syntax tree gained with exact numeric source. The internal tree keeps them for every
// document, but an unmarked (Legacy) document writes the wire form it always had, so these are left out of it.
// Exact documents write the complete expanded form.
const mappingKinds = ['SetMappingSyntax', 'ClearMappingSyntax', 'IncrementMappingSyntax', 'DecrementMappingSyntax', 'CountMappingSyntax', 'AddMappingSyntax', 'SubtractMappingSyntax'];

const additions: Readonly<Record<string, readonly string[]>> = {
    ApplicationSyntax: ['policies', 'seeds'],
    ConceptSyntax: ['validations'],
    DeclarativeValidateSyntax: ['requirements'],
    CodeValidateSyntax: ['code'],
    CaptureAppendSyntax: ['when', 'mappings', 'tags'],
    CaptureChildrenSyntax: ['map'],
    CaptureNestedSyntax: ['map'],
    CaptureSyntax: ['map'],
    EventSpecSyntax: ['key'],
    FromSyntax: ['key', 'parentKey'],
    ChildrenSyntax: ['identifiedBy'],
    RemoveWithSyntax: ['key', 'parentKey'],
    RemoveViaJoinSyntax: ['key'],
    ProjectionEntersOnSyntax: ['key'],
    ProjectionSyntax: ['key'],
    QueryParameterSyntax: ['source'],
    ProducesSyntax: ['when'],
    InvokesSyntax: ['mappings'],
    ReactionSyntax: ['where'],
    SpecificationSyntax: ['thenAbsentReadModels', 'thenQueries'],
    ...Object.fromEntries(mappingKinds.map(kind => [kind, ['source', 'value']]))
};

export function isExactOnlyMember(kind: string, member: string): boolean {
    return additions[kind]?.includes(member) === true;
}
