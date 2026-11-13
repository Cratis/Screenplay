// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions.for_ExtensionPlaceholder;

public class when_checking_a_placeholder
{
    [Fact] void should_accept_static_self_contained_html() => ExtensionPlaceholder.Violations("<div style=\"color:red\"><img src=\"data:image/png;base64,AAAA\"></div>").ShouldBeEmpty();
    [Fact] void should_reject_a_script() => ExtensionPlaceholder.Violations("<script>1</script>").ShouldContain("contains a script element");
    [Fact] void should_reject_an_inline_handler() => ExtensionPlaceholder.Violations("<div onclick=\"x()\"></div>").ShouldContain("contains an inline event handler");
    [Fact] void should_reject_an_external_image() => ExtensionPlaceholder.Violations("<img src=\"https://example.com/a.png\">").Count.ShouldEqual(1);
    [Fact] void should_reject_an_external_stylesheet() => ExtensionPlaceholder.Violations("<link rel=\"stylesheet\" href=\"a.css\">").Count.ShouldEqual(1);
    [Fact] void should_reject_a_javascript_url() => ExtensionPlaceholder.Violations("<a href=\"javascript:x()\">x</a>").ShouldContain("contains a javascript: URL");
}
