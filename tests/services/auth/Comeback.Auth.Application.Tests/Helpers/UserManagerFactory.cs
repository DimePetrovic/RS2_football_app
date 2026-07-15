namespace Comeback.Auth.Application.Tests.Helpers;

using Comeback.Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

internal static class UserManagerFactory
{
    internal static UserManager<ApplicationUser> Create()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        return Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);
    }
}
