// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { WorkspaceApplication } from '../../WorkspaceApplication';

export const placeOrder = 'slice StateChange PlaceOrder\n  command PlaceOrder\n    orderId OrderId identifier\n    authorize Staff\n    produces OrderPlaced\n      for orderId\n  event OrderPlaced\n    note String';

// An application of focused files: a root importing every document, a module file placing its folder in the
// module, and a slice file that names what the other files declare.
export function an_application(): WorkspaceApplication {
    const application = new WorkspaceApplication();
    application.set('application.play', 'concept OrderId : Uuid\npolicy Staff\n  require authenticated\nimport "**/*.play"');
    application.set('Ordering/Ordering.play', 'module Ordering\n  feature Orders\n    import "Orders/*.play"\n  import "Missing.play"');
    application.set('Ordering/Orders/PlaceOrder.play', placeOrder);
    return application;
}
