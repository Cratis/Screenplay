// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSyntaxIndex;

public class when_indexing_identity_metadata : Specification
{
    McpSyntaxIndex _index;

    void Because()
    {
        var application = new ScreenplayCompiler().Compile("identity\n  organization Organization optional from query Mine by $identity.id\nmodule Organizations\n  feature Membership\n    slice StateView View\n      readmodel Organization\n        name String\n      query Mine => Organization optional\n        by userId String").Value!;
        _index = new();
        _index.VisitApplication(application);
        _index.Complete(application);
    }

    [Fact] void should_list_the_block_as_metadata() => _index.Declarations.Single(declaration => declaration.Kind == "Identity").Name.ShouldEqual("identity");
    [Fact] void should_list_the_detail() => _index.Declarations.Single(declaration => declaration.Kind == "IdentityDetail").Name.ShouldEqual("organization");
    [Fact] void should_resolve_the_query_source() => _index.Resolve(_index.References.Single(reference => reference.Role == "identitySource")).Single().Name.ShouldEqual("Mine");
    [Fact] void should_resolve_the_detail_type() => _index.Resolve(_index.References.Single(reference => reference.Role == "identityDetailType")).Single().Name.ShouldEqual("Organization");
    [Fact] void should_own_source_references_by_the_detail() => _index.References.Single(reference => reference.Role == "identitySource").Owner!.Kind.ShouldEqual("IdentityDetail");
}
