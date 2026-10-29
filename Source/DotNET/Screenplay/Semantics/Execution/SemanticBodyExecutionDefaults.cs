// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

static class SemanticBodyExecutionDefaults
{
    internal static string NotImplemented(string kind, SemanticImplementationBody body) =>
        $"{kind} body '{body.Requirement.Member ?? body.Requirement.RequirementId}' (requirement {body.Requirement.RequirementId}) is not supported by this provider.";
}
