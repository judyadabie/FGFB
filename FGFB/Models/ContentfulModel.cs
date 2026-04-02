using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FGFB.Models
{

    public class ContentfulResponse
    {
        [JsonPropertyName("sys")]
        public SysTypeInfo? Sys { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("skip")]
        public int Skip { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("items")]
        public List<Item>? Items { get; set; }

        [JsonPropertyName("includes")]
        public Includes? Includes { get; set; }
    }

    public class SysTypeInfo
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("metadata")]
        public Metadata? Metadata { get; set; }

        [JsonPropertyName("sys")]
        public ItemSys? Sys { get; set; }

        [JsonPropertyName("fields")]
        public Fields? Fields { get; set; }
    }

    public class Metadata
    {
        [JsonPropertyName("tags")]
        public List<object>? Tags { get; set; }

        [JsonPropertyName("concepts")]
        public List<object>? Concepts { get; set; }
    }

    public class ItemSys
    {
        [JsonPropertyName("space")]
        public SysContainer? Space { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("environment")]
        public SysContainer? Environment { get; set; }

        [JsonPropertyName("publishedVersion")]
        public int? PublishedVersion { get; set; }

        [JsonPropertyName("revision")]
        public int? Revision { get; set; }

        [JsonPropertyName("contentType")]
        public SysContainer? ContentType { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }
    }

    public class SysContainer
    {
        [JsonPropertyName("sys")]
        public LinkSys? Sys { get; set; }
    }

    public class LinkSys
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("linkType")]
        public string? LinkType { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class Fields
    {
        [JsonPropertyName("blogBody")]
        public RichTextDocument? BlogBody { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("headerImage")]
        public SysContainer? HeaderImage { get; set; }

        [JsonPropertyName("thumbnail")]
        public SysContainer? Thumbnail { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author")]
        public SysContainer? Author { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public SysContainer? Picture { get; set; }
    }

    public class RichTextDocument
    {
        [JsonPropertyName("data")]
        public Dictionary<string, JsonElement>? Data { get; set; }

        [JsonPropertyName("content")]
        public List<RichTextNode>? Content { get; set; }

        [JsonPropertyName("nodeType")]
        public string? NodeType { get; set; }
    }

    public class RichTextNode
    {
        [JsonPropertyName("data")]
        public Dictionary<string, JsonElement>? Data { get; set; }

        [JsonPropertyName("content")]
        public List<RichTextNode>? Content { get; set; }

        [JsonPropertyName("marks")]
        public List<Mark>? Marks { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("nodeType")]
        public string? NodeType { get; set; }
    }

    public class Mark
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class Includes
    {
        [JsonPropertyName("Asset")]
        public List<Asset>? Asset { get; set; }
        [JsonPropertyName("Entry")]
        public List<Item>? Entry { get; set; }

    }

    public class Asset
    {
        [JsonPropertyName("metadata")]
        public Metadata? Metadata { get; set; }

        [JsonPropertyName("sys")]
        public ItemSys? Sys { get; set; }

        [JsonPropertyName("fields")]
        public AssetFields? Fields { get; set; }
    }

    public class AssetFields
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("file")]
        public AssetFile? File { get; set; }
    }

    public class AssetFile
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("details")]
        public AssetFileDetails? Details { get; set; }

        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }
    }

    public class AssetFileDetails
    {
        [JsonPropertyName("size")]
        public int? Size { get; set; }

        [JsonPropertyName("image")]
        public AssetImage? Image { get; set; }
    }

    public class AssetImage
    {
        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }
    }
}