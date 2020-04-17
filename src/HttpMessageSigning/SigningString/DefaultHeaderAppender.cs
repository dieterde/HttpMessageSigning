using System;
using System.Linq;

namespace Dalion.HttpMessageSigning.SigningString {
    internal class DefaultHeaderAppender : IHeaderAppender {
        private readonly HttpRequestForSigning _request;
        
        public DefaultHeaderAppender(HttpRequestForSigning request) {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public string BuildStringToAppend(HeaderName header) {
            var isPresent = _request.Headers.TryGetValues(header, out var headerValues);
            var sanitizedHeaderValues = headerValues.Select(SanitizeHeaderValue).Where(v => v != null).ToArray();
            return isPresent
                ? "\n" + new Header(header, sanitizedHeaderValues)
                : string.Empty;
        }

        private static string SanitizeHeaderValue(string input) {
            if (input == null) return null;
            var lines = input.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", lines.Select(l => l.Trim()));
        }
    }
}