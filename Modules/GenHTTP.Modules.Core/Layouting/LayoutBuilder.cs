﻿using System;
using System.Collections.Generic;
using System.Text;

using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Routing;
using GenHTTP.Api.Modules;
using GenHTTP.Api.Modules.Templating;

namespace GenHTTP.Modules.Core.Layouting
{

    public class LayoutBuilder : IRouterBuilder
    {
        protected string? _Index;
        protected IRenderer<TemplateModel>? _Template;
        protected IContentProvider? _ErrorHandler;

        #region Get-/Setters
        
        protected Dictionary<string, IRouter> Routes { get; }

        protected Dictionary<string, IContentProvider> Content { get; }
        
        #endregion

        #region Initialization

        public LayoutBuilder()
        {
            Routes = new Dictionary<string, IRouter>();
            Content = new Dictionary<string, IContentProvider>();
        }

        #endregion

        #region Functionality

        public LayoutBuilder Index(string index)
        {
            _Index = index;
            return this;
        }

        public LayoutBuilder Template(IBuilder<IRenderer<TemplateModel>> template)
        {
            return Template(template.Build());
        }

        public LayoutBuilder Template(IRenderer<TemplateModel> template)
        {
            _Template = template;
            return this;
        }

        public LayoutBuilder ErrorHandler(IContentBuilder errorHandler)
        {
            return ErrorHandler(errorHandler.Build());
        }

        public LayoutBuilder ErrorHandler(IContentProvider errorHandler)
        {
            _ErrorHandler = errorHandler;
            return this;
        }

        public LayoutBuilder Add(string route, IRouterBuilder router, bool index = false)
        {
            return Add(route, router.Build());
        }

        public LayoutBuilder Add(string route, IRouter router, bool index = false)
        {
            Routes.Add(route, router);
            return (index) ? Index(route) : this;
        }

        public LayoutBuilder Add(string file, IContentBuilder content, bool index = false)
        {
            return Add(file, content.Build(), index);
        }

        public LayoutBuilder Add(string file, IContentProvider content, bool index = false)
        {
            Content.Add(file, content);
            return (index) ? Index(file) : this;
        }

        public IRouter Build()
        {
            return new LayoutRouter(Routes, Content, _Index, _Template, _ErrorHandler);
        }

        #endregion

    }

}