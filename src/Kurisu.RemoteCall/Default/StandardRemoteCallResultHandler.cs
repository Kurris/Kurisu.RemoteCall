using System.Net;
using Kurisu.RemoteCall.Abstractions;
using Newtonsoft.Json;

namespace Kurisu.RemoteCall.Default;

internal class StandardRemoteCallResultHandler : IRemoteCallResultHandler
{
    /// <inheritdoc />
    public TResult Handle<TResult>(HttpStatusCode statusCode, string responseBody)
    {
        var apiResult = JsonConvert.DeserializeObject<ApiResult<TResult>>(responseBody);
        var isSuccessStatusCode = ((int)statusCode >= 200) && ((int)statusCode <= 299);
        if (!isSuccessStatusCode || apiResult.Code != 200)
        {
            if (!string.IsNullOrEmpty(apiResult?.Msg))
            {
                throw new Exception(apiResult.Msg);
            }

            throw new HttpRequestException(statusCode + " " + (int)statusCode);
        }

        return apiResult.Data;
    }

    internal class ApiResult<T>
    {
        /// <summary>
        /// 信息
        /// </summary>
        [JsonProperty(Order = 1)]
        public string Msg { get; set; }

        /// <summary>
        /// 结果内容
        /// </summary>
        [JsonProperty(Order = 2)]
        public T Data { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [JsonProperty(Order = 0)]
        public int Code { get; set; }
    }
}