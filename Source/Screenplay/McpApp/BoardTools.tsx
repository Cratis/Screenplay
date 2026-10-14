// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ToolbarButton } from '@cratis/components/Toolbar';
import { DependencyView } from '@cratis/screenplay-views';
import { ShownModel } from './ShownModel';

export interface BoardToolsProps {
    // Which model is drawn, when a change is shown over the application.
    readonly shown?: ShownModel;
    // The toggle texts, which say what the change marks on each model; the plain names when absent.
    readonly currentLabel?: string;
    readonly proposedLabel?: string;
    readonly onShow: (shown: ShownModel) => void;
    readonly view: DependencyView;
    readonly onView: (view: DependencyView) => void;
    readonly fullscreen: boolean;
    readonly onRefresh?: () => void;
    readonly onToggleFullscreen?: () => void;
}

// The view's own tools in the board's toolbar: switching between the application as it is and as a change
// would leave it, drawing it again from the server, and fullscreen - each only where it can be used.
export const BoardTools = ({ shown, currentLabel = 'Current', proposedLabel = 'Proposed', onShow, view, onView, fullscreen, onRefresh, onToggleFullscreen }: BoardToolsProps) => (
    <>
        {shown !== undefined && (
            <>
                <ToolbarButton text={currentLabel} title='The application as it is' active={shown === ShownModel.Current} tooltipPosition='bottom'
                    onClick={() => onShow(ShownModel.Current)} />
                <ToolbarButton text={proposedLabel} title='The application as the change would leave it' active={shown === ShownModel.Proposed} tooltipPosition='bottom'
                    onClick={() => onShow(ShownModel.Proposed)} />
            </>
        )}
        <ToolbarButton text='Board' title='Event model board' active={view === DependencyView.Board} tooltipPosition='bottom' onClick={() => onView(DependencyView.Board)} />
        <ToolbarButton text='Map' title='Module and feature dependencies' active={view === DependencyView.Map} tooltipPosition='bottom' onClick={() => onView(DependencyView.Map)} />
        {onRefresh && <ToolbarButton icon='pi pi-refresh' title='Draw again from the model' tooltipPosition='bottom' onClick={onRefresh} />}
        {onToggleFullscreen && (
            <ToolbarButton icon={fullscreen ? 'pi pi-window-minimize' : 'pi pi-window-maximize'} title={fullscreen ? 'Exit fullscreen' : 'Fullscreen'}
                tooltipPosition='bottom' onClick={onToggleFullscreen} />
        )}
    </>
);
