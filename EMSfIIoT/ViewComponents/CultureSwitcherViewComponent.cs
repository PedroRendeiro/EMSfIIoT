﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EMSfIIoT.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EMSfIIoT.ViewComponents
{
    public class CultureSwitcherViewComponent : ViewComponent
    {
        private readonly IOptions<RequestLocalizationOptions> localizationOptions;
        public CultureSwitcherViewComponent(IOptions<RequestLocalizationOptions> localizationOptions) =>
            this.localizationOptions = localizationOptions;

        public IViewComponentResult Invoke()
        {
            var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var model = new CultureSwitcherModel
            {
                SupportedCultures = localizationOptions.Value.SupportedUICultures.ToList(),
                CurrentUICulture = cultureFeature.RequestCulture.UICulture
            };
            return View(model);
        }
    }
}
