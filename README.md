## What is HeimGuard?

HeimGuard is a small and simple library, inspired by [PolicyServer](https://policyserver.io/) and [this talk](https://www.youtube.com/watch?v=Dlrf85NTuAU) by Dominick Baier, built to allow you to easily manage permissions in your .NET projects.

## Quickstart

Thankfully for us, .NET makes it very easy to protect a controller using a specific policy using the `Authorize` attribute, so let's start there:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("recipes")]
[Authorize(Policy = "RecipesFullAccess")]
public class RecipesController : ControllerBase
{    
    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}
```

Next, I'm going to put my user's role in my `ClaimPrincipal`. This isn't required for HeimGuard to work, but is what we'll use for this example.

```json	
{
  "sub": "145hfy662",
  "name": "John Smith",
  ...
  "role": ["Chef"]
}
```

Now I'm going to implement an interface from HeimGuard called `UserPolicyHandler`. This handler is responsible for implementing your permissions lookup for your user. It should return an `IEnumerable<string>` that stores all of the permissions that your user has available to them. 

**HeimGuard doesn't care how you store permissions and how you access them.** For simplicity sake in the example below, I'm just grabbing a static list, but this could just as easily come from a database or some external administration boundary and could be in whatever shape you want.

```csharp
using System.Security.Claims;
using HeimGuard;
using Services;

public class Permission 
{
    public string Name { get; set; }
    public List<string> Roles { get; set; }
}

public static class DummyPermissionStore
{
    public static List<Permission> GetPermissions()
    {
        return new List<Permission>()
        {
            new()
            {
                Name = "RecipesFullAccess",
                Roles = new List<string>() { "Chef" }
            }
        };
    }
}

public class SimpleUserPolicyHandler : IUserPolicyHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserPolicyHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<IEnumerable<string>> GetUserPermissions()
    {
        var user = _currentCurrentUserService.User;
        if (user == null) throw new ArgumentNullException(nameof(user));
				
      	// this gets the user's role(s) from their ClaimsPrincipal
        var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(r => r.Value).ToArray();
      
      	// this gets there permissions based on their roles. in this example, it's just using a static list
        var permissions = DummyPermissionStore.GetPermissions()
            .Where(p => p.Roles.Any(r => roles.Contains(r)))
            .Select(p => p.Name)
          	.ToArray();

        return await Task.FromResult(permissions.Distinct());
    }
}
```

Now, all we have to do is register our `SimpleUserPolicyHandler` with `AddHeimGuard` and we're good to go:

```c#
public void ConfigureServices(IServiceCollection services)
{
	  //... other services
    services.AddHeimGuard<SimpleUserPolicyHandler>()
      .MapAuthorizationPolicies()
      .AutomaticallyCheckPermissions();
}
```

You'll notice two other methods extending `AddHeimGuard`. Nether are required, but they do make your life easier.

- `MapAuthorizationPolicies` will automatically map authorization attributes to ASP.NET Core authorization policies that haven't already been mapped. That means you don't have to do this for all your policies:

  ```csharp
  services.AddAuthorization(options =>
  {
      options.AddPolicy("RecipesFullAccess",
              policy => policy.RequireClaim("permission", "RecipesFullAccess"));
  });
  ```

- `AutomaticallyCheckPermissions` will automatically checks user permissions when an authorization attribute is used. Again, this is optional, but without this, we would need to add something like this to our controller:

  ```csharp
  using HeimGuard;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  
  [ApiController]
  [Route("recipes")]
  [Authorize(Policy = "RecipesFullAccess")]
  public class RecipesController : ControllerBase
  {
      private readonly IHeimGuard _heimGuard;
  
      public RecipesController(IHeimGuard heimGuard)
      {
          _heimGuard = heimGuard;
      }
      
      [HttpGet]
      public IActionResult Get()
      {
          _heimGuard.HasPermissionAsync("RecipesFullAccess");
          return Ok();
      }
  }
  ```

