using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ServiceComponents.AspNet.OpenApi
{
    /// <summary>
    /// Hiding specific endpoints
    /// </summary>
    public class ActionHidingConvention : IActionModelConvention
    {
        private readonly IList<Func<ActionModel, bool>> _filter;

        public ActionHidingConvention(IList<Func<ActionModel, bool>> filter)
        {
            _filter = filter;
        }

        public void Apply(ActionModel action)
        {
            if (_filter.Any(func => func(action))) {
                action.ApiExplorer.IsVisible = false;
            }
        }
    }

}
