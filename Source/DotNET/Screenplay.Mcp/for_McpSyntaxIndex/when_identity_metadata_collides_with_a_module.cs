// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSyntaxIndex;

public class when_identity_metadata_collides_with_a_module : Specification
{
    const string Source =
        """
        identity
          organization Organization optional from query Mine by $identity.id
        module identity
          feature Membership
            slice StateView View
              readmodel Organization
              query Mine => Organization optional
                by userId String
        module Other
          feature Membership
            slice StateView View
              readmodel Organization
              query Mine => Organization optional
                by userId String
        """;
    McpSyntaxIndex _index;

    void Because()
    {
        var application = new ScreenplayCompiler().Parse(Source).Value!;
        _index = new();
        _index.VisitApplication(application);
        _index.Complete(application);
    }

    [Fact] void should_resolve_the_query_from_the_application() => _index.References.Single(reference => reference.Role == "identitySource").Scope.ShouldBeEmpty();
    [Fact] void should_keep_both_query_candidates() => _index.Resolve(_index.References.Single(reference => reference.Role == "identitySource")).Length.ShouldEqual(2);
    [Fact] void should_resolve_the_detail_type_from_the_application() => _index.References.Single(reference => reference.Role == "identityDetailType").Scope.ShouldBeEmpty();
    [Fact] void should_keep_both_type_candidates() => _index.Resolve(_index.References.Single(reference => reference.Role == "identityDetailType")).Length.ShouldEqual(2);
    [Fact] void should_keep_the_metadata_declaration_scope() => _index.Declarations.Single(declaration => declaration.Kind == "IdentityDetail").Scope.ShouldContainOnly("identity");
}
