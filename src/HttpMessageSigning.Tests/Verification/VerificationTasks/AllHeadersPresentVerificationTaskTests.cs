using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Dalion.HttpMessageSigning.Verification.VerificationTasks {
    public class AllHeadersPresentVerificationTaskTests {
        private readonly AllHeadersPresentVerificationTask _sut;

        public AllHeadersPresentVerificationTaskTests() {
            _sut = new AllHeadersPresentVerificationTask();
        }

        public class Verify : AllHeadersPresentVerificationTaskTests {
            private readonly HttpRequestForSigning _signedRequest;
            private readonly Client _client;
            private readonly Signature _signature;
            private readonly Func<HttpRequestForSigning, Signature, Client, Task<Exception>> _method;

            public Verify() {
                _signature = (Signature)TestModels.Signature.Clone();
                _signedRequest = (HttpRequestForSigning)TestModels.Request.Clone();
                _client = (Client)TestModels.Client.Clone();
                _method = (request, signature, client) => _sut.Verify(request, signature, client);
            }

            [Fact]
            public async Task WhenSignatureDoesNotContainRequestTarget_ReturnsVerificationException() {
                _signature.Headers = _signature.Headers
                    .Where(h => h != HeaderName.PredefinedHeaderNames.RequestTarget)
                    .ToArray();

                var actual = await _method(_signedRequest, _signature, _client);

                actual.Should().NotBeNull().And.BeAssignableTo<SignatureVerificationException>();
            }

            [Theory]
            [InlineData("RSA")]
            [InlineData("HMAC")]
            [InlineData("ECDSA")]
            public async Task WhenSignatureShouldContainDateHeader_ButItDoesnt_ReturnsVerificationException(string algorithm) {
                var client = new Client(_client.Id, new CustomSignatureAlgorithm(algorithm));
                _signature.Algorithm = algorithm + "-sha256";
                _signature.Headers = _signature.Headers
                    .Where(h => h != HeaderName.PredefinedHeaderNames.Date)
                    .ToArray();

                var actual = await _method(_signedRequest, _signature, client);

                actual.Should().NotBeNull().And.BeAssignableTo<SignatureVerificationException>();
            }

            [Fact]
            public async Task WhenSignatureShouldNotContainDateHeader_AndItDoesnt_ReturnsNull() {
                var client = new Client(_client.Id, new CustomSignatureAlgorithm("hs2019"));
                _signature.Algorithm = "hs2019";
                _signature.Headers = _signature.Headers
                    .Where(h => h != HeaderName.PredefinedHeaderNames.Date)
                    .Concat(new[] {HeaderName.PredefinedHeaderNames.Created})
                    .Concat(new[] {HeaderName.PredefinedHeaderNames.Expires})
                    .ToArray();
                _signedRequest.Headers.Add(HeaderName.PredefinedHeaderNames.Created, _signature.Created.Value.ToUnixTimeSeconds().ToString());
                _signedRequest.Headers.Add(HeaderName.PredefinedHeaderNames.Expires, _signature.Expires.Value.ToUnixTimeSeconds().ToString());
                
                var actual = await _method(_signedRequest, _signature, client);

                actual.Should().BeNull();
            }

            [Fact]
            public async Task WhenSignatureShouldContainCreatedHeader_ButItDoesnt_ReturnsVerificationException() {
                var client = new Client(_client.Id, new CustomSignatureAlgorithm("hs2019"));
                _signature.Headers = _signature.Headers
                    .Where(h => h != HeaderName.PredefinedHeaderNames.Created)
                    .ToArray();

                var actual = await _method(_signedRequest, _signature, client);

                actual.Should().NotBeNull().And.BeAssignableTo<SignatureVerificationException>();
            }

            [Theory]
            [InlineData("RSA")]
            [InlineData("HMAC")]
            [InlineData("ECDSA")]
            public async Task WhenSignatureShouldNotContainCreatedHeader_AndItDoesnt_ReturnsNull(string algorithm) {
                var client = new Client(_client.Id, new CustomSignatureAlgorithm(algorithm));
                _signature.Algorithm = algorithm + "-sha256";
                _signature.Headers = _signature.Headers
                    .Where(h => h != HeaderName.PredefinedHeaderNames.Created)
                    .ToArray();

                var actual = await _method(_signedRequest, _signature, client);

                actual.Should().BeNull();
            }

            [Fact]
            public async Task WhenSignatureShouldContainExpiresHeader_ButItDoesnt_ReturnsVerificationException() {
                var client = new Client(_client.Id, new CustomSignatureAlgorithm("hs2019"));
                _signature.Algorithm = "hs2019";
                _signature.Headers = _signature.Headers
                    .Where(h => h != HeaderName.PredefinedHeaderNames.Expires)
                    .ToArray();

                var actual = await _method(_signedRequest, _signature, client);

                actual.Should().NotBeNull().And.BeAssignableTo<SignatureVerificationException>();
            }

            [Theory]
            [InlineData("RSA")]
            [InlineData("HMAC")]
            [InlineData("ECDSA")]
            public async Task WhenSignatureShouldNotContainExpiresHeader_AndItDoesnt_ReturnsNull(string algorithm) {
                var client = new Client(_client.Id, new CustomSignatureAlgorithm(algorithm));
                _signature.Algorithm = algorithm + "-sha256";
                _signature.Headers = _signature.Headers
                    .Where(h => h != HeaderName.PredefinedHeaderNames.Expires)
                    .ToArray();

                var actual = await _method(_signedRequest, _signature, client);

                actual.Should().BeNull();
            }

            [Fact]
            public async Task WhenOneOrMoreHeadersIsMissing_ReturnsVerificationException() {
                _signedRequest.Headers.Remove("dalion-app-id");

                var actual = await _method(_signedRequest, _signature, _client);

                actual.Should().NotBeNull().And.BeAssignableTo<SignatureVerificationException>();
            }

            [Fact]
            public async Task WhenAllRequestedHeadersArePresent_ReturnsNull() {
                var actual = await _method(_signedRequest, _signature, _client);

                actual.Should().BeNull();
            }
        }
    }
}