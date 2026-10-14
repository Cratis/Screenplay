// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Reports refusals from the shared model-root rules as the <see cref="McpFailure"/> the server has always raised.
/// </summary>
static class McpRootFailures
{
    internal static T Run<T>(Func<T> read)
    {
        try
        {
            return read();
        }
        catch (ModelRootRefused refused)
        {
            throw new McpFailure(refused.Message) { FailureKind = refused.McpFailureKind };
        }
    }

    internal static void Run(Action check) => Run(() =>
    {
        check();
        return true;
    });
}
