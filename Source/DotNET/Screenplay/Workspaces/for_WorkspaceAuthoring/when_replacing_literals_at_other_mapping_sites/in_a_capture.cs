// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_literals_at_other_mapping_sites;

public class in_a_capture : given.a_document_with_other_mapping_sites
{
    void Because() => ReplaceLiteral("capture");

    [Fact] void should_preserve_every_other_byte() => AssertExact("capture");
}
