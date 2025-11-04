using Dfe.Complete.Application.Projects.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace Dfe.Complete.Tests.MockData
{
    public static class PageDataHelper
    {
        public static PageContext GetPageContext()
        {
            var expectedUser = GetUser();
            var claims = new List<Claim> { new("objectidentifier", expectedUser?.EntraUserObjectId!) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            var httpContext = new DefaultHttpContext()
            {
                User = principal
            };

            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);

            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);

            return new PageContext(actionContext)
            {
                ViewData = viewData
            };
        }

        public static UserDto GetUser()
        {
            return new UserDto { EntraUserObjectId = "test-ad-id", FirstName = "Test", LastName = "User", Team = "Support team" };
        }
    }

}
