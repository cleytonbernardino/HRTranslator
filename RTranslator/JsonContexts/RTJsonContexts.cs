using RTranslator.Models;
using System.Text.Json.Serialization;

namespace RTranslator.JsonContexts;

[JsonSerializable(typeof(List<ExploreItem>))]
[JsonSerializable(typeof(ExploreItem))]
internal partial class RTJsonContexts : JsonSerializerContext
{
}
