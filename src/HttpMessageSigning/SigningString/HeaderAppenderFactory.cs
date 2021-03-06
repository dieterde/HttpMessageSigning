using System;

namespace Dalion.HttpMessageSigning.SigningString {
    internal class HeaderAppenderFactory : IHeaderAppenderFactory {
        public IHeaderAppender Create(HttpRequestForSigning request, DateTimeOffset? timeOfComposing, TimeSpan? expires) {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return new CompositeHeaderAppender(
                new DefaultHeaderAppender(request), 
                new RequestTargetHeaderAppender(request), 
                new CreatedHeaderAppender(timeOfComposing),
                new ExpiresHeaderAppender(timeOfComposing, expires),
                new DateHeaderAppender(request));
        }
    }
}