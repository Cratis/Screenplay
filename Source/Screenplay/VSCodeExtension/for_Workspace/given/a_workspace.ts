// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { reset, state } from '../../vscode.stub';

// A workspace holding exactly the files a spec names, so what the probe asks the editor for is measured
// against a real glob rather than against a double that answers by comparing base names.
export function a_workspace_holding(files: readonly string[], folder = '/repo'): void {
    reset();
    state.workspaceFolder = folder;
    for (const file of files) state.files.add(file);
}
