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
} as const;

export type DiagnosticCode = (typeof diagnosticCodes)[keyof typeof diagnosticCodes];
