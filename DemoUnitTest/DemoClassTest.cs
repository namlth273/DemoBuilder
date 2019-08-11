using AutoFixture;
using DemoBuilder;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DemoUnitTest
{
    public class DemoClassTest
    {
        private IFixture _fixture;
        private IDemoClient _demoClient;
        private HttpRequestMessage _request;
        private Product _response;
        private FluentMockServer _server;
        private HttpClient _client;

        [OneTimeSetUp]
        public void SetupOne()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            _server = FluentMockServer.Start();
        }

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();

            _server.Reset();

            _client = new HttpClient
            {
                BaseAddress = new Uri(_server.Urls.First())
            };

            _fixture.Inject(_client);

            _demoClient = _fixture.Create<DemoClient>();

            _response = _fixture.Create<Product>();

            _fixture.Customize<HttpRequestMessage>(c =>
                c.OmitAutoProperties()
                    .With(w => w.Method, HttpMethod.Post)
                    .With(w => w.Content, new JsonContent(_response))
                    .With(w => w.RequestUri, new Uri(_server.Urls.First() + "/token")));

            _request = _fixture.Create<HttpRequestMessage>();
        }

        [Test]
        public async Task CanSend()
        {
            var request = Request.Create().WithBody(new JmesPathMatcher($"id == '{_response.Id}'")).WithPath("/token")
                .UsingPost();

            _server.Given(request).RespondWith(Response.Create()
                .WithHeaderContentTypeAsJson()
                .WithSuccess()
                .WithBodyAsJson(_response));

            var response = await _demoClient.SendAsync<Product>(_request);

            response.ShouldNotBeNull();
            response.Id.ShouldBe(_response.Id);
        }

        [Test]
        public void SendUnAuthorized()
        {
            var request = Request.Create()
                .WithPath("/token")
                .UsingPost()
                .WithHeader("api-key", "*", MatchBehaviour.RejectOnMatch)
                .UsingAnyMethod();

            _server.Given(request).RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Unauthorized)
                .WithBody(@"{ ""result"": ""api-key missing""}"));

            Assert.ThrowsAsync<UnAuthorizedException>(() => _demoClient.SendAsync<Product>(_request));
        }

        [Test]
        public void CanNotSend()
        {
            var request = Request.Create().WithPath("/token").UsingGet();

            _server.Given(request).RespondWith(Response.Create().WithNotFound());

            var exception = Assert.ThrowsAsync<NotFoundException>(() => _demoClient.SendAsync<Product>(_request));

            exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [OneTimeTearDown]
        public void ShutdownServer()
        {
            _server.Stop();
        }
    }

    public static class WireMockExtensions
    {
        public static IResponseBuilder WithHeaderContentTypeAsJson(this IResponseBuilder builder)
        {
            return builder.WithHeader("Content-Type", "application/json");
        }
    }

    public class JsonContent : StringContent
    {
        public JsonContent(object content) : base(JsonConvert.SerializeObject(content))
        {
        }
    }
}