using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求content内容处理
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class RequestContentHandlerAttribute : Attribute
{
    /// <summary>
    /// type is <see cref="IContentHandler"/>
    /// </summary>
    /// <param name="type"></param>
    public RequestContentHandlerAttribute(Type type)
    {
        Handler = type;
    }

    /// <summary>
    /// 处理类
    /// </summary>
    public Type Handler { get; set; }
}