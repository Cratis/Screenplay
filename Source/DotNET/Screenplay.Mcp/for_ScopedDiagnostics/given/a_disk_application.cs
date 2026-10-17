// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics.given;

public class a_disk_application : Specification
{
    protected string Root;
    protected bool Resolved;
    protected ScopedDiagnosticResult? Result;
    protected ScopeSelectionError? Error;

    void Establish() => Root = SpecFixtureDirectory.Create("scoped-diagnostics");

    void Destroy() => SpecFixtureDirectory.Delete(Root);
}
