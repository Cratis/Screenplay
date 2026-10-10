// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_identity_metadata : given.a_compiler
{
    const string Source =
        """"
        identity
          description "Caller details"
          department String optional from claim "department"
          organization Organization optional from query MyOrganization by $identity.id
          partner Bool
            ```csharp
            return context.Identity.HasRole("Partner");
            ```
          external String
            file "Identity/External.cs"
        module Organizations
          feature Membership
            slice StateView Mine
              readmodel Organization
                name String
              query MyOrganization => Organization optional
                by userId String
        """";

    CompilationResult<ApplicationSyntax> _result;
    CompilationResult<ApplicationSyntax> _reparsed;
    string _printed;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _printed = new ScreenplayPrinter().Print(_result.Value!);
        _reparsed = _compiler.Compile(_printed);
    }

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_every_source() => _result.Value!.Identity!.Details.Select(detail => detail.Source.GetType()).ShouldContainOnly(typeof(ClaimIdentitySourceSyntax), typeof(QueryIdentitySourceSyntax), typeof(CodeIdentitySourceSyntax), typeof(FileIdentitySourceSyntax));
    [Fact] void should_keep_the_description() => _result.Value!.Identity!.Description.ShouldEqual("Caller details");
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_stably() => new ScreenplayPrinter().Print(_reparsed.Value!).ShouldEqual(_printed);

    [Theory]
    [InlineData("authentication\n  identity\n    department String from claim \"department\"", DiagnosticCodes.InvalidProviderDeclaration)]
    [InlineData("identity Name", DiagnosticCodes.InvalidIdentityDeclaration)]
    [InlineData("identity\nidentity", DiagnosticCodes.DuplicateIdentity)]
    [InlineData("identity\n  provider", DiagnosticCodes.InvalidIdentityDetail)]
    [InlineData("identity\n  department String from unknown Name", DiagnosticCodes.UnknownIdentitySource)]
    [InlineData("identity\n  department String", DiagnosticCodes.IdentityDetailWithoutSource)]
    [InlineData("identity\n  department String from claim \"department\"\n    cache forever", DiagnosticCodes.IdentitySourceWithBody)]
    [InlineData("identity\n  department String from query Q by $identity.id\n    cache forever", DiagnosticCodes.IdentitySourceWithBody)]
    [InlineData("identity\n  department String from claim \"a\"\n  department String from claim \"b\"", DiagnosticCodes.DuplicateIdentityDetail)]
    [InlineData("identity\n  id String from claim \"id\"", DiagnosticCodes.BuiltInIdentityDetail)]
    [InlineData("identity\n  department Missing from claim \"a\"", DiagnosticCodes.UnknownType)]
    [InlineData("identity\n  department String from query Missing by $identity.id", DiagnosticCodes.UnknownIdentityQuery)]
    [InlineData("identity\n  department String from query Missing", DiagnosticCodes.IdentityQueryWithoutKey)]
    [InlineData("identity\n  department String from query Missing by $identity.department", DiagnosticCodes.InvalidIdentityQueryKey)]
    [InlineData("identity\n  department String from query Missing by $context.tenant", DiagnosticCodes.InvalidIdentityQueryKey)]
    [InlineData("identity\n  department String from query Missing by department", DiagnosticCodes.InvalidIdentityQueryKey)]
    [InlineData("identity\n  department String\n    file \"one.cs\"\n    file \"two.cs\"", DiagnosticCodes.InvalidIdentityDetail)]
    void should_report_invalid_declarations(string source, string code) => _compiler.Compile(source).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);

    [Theory]
    [InlineData("$identity.department", false)]
    [InlineData("$identity.department.name", false)]
    [InlineData("$identity.missing", true)]
    [InlineData("$context.identity.department", true)]
    void should_resolve_only_the_new_root_against_details(string path, bool warning) => _compiler.Compile($"policy P\n  require claim \"x\" matches {path}\nidentity\n  department String from claim \"department\"").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownContextIdentityProperty).ShouldEqual(warning);

    [Theory]
    [InlineData("String[]", "by id String", "String", DiagnosticCodes.InvalidIdentityQuery)]
    [InlineData("String", "", "String", DiagnosticCodes.InvalidIdentityQuery)]
    [InlineData("String optional", "by id String", "String", DiagnosticCodes.IdentityQueryTypeMismatch)]
    [InlineData("String", "by id String", "Int", DiagnosticCodes.IdentityQueryTypeMismatch)]
    void should_validate_the_query_contract(string result, string key, string detailType, string code) => _compiler.Compile($"identity\n  department {detailType} from query Q by $identity.id\nmodule M\n  feature F\n    slice StateView V\n      query Q => {result}\n        {key}").Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);

    [Fact]
    void should_reject_a_query_authorized_by_a_detail() => _compiler.Compile("identity\n  department String from query Q by $identity.id\npolicy P\n  require claim \"department\" matches $identity.department\nmodule M\n  authorize P\n  feature F\n    slice StateView V\n      query Q => String\n        by id String").Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.IdentityQueryAuthorizationDependency);
}
