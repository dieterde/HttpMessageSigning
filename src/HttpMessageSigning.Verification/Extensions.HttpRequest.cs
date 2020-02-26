using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Dalion.HttpMessageSigning.Verification {
    public static partial class Extensions {
        internal static HttpRequestForSigning ToRequestForSigning(this HttpRequest request) {
            if (request == null) return null;

            var requestMessage = new HttpRequestForSigning {
                RequestUri = new Uri(request.GetEncodedUrl(), UriKind.Absolute),
                Method = string.IsNullOrEmpty(request.Method)
                    ? HttpMethod.Get
                    : new HttpMethod(request.Method)
            };

            foreach (var header in request.Headers) {
                requestMessage.Headers[header.Key] = header.Value;
            }

            return requestMessage;
        }
    }
}