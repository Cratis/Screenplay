// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Api.for_OpenApiDocument;

public class when_writing_a_model : for_ApiDescription.given.a_lending_model
{
    byte[] _bytes;
    JsonObject _document;

    void Because()
    {
        _bytes = OpenApiDocument.Write(ApiDescription.For(Model));
        _document = JsonNode.Parse(_bytes)!.AsObject();
    }

    JsonObject Path(string route, string method) => _document["paths"]![route]![method]!.AsObject();

    [Fact] void should_be_openapi_3_1() => _document["openapi"]!.GetValue<string>().ShouldEqual("3.1.0");
    [Fact] void should_title_the_document_after_the_application() => _document["info"]!["title"]!.GetValue<string>().ShouldEqual(Model.Application.Name);
    [Fact] void should_version_the_document_by_model_revision() => _document["info"]!["version"]!.GetValue<string>().ShouldEqual(Model.Revision.ToString());
    [Fact] void should_describe_every_route() => _document["paths"]!.AsObject().Select(path => path.Key).ShouldContainOnly(
    [
        "/api/lending/catalog/add-book",
        "/api/lending/catalog/add-book/validate",
        "/api/lending/catalog/shelve-book/shelve-book",
        "/api/lending/catalog/shelve-book/shelve-book/validate",
        "/api/lending/catalog/shelve-book/retire-book",
        "/api/lending/catalog/shelve-book/retire-book/validate",
        "/api/lending/catalog/catalog-list/all-books",
        "/api/lending/catalog/catalog-list/book-by-id"
    ]);
    [Fact] void should_refer_the_command_body_to_its_schema() => Path("/api/lending/catalog/add-book", "post")["requestBody"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>().ShouldEqual("#/components/schemas/AddBook");
    [Fact] void should_require_the_command_inputs() => _document["components"]!["schemas"]!["AddBook"]!["required"]!.AsArray().Select(name => name!.GetValue<string>()).ShouldContainOnly(["bookId", "title"]);
    [Fact] void should_describe_a_concept_by_its_primitive() => _document["components"]!["schemas"]!["BookId"]!["format"]!.GetValue<string>().ShouldEqual("uuid");
    [Fact] void should_mark_an_observable_query() => Path("/api/lending/catalog/catalog-list/all-books", "get")["x-screenplay-observable"]!["transport"]!.GetValue<string>().ShouldEqual("websocket");
    [Fact] void should_not_mark_a_snapshot_query() => Path("/api/lending/catalog/catalog-list/book-by-id", "get").ContainsKey("x-screenplay-observable").ShouldBeFalse();
    [Fact] void should_return_a_list_of_read_models() => Path("/api/lending/catalog/catalog-list/all-books", "get")["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["allOf"]![1]!["properties"]!["data"]!["items"]!["$ref"]!.GetValue<string>().ShouldEqual("#/components/schemas/CatalogEntry");
    [Fact] void should_state_the_required_policy() => Path("/api/lending/catalog/add-book", "post")["x-screenplay-authorization"]!["requires"]!.GetValue<string>().ShouldEqual("Librarian");
    [Fact] void should_state_an_anonymous_operation() => Path("/api/lending/catalog/catalog-list/book-by-id", "get")["x-screenplay-authorization"]!["anonymous"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_spell_every_policy_condition() => _document["x-screenplay-policies"]!["Librarian"]!.GetValue<string>().ShouldEqual("role \"Librarian\"");
    [Fact] void should_write_the_same_bytes_every_time() => OpenApiDocument.Write(ApiDescription.For(Model)).ShouldEqual(_bytes);
}
