using System.Reflection;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Proxy.Internal;

namespace Kurisu.RemoteCall.Proxy;

/// <summary>
/// 代理生成器
/// </summary>
internal class ProxyGenerator : DispatchProxy
{
    /// <summary>
    /// DispatchProxy需要无参构造函数
    /// </summary>
    // ReSharper disable once EmptyConstructor
    public ProxyGenerator()
    {
    }

    /// <summary>
    /// 代理实现
    /// </summary>
    protected IInterceptor Interceptor { get; set; }

    /// <summary>
    /// 代理对象
    /// </summary>
    protected internal object Target { get; set; }

    /// <summary>
    /// 代理接口
    /// </summary>
    protected internal Type InterfaceType { get; set; }

    private static readonly MethodInfo _createMethod = typeof(DispatchProxy).GetMethod(nameof(DispatchProxy.Create), BindingFlags.Static | BindingFlags.Public)!;

    public IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// 创建代理对象
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="target"></param>
    /// <param name="interfaceType"></param>
    /// <param name="interceptor"></param>
    /// <returns></returns>
    public static object Create(IServiceProvider serviceProvider, object target, Type interfaceType, IInterceptor interceptor)
    {
        ProxyGenerator proxy = (ProxyGenerator)_createMethod!.MakeGenericMethod(interfaceType, typeof(ProxyGenerator)).Invoke(null, null)!;
        proxy.Target = target;
        proxy.Interceptor = interceptor;
        proxy.InterfaceType = interfaceType;
        proxy.ServiceProvider = serviceProvider;
        return proxy;
    }

    /// <summary>
    /// 代理方法执行
    /// </summary>
    /// <param name="method"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    protected override object Invoke(MethodInfo method, object[] args)
    {
        var info = new ProxyInfo
        {
            Target = Target,
            Method = method,
            Parameters = args,
            InterfaceType = InterfaceType,
            ServiceProvider = ServiceProvider
        };

        if (info.IsInterceptor(Interceptor))
            Interceptor.Intercept(info);
        else
            info.Proceed();

        return info.ReturnValue;
    }
}