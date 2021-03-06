﻿using System.Net;
using System.IO;
using System.Text;
using Paydock_dotnet_sdk.Services;

namespace Paydock_dotnet_sdk.Tools
{
    public enum HttpMethod
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    /// <summary>
    /// Helper class to handle calling the API and basic serialisation
    /// </summary>
    public class ServiceHelper : IServiceHelper
    {
        static ServiceHelper()
        {
            // set to TLS1.2
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.DefaultConnectionLimit = 9999;
        }


        /// <summary>
        /// Call the API, throws ResponseException on any errors
        /// </summary>
        /// <param name="url">relative URL to call (eg charge or notification/templates)</param>
        /// <param name="method">HTTP method to call</param>
        /// <param name="json">Data to send, will be ignored for some HTTP methods</param>
        /// <param name="excludeSecretKey">Don't send secret key with the request, defaults to false</param>
        /// <param name="overrideConfigSecretKey">Use a custom secret key rather than the value in shared config, defaults to null</param>
        /// <returns>the response string</returns>
        public string CallPaydock(string endpoint, HttpMethod method, string json, bool excludeSecretKey = false, string overrideConfigSecretKey = null)
        {
            var url = Config.BaseUrl() + endpoint;
            var request = HttpWebRequest.Create((string)url);

            request.ContentType = "application/json";
            if (!excludeSecretKey)
            {
                if (string.IsNullOrEmpty(overrideConfigSecretKey))
                    request.Headers.Add("x-user-secret-key", Config.SecretKey);
                else
                    request.Headers.Add("x-user-secret-key", overrideConfigSecretKey);
            }

            request.Method = method.ToString();
			request.Timeout = Config.TimeoutMilliseconds;

			string result = "";

            if (method == HttpMethod.POST || method == HttpMethod.PUT)
            {
                using (var stream = request.GetRequestStream())
                {
					if (json == null)
						json = "";

                    var data = Encoding.UTF8.GetBytes(json);
                    stream.Write(data, 0, json.Length);
                }
            }

            WebResponse response = null;
            try
            {
                response = request.GetResponse();
            }

            catch (WebException ex)
            {
                ConvertException(ex);
            }

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                result = reader.ReadToEnd();
            }
            
            return result;
        }
        
        /// <summary>
        /// Parses and converts exception & response into common format
        /// </summary>
        /// <param name="exception">Exception to convert</param>
        private void ConvertException(WebException exception)
        {
			if (exception.Response == null)
			{
				ResponseExceptionFactory.CreateTimeoutException();
			}

            using (var reader = new StreamReader(exception.Response.GetResponseStream()))
            {
				var result = reader.ReadToEnd();
				ResponseExceptionFactory.CreateResponseException(result, exception);
			}
        }
    }
}
