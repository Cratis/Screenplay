// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax, CompilationResult } from '@cratis/screenplay-compiler';
import { dependencyMapFor, dependencyOverlayFor, screensOfApplication, timelineAnnotations, toEventModelDocument, withSliceAnnotations } from '@cratis/screenplay-event-models';
import { BoardProblem, ShowBoardMessage } from '../Webview/BoardMessage';

// The board for a compiled application: its document, the Scene documents of its screens, and the problems
// compiling found. The board is drawn whatever the problems are - everything that could be read is shown, and
// the problems say what is missing.
//
// The screens are converted from the same slices the document was mapped from, so every screen is keyed by the
// slice and role the board asks for. A screen the bridge could not convert is a finding, never an error: that
// role keeps the board's own placeholder window.
export function boardFor(compilation: CompilationResult<ApplicationSyntax>, name: string, wholeApplication: ApplicationSyntax = compilation.value): ShowBoardMessage {
    const screens = screensOfApplication(compilation.value);
    return {
        type: 'show',
        document: withSliceAnnotations(toEventModelDocument(compilation.value, name), timelineAnnotations(wholeApplication, compilation.diagnostics)),
        dependencies: dependencyMapFor(wholeApplication),
        overlay: dependencyOverlayFor(wholeApplication),
        screens: screens.documents,
        screenFindings: screens.findings,
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
