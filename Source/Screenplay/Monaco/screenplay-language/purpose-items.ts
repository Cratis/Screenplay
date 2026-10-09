// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CompletionEntry } from './completion-items';

export const purposeReferenceItem: CompletionEntry = { label: 'purpose', insertText: 'purpose ${1:Name}', documentation: 'Adds a declared processing purpose to the union covering this slice and its enclosing containers.' };

export const purposeItems: CompletionEntry[] = [
    { label: 'description', insertText: 'description "${1:processing intent}"', documentation: 'Describes the processing, not a legal verdict.' },
    { label: 'basis', insertText: 'basis ${1|consent,contract,legalObligation,vitalInterests,publicTask,legitimateInterests|}', documentation: 'One Art. 6(1) lawful basis, with an optional quoted reference.' },
    { label: 'interest', insertText: 'interest "${1:legitimate interests pursued}"', documentation: 'Statement accompanying basis legitimateInterests.' },
    { label: 'condition', insertText: 'condition ${1|explicitConsent,employmentLaw,vitalInterests,notForProfit,madePublic,legalClaims,substantialPublicInterest,healthCare,publicHealth,research|}', documentation: 'Art. 9(2) condition for special-category data.' },
    { label: 'authorization', insertText: 'authorization "${1:authorization in law}"', documentation: 'Art. 10 authorization for criminal-offence data.' },
    { label: 'subjects', insertText: 'subjects ${1:customer}', documentation: 'Comma-separated open identifiers for categories of data subjects.' },
    { label: 'retention', insertText: 'retention "${1:period or criteria}"', documentation: 'Declared retention, not enforced.' },
    { label: 'recipient', insertText: 'recipient "${1:recipient category}"', documentation: 'Repeatable recipient category.' },
    { label: 'transfer', insertText: 'transfer "${1:destination}" safeguard "${2:safeguard}"', documentation: 'Repeatable destination and safeguards.' },
    { label: 'erasure exception', insertText: 'erasure exception ${1|expression,legalObligation,publicTask,publicHealth,archiving,legalClaims|}', documentation: 'Declared Art. 17(3) exception; does not change Chronicle per-subject erasure.' },
];
