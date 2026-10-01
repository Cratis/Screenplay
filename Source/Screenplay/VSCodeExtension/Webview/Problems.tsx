// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BoardProblem } from './BoardMessage';

export interface ProblemsProps {
    readonly problems: readonly BoardProblem[];
    readonly onShowSource: (line?: number) => void;
}

// The errors the compiler found, above the board. The board still draws everything that could be read, so
// a typo in one slice does not empty it - but a reader has to know that what they see is incomplete.
export const Problems = ({ problems, onShowSource }: ProblemsProps) => {
    const errors = problems.filter(problem => problem.severity === 'error');
    if (errors.length === 0) {
        return null;
    }
    return (
        <div className='screenplay-board__problems' role='status'>
            <span>{errors.length === 1 ? '1 error' : `${errors.length} errors`} - the board shows what could be read.</span>
            <ul>
                {errors.slice(0, 5).map((problem, index) => (
                    <li key={index}>
                        <button type='button' onClick={() => onShowSource(problem.line)}>
                            {problem.path === undefined ? '' : `${problem.path} `}line {problem.line}
                        </button>
                        {` ${problem.code}: ${problem.message}`}
                    </li>
                ))}
            </ul>
        </div>
    );
};
