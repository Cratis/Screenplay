// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BoardProblem, ScreenFinding } from './BoardMessage';

export interface ProblemsProps {
    readonly problems: readonly BoardProblem[];
    readonly screenFindings?: readonly ScreenFinding[];
    readonly onShowSource: (line?: number, path?: string) => void;
}

// The errors the compiler found, above the board. The board still draws everything that could be read, so
// a typo in one slice does not empty it - but a reader has to know that what they see is incomplete.
//
// Beneath them, what a screen said that the board could not draw. It is not a diagnostic - the model is
// correct and compiles - so it never counts as an error and never empties the board; it says why a screen on
// the board shows less than the .play reads, or why a cell shows the placeholder window instead of a screen.
export const Problems = ({ problems, screenFindings = [], onShowSource }: ProblemsProps) => {
    const errors = problems.filter(problem => problem.severity === 'error');
    if (errors.length === 0 && screenFindings.length === 0) {
        return null;
    }
    return (
        <div className={`screenplay-board__problems${errors.length === 0 ? ' screenplay-board__problems--notes' : ''}`} role='status'>
            {errors.length > 0 && <>
                <span>{errors.length === 1 ? '1 error' : `${errors.length} errors`} - the board shows what could be read.</span>
                <ul>
                    {errors.slice(0, 5).map((problem, index) => (
                        <li key={index}>
                            <button type='button' onClick={() => onShowSource(problem.line, problem.path)}>
                                {problem.path === undefined ? '' : `${problem.path} `}line {problem.line}
                            </button>
                            {` ${problem.code}: ${problem.message}`}
                        </li>
                    ))}
                </ul>
            </>}
            {screenFindings.length > 0 && <>
                <span className='screenplay-board__problems-note'>{screenFindings.length === 1 ? '1 screen note' : `${screenFindings.length} screen notes`} - the board draws the screens it could read.</span>
                <ul className='screenplay-board__problems-notes'>
                    {screenFindings.slice(0, 5).map((finding, index) => <li key={index}>{`${finding.screen}: ${finding.message}`}</li>)}
                </ul>
            </>}
        </div>
    );
};
