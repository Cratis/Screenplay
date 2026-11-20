// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax, CompilationResult } from '@cratis/screenplay-compiler';
import { dependencyMapFor, dependencyOverlayFor, screensOfApplication, timelineAnnotations, toEventModelDocument, withSliceAnnotations } from '@cratis/screenplay-event-models';
import { BoardProblem, ShowBoardMessage } from '../Webview/BoardMessage';

// The board for a compiled application: its document, the screens the board can draw, and the problems
// compiling found. The board is drawn whatever the problems are - everything that could be read is shown, and
// the problems say what is missing. A screen that could not be read back as a Scene document is a finding
// rather than a problem: the role keeps the board's placeholder window and the rest of the board is unaffected.
export function boardFor(compilation: CompilationResult<ApplicationSyntax>, name: string, wholeApplication: ApplicationSyntax = compilation.value): ShowBoardMessage {
    const screens = screensOfApplication(compilation.value);
    return {
        type: 'show',
        document: withSliceAnnotations(toEventModelDocument(compilation.value, name), timelineAnnotations(wholeApplication, compilation.diagnostics)),
        dependencies: dependencyMapFor(wholeApplication),
        overlay: dependencyOverlayFor(wholeApplication),
        screens: screens.documents,
        screenFindings: screens.findings.map(finding => ({ screen: finding.screen, message: finding.message })),
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
