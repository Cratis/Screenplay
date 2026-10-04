// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The PLAY codes the editor reports. Every one of them mirrors a condition the compiler checks and
// carries the compiler's own code for it, so the same problem is called the same thing whether it was
// found by the CLI or by a squiggle - see Documentation/screenplay/diagnostics.md for the catalogue.
//
// A code is only ever added here for a condition the compiler can also report. The editor makes a few
// structural checks the compiler does not - the capture and projection validators are where they live -
// and those stay codeless deliberately: minting a PLAY number for something no compiler run can emit
// would make the catalogue describe two different tools.
export const diagnosticCodes = {
    invalidEventSourceDeclaration: 'PLAY0503',
    invalidCommandStream: 'PLAY0504',
    ambiguousCommandStream: 'PLAY0505',
    unsupportedStreamIdType: 'PLAY0506',
    redundantSourceStreamId: 'PLAY0507',
    invalidImplementationBlock: 'PLAY0492',
    invalidImplementationHint: 'PLAY0493',
    conflictingImplementationSources: 'PLAY0494',
    eventSourceIdInPayload: 'PLAY0469',
    explicitProducesTargetsRequired: 'PLAY0470',
    redundantEventId: 'PLAY0471',
    invalidEventId: 'PLAY0472',
    inlineEventCollision: 'PLAY0473',
    inlineEventOutsideCommand: 'PLAY0474',
    inlineEventGeneration: 'PLAY0475',
    reservedProductionMetadata: 'PLAY0476',
    invalidEventDocumentation: 'PLAY0477',
    tabIndentation: 'PLAY0006',
    unknownPrimitiveType: 'PLAY0008',
    unknownSliceType: 'PLAY0028',
    duplicateCommandIdentifier: 'PLAY0036',
    unknownContextPath: 'PLAY0153',
    unknownContextCausedByProperty: 'PLAY0154',
    unknownContextIdentityProperty: 'PLAY0155',
    unclosedCodeBlock: 'PLAY0164',
    unknownType: 'PLAY0165',
    unknownEvent: 'PLAY0166',
    unknownPolicy: 'PLAY0167',
    duplicateDeclaration: 'PLAY0168',
    unknownEventContextMember: 'PLAY0295',
    unknownEventContextPath: 'PLAY0296',
    eventContextPathBelowCollection: 'PLAY0297',
    missingEventContextPath: 'PLAY0298',
    unresolvedDynamicKeySource: 'PLAY0299',
    invalidFileImport: 'PLAY0454',
    omittedProductionDestination: 'PLAY0478',
    legacyOptionalSuffix: 'PLAY0479',
    invalidOptionalModifierOrder: 'PLAY0480',
    optionalReadsNotSupported: 'PLAY0481',
    unavailableResponseExecution: 'PLAY0268',
    generatedPropertyOutsideCommand: 'PLAY0482',
    invalidGeneratedType: 'PLAY0483',
    invalidGeneratedModifierOrder: 'PLAY0484',
    generatedPropertyInput: 'PLAY0485',
    invalidCommandResponse: 'PLAY0486',
    invalidResponseSource: 'PLAY0487',
    duplicateResponseField: 'PLAY0488',
    invalidResponseShape: 'PLAY0489',
    invalidGeneratedFixture: 'PLAY0490',
    invalidReturnExpectation: 'PLAY0491',
} as const;

export type DiagnosticCode = (typeof diagnosticCodes)[keyof typeof diagnosticCodes];
