using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Default;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 结果处理标记
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class RequestResultHandlerAttribute : Attribute
{
    /// <summary>
    /// 结果处理handler,使用<see cref="StandardRemoteCallResultHandler"/>
    /// </summary>
    public RequestResultHandlerAttribute()
    {
        HandlerType = typeof(StandardRemoteCallResultHandler);
    }

    /// <summary>
    /// 结果处理handler
    /// </summary>
    /// <param name="handlerType"><see cref="IRemoteCallResultHandler"/></param>
    public RequestResultHandlerAttribute(Type handlerType)
    {
        HandlerType = handlerType;
    }

    /// <summary>
    /// 处理类
    /// </summary>
    public Type HandlerType { get; }
}