// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CompletionEntry } from './completion-items';

/** The gate every module, feature, slice, command, query and screen accepts. Decided before authorization. */
export const availableItems: CompletionEntry[] = [
    { label: 'available for', insertText: 'available for ${1:Audience}', documentation: 'Offers this only to the named audiences. Combine audiences with `and` / `or` and parentheses. Decided before `authorize`; an unknown audience never opens the gate.' },
    { label: 'available when', insertText: 'available when ${1:\\$context.name} ${2|==,!=,>,>=,<,<=|} ${3:value}', documentation: 'Offers this only while a condition over `$context` holds, such as a value a `provide` declares.' },
    { label: 'available with', insertText: 'available with ${1:Availability}', documentation: 'Offers this only while the named availabilities hold.' },
];

/** The application-level declarations the gates refer to. */
export const availabilityDeclarationItems: CompletionEntry[] = [
    { label: 'audience', insertText: 'audience ${1:Name}\n    description "${2:who this audience is}"', documentation: 'Declares a named audience that gates (`available for`) and personas (`audience`) refer to.' },
    { label: 'availability', insertText: 'availability ${1:Name}\n    description "${2:when this holds}"\n    for ${3:Audience}', documentation: 'Declares a reusable availability: the lines `for`, `when` and `with` must all hold. Gates refer to it with `available with`.' },
];

/** What a persona lists inside its body. */
export const personaAudienceItems: CompletionEntry[] = [
    { label: 'audience', insertText: 'audience ${1:Name}', documentation: 'An audience this persona belongs to. Authoring only: it decides which gated screens and slices the persona sees.' },
];
