// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BoardProblem, BoardScreenFinding } from './BoardMessage';

export interface ProblemsProps {
    readonly problems: readonly BoardProblem[];

    // What a screen said that could not be drawn. Not a problem with the model - the role simply keeps the
    // board's placeholder window - so it is listed apart from the errors and never counted among them.
    readonly screenFindings?: readonly BoardScreenFinding[];
    readonly onShowSource: (line?: number, path?: string) => void;
}

// The errors the compiler found, above the board. The board still draws everything that could be read, so
// a typo in one slice does not empty it - but a reader has to know that what they see is incomplete.
export const Problems = ({ problems, screenFindings = [], onShowSource }: ProblemsProps) => {
    const errors = problems.filter(problem => problem.severity === 'error');
    if (errors.length === 0 && screenFindings.length === 0) {
        return null;
    }
    return (
        <div className='screenplay-board__problems' role='status'>
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
            {screenFindings.length > 0 && <div className='screenplay-board__screen-findings'>
                <span>{screenFindings.length === 1 ? '1 screen' : `${screenFindings.length} screens`} drawn as a placeholder window.</span>
                <ul>
                    {screenFindings.slice(0, 5).map((finding, index) => <li key={index}>{`${finding.screen}: ${finding.message}`}</li>)}
                </ul>
            </div>}
        </div>
    );
};
