using HHub.Shared.Enums;

namespace HHub.Shared.Attributes;

[AttributeUsage((AttributeTargets.Property | AttributeTargets.Field))]
public class CacheAttribute(CacheAttrributeType type) : Attribute
{
    public CacheAttrributeType Type { get; } = type;
}

