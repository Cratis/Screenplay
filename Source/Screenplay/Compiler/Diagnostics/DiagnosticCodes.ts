// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The subset of the C# compiler's DiagnosticCodes this compiler reports. The values are the same, so a
// code means the same thing whichever compiler reported it.
export const DiagnosticCodes = {
    UnknownTopLevelConstruct: 'PLAY0001',
    InvalidDomainDeclaration: 'PLAY0002',
    DuplicateDomain: 'PLAY0003',
    DomainNotFirst: 'PLAY0004',
    InvalidImportDeclaration: 'PLAY0005',
    TabIndentation: 'PLAY0006',
    InvalidConceptDeclaration: 'PLAY0007',
    UnknownPrimitiveType: 'PLAY0008',
    InvalidEnumerationValue: 'PLAY0009',
    UnknownConceptDirective: 'PLAY0010',
    InvalidTypeDeclaration: 'PLAY0014',
    TypeWithoutProperties: 'PLAY0015',
    InvalidPropertyDeclaration: 'PLAY0016',
    IdentifierOutsideCommand: 'PLAY0017',
    InvalidEventDeclaration: 'PLAY0018',
    IdentifierOnEventProperty: 'PLAY0019',
    InvalidModuleDeclaration: 'PLAY0021',
    UnknownModuleDirective: 'PLAY0022',
    InvalidFeatureDeclaration: 'PLAY0023',
    UnknownFeatureDirective: 'PLAY0024',
    InvalidSliceDeclaration: 'PLAY0027',
    UnknownSliceType: 'PLAY0028',
    UnknownSliceDirective: 'PLAY0029',
    InvalidDescription: 'PLAY0145',
    EmptyDescription: 'PLAY0146',
    DuplicateDescription: 'PLAY0147',
    ExpectedCodeFence: 'PLAY0163',
    UnclosedCodeBlock: 'PLAY0164',
    InvalidReadModelDeclaration: 'PLAY0186',
    LegacyInlineCodeFence: 'PLAY0397',
    InvalidEventGeneration: 'PLAY0446',
} as const;
