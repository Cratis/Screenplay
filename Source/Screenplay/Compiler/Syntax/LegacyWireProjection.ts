// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The members the syntax tree gained with exact numeric source. The internal tree keeps them for every
// document, but an unmarked (Legacy) document writes the wire form it always had, so these are left out of it.
// Exact documents write the complete expanded form.
const additions: Readonly<Record<string, ReadonlySet<string>>> = Object.fromEntries(
    ('ApplicationSyntax:policies,seeds;ConceptSyntax:validations;DeclarativeValidateSyntax:requirements;CodeValidateSyntax:code;' +
    'CaptureAppendSyntax:when,mappings,tags;CaptureChildrenSyntax:map;CaptureNestedSyntax:map;CaptureSyntax:map;EventSpecSyntax:key;' +
    'FromSyntax:key,parentKey;ChildrenSyntax:identifiedBy;RemoveWithSyntax:key,parentKey;RemoveViaJoinSyntax:key;ProjectionEntersOnSyntax:key;' +
    'ProjectionSyntax:key;QueryParameterSyntax:source;ProducesSyntax:when;InvokesSyntax:mappings;ReactionSyntax:where;' +
    'SpecificationSyntax:thenAbsentReadModels,thenQueries').split(';').map(entry => entry.split(':')).map(([kind, members]) => [kind, new Set(members.split(','))]));

// Every projection mapping kind, but not the always-modeled PropertyMappingSyntax.
const mappingKind = /^(?:Set|Clear|Increment|Decrement|Count|Add|Subtract)MappingSyntax$/;

export function isExactOnlyMember(kind: string, member: string): boolean {
    return additions[kind]?.has(member) === true || (mappingKind.test(kind) && (member === 'source' || member === 'value'));
}
