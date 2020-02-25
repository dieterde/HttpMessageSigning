using System;
using System.Net.Http;
using Dalion.HttpMessageSigning.Logging;
using Dalion.HttpMessageSigning.SigningString;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace Dalion.HttpMessageSigning.Signing {
    public class SignatureCreatorTests {
        private readonly ISigningStringComposer _signingStringComposer;
        private readonly ISignatureAlgorithmFactory _signatureAlgorithmFactory;
        private readonly IBase64Converter _base64Converter;
        private readonly IHttpMessageSigningLogger<SignatureCreator> _logger;
        private readonly SignatureCreator _sut;

        public SignatureCreatorTests() {
            FakeFactory.Create(out _base64Converter, out _signatureAlgorithmFactory, out _signingStringComposer, out _logger);
            _sut = new SignatureCreator(_signingStringComposer, _signatureAlgorithmFactory, _base64Converter, _logger);
        }

        public class CreateSignature : SignatureCreatorTests {
            private readonly HttpRequestMessage _httpRequest;
            private readonly SigningSettings _settings;
            private readonly DateTimeOffset _timeOfSigning;

            public CreateSignature() {
                _httpRequest = new HttpRequestMessage {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("http://dalion.eu/api/resource/id1")
                };
                _settings = new SigningSettings {
                    Expires = TimeSpan.FromMinutes(5),
                    SignatureAlgorithm = SignatureAlgorithm.RSA,
                    HashAlgorithm = HashAlgorithm.SHA512,
                    ClientKey = new ClientKey {
                        Id = new KeyId("client1"),
                        Secret = new Secret("s3cr3t")
                    },
                    Headers = new[] {
                        HeaderName.PredefinedHeaderNames.RequestTarget,
                        HeaderName.PredefinedHeaderNames.Date,
                        HeaderName.PredefinedHeaderNames.Expires,
                        new HeaderName("dalion_app_id")
                    }
                };
                _timeOfSigning = new DateTimeOffset(2020, 2, 24, 11, 20, 14, TimeSpan.Zero);
            }

            [Fact]
            public void GivenNullRequest_ThrowsArgumentNullException() {
                Action act = () => _sut.CreateSignature(null, _settings, _timeOfSigning);
                act.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void GivenNullSettings_ThrowsArgumentNullException() {
                Action act = () => _sut.CreateSignature(_httpRequest, null, _timeOfSigning);
                act.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void GivenInvalidSettings_ThrowsValidationException() {
                _settings.ClientKey = null; // Make invalid
                Action act = () => _sut.CreateSignature(_httpRequest, _settings, _timeOfSigning);
                act.Should().Throw<ValidationException>();
            }

            [Fact]
            public void ReturnsSignatureWithCalculatedSignatureString() {
                var composedString = "{the composed string}";
                A.CallTo(() => _signingStringComposer.Compose(_httpRequest, _settings, _timeOfSigning))
                    .Returns(composedString);

                var hashAlgorithm = A.Fake<ISignatureAlgorithm>();
                A.CallTo(() => _signatureAlgorithmFactory.Create(_settings.SignatureAlgorithm, _settings.HashAlgorithm, _settings.ClientKey.Secret))
                    .Returns(hashAlgorithm);

                var signatureHash = new byte[] {0x03, 0x04};
                A.CallTo(() => hashAlgorithm.ComputeHash(composedString))
                    .Returns(signatureHash);

                var signatureString = "xyz=";
                A.CallTo(() => _base64Converter.ToBase64(signatureHash))
                    .Returns(signatureString);

                var actual = _sut.CreateSignature(_httpRequest, _settings, _timeOfSigning);

                actual.String.Should().Be(signatureString);
            }

            [Fact]
            public void ReturnsSignatureWithExpectedKeyId() {
                var actual = _sut.CreateSignature(_httpRequest, _settings, _timeOfSigning);
                actual.KeyId.Should().Be(_settings.ClientKey.Id);
            }

            [Fact]
            public void ReturnsSignatureWithExpectedAlgorithm() {
                var actual = _sut.CreateSignature(_httpRequest, _settings, _timeOfSigning);
                actual.Algorithm.Should().Be("rsa-sha512");
            }

            [Fact]
            public void ReturnsSignatureWithExpectedCreatedValue() {
                var actual = _sut.CreateSignature(_httpRequest, _settings, _timeOfSigning);
                actual.Created.Should().Be(_timeOfSigning);
            }

            [Fact]
            public void ReturnsSignatureWithExpectedExpiresValue() {
                var actual = _sut.CreateSignature(_httpRequest, _settings, _timeOfSigning);
                actual.Expires.Should().Be(_timeOfSigning.Add(_settings.Expires));
            }

            [Fact]
            public void ReturnsSignatureWithExpectedHeaders() {
                var actual = _sut.CreateSignature(_httpRequest, _settings, _timeOfSigning);
                actual.Headers.Should().BeEquivalentTo(
                    new[] {
                        HeaderName.PredefinedHeaderNames.RequestTarget,
                        HeaderName.PredefinedHeaderNames.Date,
                        HeaderName.PredefinedHeaderNames.Expires,
                        new HeaderName("dalion_app_id")
                    },
                    options => options.WithStrictOrdering());
            }
        }
    }
}