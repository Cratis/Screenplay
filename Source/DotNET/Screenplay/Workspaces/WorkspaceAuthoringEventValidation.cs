// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceAuthoringEventValidation
{
    internal static bool ArraysAreDefault(WorkspaceAuthoringRequest request) => request.EventRenames.IsDefault || request.EventRevisionAdvancements.IsDefault;

    internal static bool EndpointsAreMissing(WorkspaceAuthoringRequest request) =>
        request.EventRenames.Any(rename => rename is null || rename.PreviousAddress is null || rename.CurrentAddress is null) ||
        request.EventRevisionAdvancements.Any(advancement => advancement is null || advancement.Address is null);
}
