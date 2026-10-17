// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_collecting_failures.given;

public class an_application : Specification
{
    protected SemanticApplication _application;
    protected SemanticSlice _first;
    protected SemanticSlice _second;

    void Establish()
    {
        _first = Slice("First");
        _second = Slice("Second");
        _application = Application(_first, _second);
    }

    protected static SemanticId Id(SemanticKind kind, string name) => SemanticId.Create(kind, name);

    protected static SemanticSlice Slice(string name) => new(Id(SemanticKind.Slice, name), name, SemanticSliceKind.StateChange, [], [], [], [], [], []);

    protected static SemanticApplication Application(params SemanticSlice[] slices) => new(
        Id(SemanticKind.Application, "Application"),
        "Application",
        [],
        [],
        [new(Id(SemanticKind.Module, "Module"), "Module", [new(Id(SemanticKind.Feature, "Feature"), "Feature", [], [.. slices])])]);
}
