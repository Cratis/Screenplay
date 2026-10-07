// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax, CompilationResult } from '@cratis/screenplay-compiler';
import { dependencyMapFor, toEventModelDocument } from '@cratis/screenplay-event-models';
import { BoardProblem, ShowBoardMessage } from '../Webview/BoardMessage';

// The board for a compiled application: its document, and the problems compiling found. The board is drawn
// whatever the problems are - everything that could be read is shown, and the problems say what is missing.
export function boardFor(compilation: CompilationResult<ApplicationSyntax>, name: string, wholeApplication: ApplicationSyntax = compilation.value): ShowBoardMessage {
    return {
        type: 'show',
        document: toEventModelDocument(compilation.value, name),
        dependencies: dependencyMapFor(wholeApplication),
        problems: compilation.diagnostics.map(diagnostic => {
            const problem: BoardProblem = {
                severity: diagnostic.severity,
                code: diagnostic.code,
                message: diagnostic.message,
                line: diagnostic.location.line,
            };
            return diagnostic.location.path === undefined ? problem : { ...problem, path: diagnostic.location.path };
        }),
    };
}
