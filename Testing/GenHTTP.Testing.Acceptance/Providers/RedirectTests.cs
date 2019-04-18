﻿using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using Xunit;

using GenHTTP.Modules.Core;
using GenHTTP.Testing.Acceptance.Domain;

namespace GenHTTP.Testing.Acceptance.Providers
{

    public class RedirectTests
    {

        /// <summary>
        /// As a developer, I would like to temporarily redirect requests
        /// to another location.
        /// </summary>
        [Fact]
        public void TestTemporary()
        {
            var redirect = Redirect.To("https://google.de/", true);

            var router = Layout.Create().Add("_", redirect, true);

            using var runner = TestRunner.Run(router);

            using var response = runner.GetResponse();

            Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
            Assert.Equal("https://google.de/", response.Headers["Location"]);
        }

        /// <summary>
        /// As a developer, I would like to permanently redirect requests
        /// to another location.
        /// </summary>
        [Fact]
        public void TestPermanent()
        {
            var redirect = Redirect.To("https://google.de/");

            var router = Layout.Create().Add("_", redirect, true);

            using var runner = TestRunner.Run(router);

            using var response = runner.GetResponse();

            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal("https://google.de/", response.Headers["Location"]);
        }

    }

}
