// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_UiProfileShellBinder.given;

public class a_profile_bound_to_a_catalog : Specification
{
    protected static readonly TemplateDescriptor AppShell = new("AppShell", TemplateDescriptorKind.Layout)
    {
        Slots = ["sidebar", "content"],
        Options = ["navigationMode"]
    };

    protected static readonly TemplateDescriptor NoContent = new("Bare", TemplateDescriptorKind.Layout)
    {
        Slots = ["sidebar"]
    };

    protected IReadOnlyList<BlueprintDescriptor> _catalog;
    protected IReadOnlyList<Diagnostic> _diagnostics;

    void Establish() => _catalog = [new("Cratis.Blueprint.Default", "1.4.2", [AppShell, NoContent])];

    protected IReadOnlyList<Diagnostic> Bind(string source, IReadOnlyList<BlueprintDescriptor>? catalog = null)
    {
        var compiled = new ScreenplayCompiler().Compile(source);
        return UiProfileShellBinder.Bind(compiled.Value!, catalog ?? _catalog);
    }
}
