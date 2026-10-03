// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';

// Without host context, a standalone model is the entire application.
export interface CodeActionOptions {
    readonly placement?: (model: editor.ITextModel) => readonly string[] | undefined;
    readonly otherDocuments?: (model: editor.ITextModel) => readonly string[];
}
