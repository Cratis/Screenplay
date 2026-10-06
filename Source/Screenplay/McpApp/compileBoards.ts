// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type PlayFileSource } from '@cratis/screenplay-compiler';
import { compileEventModelApplication, toEventModelDocument } from '@cratis/screenplay-event-models';
import { applyChanges } from './applyChanges';
import type { CompiledBoard } from './CompiledBoard';
import type { CompiledBoards } from './CompiledBoards';
import type { VisualizedModel } from './VisualizedModel';

// Compiles what the server sent into the boards the view draws. Both are compiled the same way and their
// identities come from names, so switching between them keeps what the board remembers - collapsed
// modules, the viewport - for everything the change leaves in place.
export function compileBoards(model: VisualizedModel): CompiledBoards {
    const current = compile(applyChanges(model.documents, []), model.application);
    if (!model.changes) {
        return { current };
    }
    return { current, proposed: compile(applyChanges(model.documents, model.changes), model.application) };
}

function compile(sources: readonly PlayFileSource[], application: string): CompiledBoard {
    const compilation = compileEventModelApplication(sources);
    return {
        document: toEventModelDocument(compilation.value, application),
        errors: compilation.diagnostics.filter(diagnostic => diagnostic.severity === 'error').length,
    };
}
