﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using GenHTTP.Api.Protocol;

namespace GenHTTP.Modules.Robots.Provider
{

    public class RobotsContent : IResponseContent
    {

        #region Get-/Setters

        public ulong? Length => null;

        private List<RobotsDirective> Directives { get; }

        private string? Sitemap { get; }

        #endregion

        #region Initialization

        public RobotsContent(List<RobotsDirective> directives, string? sitemap)
        {
            Directives = directives;
            Sitemap = sitemap;
        }

        #endregion

        #region Functionality

        public async Task Write(Stream target, uint bufferSize)
        {
            using (var writer = new StreamWriter(target, Encoding.UTF8, (int)bufferSize, true))
            {
                foreach (var directive in Directives)
                {
                    foreach (var agent in directive.UserAgents)
                    {
                        await writer.WriteLineAsync($"User-agent: {agent}");
                    }

                    foreach (var path in directive.Allowed)
                    {
                        await writer.WriteLineAsync($"Allow: {path}");
                    }

                    foreach (var path in directive.Disallowed)
                    {
                        await writer.WriteLineAsync($"Disallow: {path}");
                    }

                    await writer.WriteLineAsync();
                }

                if (Sitemap != null)
                {
                    await writer.WriteLineAsync($"Sitemap: {Sitemap}");
                }
            }
        }

        #endregion

    }

}
