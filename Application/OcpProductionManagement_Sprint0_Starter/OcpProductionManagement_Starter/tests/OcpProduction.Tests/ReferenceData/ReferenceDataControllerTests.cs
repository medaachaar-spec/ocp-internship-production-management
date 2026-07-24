using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Security;
using OcpProduction.Web.Controllers;
using Xunit;

namespace OcpProduction.Tests.ReferenceData;

public sealed class ReferenceDataControllerTests
{
    [Fact]
    public void Controller_uses_CanAdminister_policy()
    {
        var authorize = Assert.Single(typeof(ReferenceDataController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(PolicyNames.CanAdminister, authorize.Policy);
    }

    [Fact]
    public void All_post_actions_use_antiforgery_and_no_delete_action_exists()
    {
        var actions = typeof(ReferenceDataController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.DeclaringType == typeof(ReferenceDataController))
            .ToList();

        var postActions = actions.Where(action => action.GetCustomAttribute<HttpPostAttribute>() is not null).ToList();

        Assert.NotEmpty(postActions);
        Assert.All(postActions, action => Assert.NotNull(action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
        Assert.DoesNotContain(actions, action => action.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
    }
}
