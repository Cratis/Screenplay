// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, type ErrorInfo, type ReactNode } from 'react';

export interface BoardErrorBoundaryProps {
    readonly children: ReactNode;
    // What the board is drawn with besides its document; when it changes, the board is tried again.
    readonly resetWhenChanged: unknown;
    // Puts the board back to how it is first shown, for when what it was asked to show cannot be drawn.
    readonly onReset: () => void;
    // What is drawn, named in the failure message. Defaults to the board.
    readonly subject?: string;
    // The view's tools, kept in reach when drawing fails because the failed view's own toolbar goes with it.
    readonly tools?: ReactNode;
}

interface BoardErrorBoundaryState {
    readonly error?: Error;
}

// Keeps a failure while drawing the board from emptying the whole view: it says what failed, so it can be
// reported, and offers to draw the board again. Switching to the other model tries it again too.
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
        console.error(`The ${this.props.subject ?? 'event model board'} failed to draw`, error, info.componentStack);
    }

    override render() {
        const { error } = this.state;
        if (error === undefined) {
            return this.props.children;
        }
        const subject = this.props.subject ?? 'board';
        return (
            <>
            {this.props.tools && <div className='screenplay-dependency-map-toolbar' role='toolbar' aria-label='Model and view'>{this.props.tools}</div>}
            <div className='screenplay-mcp-board__message screenplay-mcp-board__failure'>
                <p>The {subject} failed to draw: {error.message}</p>
                <button type='button' onClick={() => {
                    this.props.onReset();
                    this.setState({ error: undefined });
                }}>Draw the {subject} again</button>
                <pre>{error.stack}</pre>
            </div>
            </>
        );
    }
}
