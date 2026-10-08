// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CompletionEntry } from './completion-items';

export const exampleDeclarationItems: CompletionEntry[] = [
    { label: 'example', insertText: 'example ${1:Name} : ${2:Type}\n    ${3:property} = ${4:value}', documentation: 'One possibly partial event, command or read-model fixture. Step assignments override its values; no implicit defaults.' },
];
