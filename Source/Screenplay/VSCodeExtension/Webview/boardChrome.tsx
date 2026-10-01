// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BoardCanvasChrome } from '@cratis/event-models';

// The board's zoom and minimap controls as Cratis Studio draws them.
export const boardChrome: BoardCanvasChrome = {
    controlsLabels: { toggleMinimap: 'Toggle minimap', zoomOut: 'Zoom out', resetZoom: 'Reset zoom', zoomIn: 'Zoom in', help: 'Help' },
    controlsIcons: {
        toggleMinimap: <i className='pi pi-th-large' aria-hidden />,
        zoomOut: <i className='pi pi-minus' aria-hidden />,
        zoomIn: <i className='pi pi-plus' aria-hidden />,
        help: <i className='pi pi-question-circle' aria-hidden />,
    },
};
