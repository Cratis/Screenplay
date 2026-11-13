// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CompletionEntry } from './completion-items';

/**
 * Completion entries for the `provide` declaration, offered where the application, a module, a feature or a slice may declare one.
 */
export const provideItems: CompletionEntry[] = [
    { label: 'provide from query', insertText: 'provide ${1:name} ${2:Type} from query ${3:QueryName} by ${4:$context.value}', documentation: 'Provides a value, read from a query, to everything beneath this level. The nearest declaration of a name wins.' },
    { label: 'provide from value', insertText: 'provide ${1:name} ${2:Type} from ${3:$context.tenant}', documentation: 'Provides a value taken from a literal, $context, $identity or a template to everything beneath this level.' },
    { label: 'provide from host', insertText: 'provide ${1:name} ${2:Type} from host', documentation: 'Provides a value the host supplies. The model cannot evaluate it, so it is opaque to checks.' },
    { label: 'provide with code', insertText: 'provide ${1:name} ${2:Type}\n    file ${3:Path}', documentation: 'Provides a value computed by code the model cannot evaluate. Name the file, or write a fenced code block.' },
];
