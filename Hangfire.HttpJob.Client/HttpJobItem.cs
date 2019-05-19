﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpClientFactory.Impl;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Client
{
    internal class HttpJobItem : PerHostHttpClientFactory
    {
        private readonly string _hangfireUrl;
        private readonly HangfireServerPostOption _httpPostOption;

        private HttpJobItem()
        {
            Method = "Post";
            ContentType = "application/json";
            Timeout = 20000;
            DelayFromMinutes = 15;
        }

        public HttpJobItem(string hangfireUrl, HangfireServerPostOption option) :this()
        {
            _hangfireUrl = hangfireUrl;
            _httpPostOption = option;
        }


        #region HttpJob
        /// <summary>
        /// 请求Url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string Data { get; set; }

        public string ContentType { get; set; }

        public int Timeout { get; set; }

        public int DelayFromMinutes { get; set; }
        public string Cron { get; set; }
        public string JobName { get; set; }
        public string QueueName { get; set; }

        /// <summary>
        /// 是否成功发送邮件
        /// </summary>
        public bool SendSucMail { get; set; }

        /// <summary>
        /// 是否失败发送邮件
        /// </summary>
        public bool SendFaiMail { get; set; }

        /// <summary>
        /// 指定发送邮件
        /// </summary>
        public string Mail { get; set; }

        /// <summary>
        /// 开启失败重启
        /// </summary>
        public bool EnableRetry { get; set; }

        public string BasicUserName { get; set; }
        public string BasicPassword { get; set; }
        #endregion

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <returns></returns>
        public async Task<HangfireAddJobResult> PostAsync()
        {
            var result = new HangfireAddJobResult();
            try
            {
                var client = GetHttpClient(this._hangfireUrl);
                var httpMesage = PrepareHttpRequestMessage();
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_httpPostOption.TimeOut));
                var httpResponse = await client.SendAsync(httpMesage, cts.Token);
                if (httpResponse.StatusCode != HttpStatusCode.NoContent)
                {
                    result.IsSuccess = false;
                    result.ErrMessage = httpResponse.StatusCode.ToString();
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrMessage = ex.Message;
                if (_httpPostOption.ThrowException) throw;
            }

            return new HangfireAddJobResult
            {
                IsSuccess = true
            };
        }
        public HangfireAddJobResult Post()
        {
            return PostAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }


        private  HttpRequestMessage PrepareHttpRequestMessage()
        {
            var request = new HttpRequestMessage(new HttpMethod("POST"), this._hangfireUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var data = JsonConvert.SerializeObject(this);
            var bytes = Encoding.UTF8.GetBytes(data);
            request.Content = new ByteArrayContent(bytes, 0, bytes.Length);
            if (!string.IsNullOrEmpty(_httpPostOption.BasicUserName) && !string.IsNullOrEmpty(_httpPostOption.BasicPassword))
            {
                var byteArray = Encoding.ASCII.GetBytes(_httpPostOption.BasicUserName + ":" + _httpPostOption.BasicPassword);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            return request;
        }

        #region 重写HttpClientFactory

        protected override HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.ConnectionClose = false;
            client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36");
            return client;
        }

        protected override HttpMessageHandler CreateMessageHandler()
        {
            var handler = new HttpClientHandler();
            if (_httpPostOption.WebProxy == null)
            {
                handler.UseProxy = false;
            }
            else
            {
                handler.Proxy = _httpPostOption.WebProxy;
            }

            handler.AllowAutoRedirect = false;

            handler.AutomaticDecompression = DecompressionMethods.None;

            handler.UseCookies = false;

            return handler;
        }
        #endregion
    }
}
