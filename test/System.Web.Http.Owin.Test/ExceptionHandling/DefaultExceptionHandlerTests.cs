﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.Owin.ExceptionHandling
{
    public class DefaultExceptionHandlerTests
    {
        [Fact]
        public void HandleAsync_IfContextIsNull_Throws()
        {
            // Arrange
            IExceptionHandler product = CreateProductUnderTest();
            ExceptionHandlerContext context = null;
            CancellationToken cancellationToken = CancellationToken.None;

            // Act & Assert
            Assert.ThrowsArgumentNull(() => product.HandleAsync(context, cancellationToken), "context");
        }

        [Fact]
        public void HandleAsync_IfExceptionIsNull_Throws()
        {
            // Arrange
            IExceptionHandler product = CreateProductUnderTest();
            ExceptionHandlerContext context = CreateContext(new ExceptionContext());
            Assert.Null(context.ExceptionContext.Exception); // Guard
            CancellationToken cancellationToken = CancellationToken.None;

            // Act & Assert
            Assert.ThrowsArgument(() => product.HandleAsync(context, cancellationToken), "context",
                "ExceptionContext.Exception must not be null.");
        }

        [Fact]
        public void HandleAsync_IfRequestIsNull_Throws()
        {
            // Arrange
            IExceptionHandler product = CreateProductUnderTest();
            ExceptionHandlerContext context = new ExceptionHandlerContext(new ExceptionContext
            {
                Exception = CreateException()
            });
            Assert.Null(context.ExceptionContext.Request); // Guard
            CancellationToken cancellationToken = CancellationToken.None;

            // Act & Assert
            Assert.ThrowsArgument(() => product.HandleAsync(context, cancellationToken), "context",
                "ExceptionContext.Request must not be null.");
        }

        [Fact]
        public void HandleAsync_HandlesExceptionViaCreateErrorResponse()
        {
            IExceptionHandler product = CreateProductUnderTest();

            // Arrange
            using (HttpRequestMessage expectedRequest = CreateRequest())
            {
                expectedRequest.SetRequestContext(new HttpRequestContext { IncludeErrorDetail = true });
                ExceptionHandlerContext context = CreateValidContext(expectedRequest);
                CancellationToken cancellationToken = CancellationToken.None;

                // Act
                Task task = product.HandleAsync(context, cancellationToken);
                task.WaitUntilCompleted();

                // Assert
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                IHttpActionResult result = context.Result;
                Assert.IsType(typeof(ResponseMessageResult), result);
                ResponseMessageResult typedResult = (ResponseMessageResult)result;
                using (HttpResponseMessage response = typedResult.Response)
                {
                    Assert.NotNull(response);

                    using (HttpResponseMessage expectedResponse = expectedRequest.CreateErrorResponse(
                        HttpStatusCode.InternalServerError, context.ExceptionContext.Exception))
                    {
                        AssertErrorResponse(expectedResponse, response);
                    }

                    Assert.Same(expectedRequest, response.RequestMessage);
                }
            }
        }

        private static void AssertErrorResponse(HttpResponseMessage expected, HttpResponseMessage actual)
        {
            Assert.NotNull(expected); // Guard
            Assert.IsType(typeof(ObjectContent<HttpError>), expected.Content); // Guard
            ObjectContent<HttpError> expectedContent = (ObjectContent<HttpError>)expected.Content;
            Assert.NotNull(expectedContent.Formatter); // Guard

            Assert.NotNull(actual);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.IsType(typeof(ObjectContent<HttpError>), actual.Content);
            ObjectContent<HttpError> actualContent = (ObjectContent<HttpError>)actual.Content;
            Assert.NotNull(actualContent.Formatter);
            Assert.Same(expectedContent.Formatter.GetType(), actualContent.Formatter.GetType());
            Assert.Equal(expectedContent.Value, actualContent.Value);
            Assert.Same(expected.RequestMessage, actual.RequestMessage);
        }

        private static ExceptionHandlerContext CreateContext(ExceptionContext exceptionContext)
        {
            return new ExceptionHandlerContext(exceptionContext);
        }

        private static Exception CreateException()
        {
            return new NotSupportedException();
        }

        private static DefaultExceptionHandler CreateProductUnderTest()
        {
            return new DefaultExceptionHandler();
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage();
        }

        private static ExceptionHandlerContext CreateValidContext(HttpRequestMessage request)
        {
            return CreateContext(new ExceptionContext
            {
                Exception = CreateException(),
                Request = request
            });
        }
    }
}
