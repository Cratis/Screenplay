// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CompletionEntry } from './completion-items';

export const documentationItem: CompletionEntry = {
    label: 'documentation',
    insertText: 'documentation\n    ```markdown\n    ${1:Reasoning, assumptions or rejected alternatives}\n    ```',
    documentation: 'One nonempty authoring-only Markdown block; report-only (PLAY0270), with no executable effect.',
};
