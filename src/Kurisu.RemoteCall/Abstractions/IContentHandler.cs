namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IContentHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    HttpContent Create(List<ParameterValue> values);
}