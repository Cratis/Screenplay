// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_testing_a_root_with_persisted_identities : given.a_model
{
    string _previousId;
    int _exitCode;
    JsonElement _result;

    void Establish()
    {
        _previousId = ModelSource.Load(Root).Workspace.IdentityCatalog.Semantics
            .Single(assignment => assignment.Address.Kind == SemanticKind.Specification && assignment.Address.Name == "Correct").Id.ToString();
        ToolCommands.Run(["rename", "M.F.Register.Correct", "Renamed", Root], TextWriter.Null, TextWriter.Null).ShouldEqual(0);
    }

    void Because()
    {
        _exitCode = ModelTest.Run([Root, "--filter", "M.F.Register.Renamed", "--format", "json"], Output, Error);
        _result = JsonElement.Parse(Output.ToString()).GetProperty("results")[0];
    }

    [Fact] void should_pass() => _exitCode.ShouldEqual(0);
    [Fact] void should_report_the_identity_the_rename_kept() => _result.GetProperty("semanticId").GetString().ShouldEqual(_previousId);
}
