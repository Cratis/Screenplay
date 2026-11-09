// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_Agents;

public class when_compiling_an_explicit_allowlist : given.an_agent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = Compiler.Compile(Model + "\n" + Allowlist);

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_token_kind() => _result.Value!.Agents!.Tokens!.Kind.ShouldEqual(AgentTokenKind.Personal);
    [Fact] void should_keep_authored_order() => _result.Value!.Agents!.Operations.Select(operation => operation.Kind).ShouldContainOnly(AgentOperationKind.Query, AgentOperationKind.Command);
    [Fact] void should_keep_the_authored_reference() => _result.Value!.Agents!.Operations.First().Reference.ShouldEqual("All");
    [Fact] void should_keep_the_explicit_scope() => _result.Value!.Agents!.Operations.First().Scope.ShouldEqual(AgentScope.Read);
    [Fact] void should_keep_the_literal_description() => _result.Value!.Agents!.Operations.First().Description.ShouldEqual("Find invoices");
}
