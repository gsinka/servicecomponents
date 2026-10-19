using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using ServiceComponents.AspNet.Exceptions;
using ServiceComponents.Core.Exceptions;
using Xunit;

namespace DotNet.ErrorHandler
{
    // Stand-ins with the same full type names as the exceptions of the DotNet.ErrorHandler package
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}

namespace ServiceComponents.Test
{
    public class ErrorHandlerExceptionMappingTests
    {
        private const string TitleKey = "TEST@ERROR_TITLE";
        private const string ErrorCodeKey = "TEST@ERROR_ERROR_CODE";
        private const string HelpUrlsKey = "TEST@ERROR_HELP_URLS";

        private readonly JsonExceptionMapper _mapper = new();

        private async Task<HttpResponseMessage> Write(Exception exception)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await _mapper.WriteResponse(exception, context.Response);

            context.Response.Body.Position = 0;
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

            return new HttpResponseMessage((HttpStatusCode)context.Response.StatusCode) {
                Content = new StringContent(body, Encoding.UTF8, context.Response.ContentType)
            };
        }

        [Fact]
        public async Task Response_is_problem_details_for_the_frontend()
        {
            var exception = new DotNet.ErrorHandler.BusinessException("something failed");
            exception.Data[TitleKey] = "Some title";
            exception.Data[ErrorCodeKey] = "E-42";
            exception.Data["TEST@ERROR_CONTEXT"] = "ANALYZE";
            exception.Data["TEST@ERROR_HINT"] = "Try again";
            exception.Data["TEST@ERROR_UTC_TIMESTAMP"] = new DateTime(2026, 10, 8, 1, 2, 3, DateTimeKind.Utc);

            var inner = new Exception("unit not valid");
            inner.Data["TEST@ERROR_HINT"] = "Check the unit";
            exception.Data["TEST@ERROR_ISERVICE_ERROR"] = inner;

            var response = await Write(exception);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal(400, json.Value<int>("status"));
            Assert.Equal("Some title", json.Value<string>("title"));
            Assert.Equal("something failed", json.Value<string>("detail"));
            Assert.Equal("E-42", json.Value<string>("errorCode"));
            Assert.Equal("ANALYZE", json.Value<string>("contextCode"));
            Assert.Equal("Try again", json.Value<string>("hint"));
            Assert.Equal("unit not valid", json["iServiceErrorObject"]!.Value<string>("text"));
            Assert.Equal("Check the unit", json["iServiceErrorObject"]!.Value<string>("hint"));
            Assert.Equal(new DateTime(2026, 10, 8, 1, 2, 3, DateTimeKind.Utc), json.Value<DateTime>("timestamp").ToUniversalTime());
        }

        [Fact]
        public async Task Extra_fields_of_the_service_error_reach_the_frontend_unchanged_through_the_bff()
        {
            var exception = new DotNet.ErrorHandler.BusinessException("something failed");
            var inner = new Exception("requested unit unique-id not valid");
            inner.Data["hint"] = "Requested Unit UniqueId not valid";
            inner.Data["module"] = "/go/src/bl_datatypes_service_patch_datatypes_by_id.go";
            inner.Data["source"] = "iservice-rads/solution/go.DatatypesApiService.uniqueUnitID";
            inner.Data["line"] = "247";
            inner.Data["code"] = 1915;
            inner.Data["status"] = 400;
            inner.Data["links"] = "?errorCode=1915=1915";
            exception.Data["TEST@ERROR_ISERVICE_ERROR"] = inner;

            // MS -> BFF
            var restored = await Assert.ThrowsAsync<BusinessException>(() => _mapper.ThrowExceptionIfNeeded(Write(exception).Result));
            // BFF -> frontend
            var json = JObject.Parse(await (await Write(restored)).Content.ReadAsStringAsync());

            var serviceError = json["iServiceErrorObject"]!;
            Assert.Equal("requested unit unique-id not valid", serviceError.Value<string>("text"));
            Assert.Equal("Requested Unit UniqueId not valid", serviceError.Value<string>("hint"));
            Assert.Equal("/go/src/bl_datatypes_service_patch_datatypes_by_id.go", serviceError.Value<string>("module"));
            Assert.Equal("iservice-rads/solution/go.DatatypesApiService.uniqueUnitID", serviceError.Value<string>("source"));
            Assert.Equal("247", serviceError.Value<string>("line"));
            Assert.Equal(1915, serviceError.Value<int>("code"));
            Assert.Equal(400, serviceError.Value<int>("status"));
            Assert.Equal("?errorCode=1915=1915", serviceError.Value<string>("links"));
        }

        [Fact]
        public async Task Public_properties_of_the_service_error_exception_are_sent_too()
        {
            var exception = new DotNet.ErrorHandler.BusinessException("something failed");
            exception.Data["TEST@ERROR_ISERVICE_ERROR"] = new ServiceErrorStandIn("text", "the hint", "the links");

            var json = JObject.Parse(await (await Write(exception)).Content.ReadAsStringAsync());

            var serviceError = json["iServiceErrorObject"]!;
            Assert.Equal("the hint", serviceError.Value<string>("hint"));
            Assert.Equal("the links", serviceError.Value<string>("links"));
            Assert.Null(serviceError["message"]);
        }

        private class ServiceErrorStandIn : Exception
        {
            public string Hint { get; }
            public string Links { get; }

            public ServiceErrorStandIn(string message, string hint, string links) : base(message)
            {
                Hint = hint;
                Links = links;
            }
        }

        [Fact]
        public async Task Restored_core_exception_with_metadata_is_problem_details_too()
        {
            var exception = new NotFoundException("missing");
            exception.Data[TitleKey] = "Some title";

            var response = await Write(exception);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        }

        [Fact]
        public async Task Core_exception_without_metadata_keeps_the_legacy_response()
        {
            var response = await Write(new BusinessException(12, "legacy"));
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal("business", json.Value<string>("Type"));
        }

        [Theory]
        [InlineData(typeof(DotNet.ErrorHandler.BusinessException), HttpStatusCode.BadRequest, typeof(BusinessException))]
        [InlineData(typeof(DotNet.ErrorHandler.NotFoundException), HttpStatusCode.NotFound, typeof(NotFoundException))]
        [InlineData(typeof(DotNet.ErrorHandler.ConflictException), HttpStatusCode.Conflict, typeof(ConflictException))]
        public async Task Exception_round_trips_with_metadata(Type thrown, HttpStatusCode expectedStatus, Type expectedException)
        {
            var exception = (Exception)Activator.CreateInstance(thrown, "something failed")!;
            exception.Data[TitleKey] = "Some title";
            exception.Data[ErrorCodeKey] = "E-42";
            exception.Data[HelpUrlsKey] = new List<string> { "http://help/1", "http://help/2" };
            exception.Data["OTHER_KEY"] = "not transferred";

            var response = await Write(exception);

            Assert.Equal(expectedStatus, response.StatusCode);

            var rethrown = await Assert.ThrowsAsync(expectedException, () => _mapper.ThrowExceptionIfNeeded(response));

            Assert.Equal("something failed", rethrown.Message);
            Assert.Equal("Some title", rethrown.Data[TitleKey]);
            Assert.Equal("E-42", rethrown.Data[ErrorCodeKey]);
            Assert.Equal("[\"http://help/1\",\"http://help/2\"]", rethrown.Data[HelpUrlsKey]);
            Assert.False(rethrown.Data.Contains("OTHER_KEY"));
        }

        [Theory]
        [InlineData("<html>Bad gateway</html>")]
        [InlineData("{\"message\":\"no type here\"}")]
        [InlineData("")]
        public async Task Unknown_body_does_not_break_the_mapping(string body)
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent(body) };

            var exception = await Assert.ThrowsAsync<Exception>(() => _mapper.ThrowExceptionIfNeeded(response));

            Assert.Contains("502", exception.Message);
        }

        [Fact]
        public async Task Conflict_status_without_known_body_type_maps_to_conflict()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Conflict) {
                Content = new StringContent("{\"type\":\"generic\",\"errorMessage\":\"conflicting\"}")
            };

            await Assert.ThrowsAsync<ConflictException>(() => _mapper.ThrowExceptionIfNeeded(response));
        }
    }
}
