// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, type ErrorInfo, type ReactNode } from 'react';

export interface BoardErrorBoundaryProps {
    readonly children: ReactNode;
    // What the board is drawn with besides its document; when it changes, the board is tried again.
    readonly resetWhenChanged: unknown;
    // Puts the board back to how it is first shown, for when what it was asked to show cannot be drawn.
    readonly onReset: () => void;
}

interface BoardErrorBoundaryState {
    readonly error?: Error;
}

// Keeps a failure while drawing the board from emptying the whole webview: it says what failed, so it can
// be reported, and offers to show the board as it first appears. A change of presentation from elsewhere
// - another open board - tries the board again.
export class BoardErrorBoundary extends Component<BoardErrorBoundaryProps, BoardErrorBoundaryState> {
    override state: BoardErrorBoundaryState = {};

    static getDerivedStateFromError(error: Error): BoardErrorBoundaryState {
        return { error };
    }

    override componentDidUpdate(previous: BoardErrorBoundaryProps): void {
        if (this.state.error !== undefined && previous.resetWhenChanged !== this.props.resetWhenChanged) {
            this.setState({ error: undefined });
        }
    }

    override componentDidCatch(error: Error, info: ErrorInfo): void {
        console.error('The event model board failed to draw', error, info.componentStack);
    }

    override render() {
        const { error } = this.state;
        if (error === undefined) {
            return this.props.children;
        }
        return (
            <div className='screenplay-board__message screenplay-board__failure'>
                <p>The board failed to draw: {error.message}</p>
                <button type='button' onClick={() => {
                    this.props.onReset();
                    this.setState({ error: undefined });
                }}>Show the default view</button>
                <pre>{error.stack}</pre>
            </div>
        );
    }
}
