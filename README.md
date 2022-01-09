<p>
    <a href="https://github.com/pdevito3/heimguard/releases"><img src="https://img.shields.io/nuget/v/heimguard.svg" alt="Latest Release"></a>   
    <a href="https://github.com/pdevito3/heimguard/blob/master/LICENSE.txt"><img src ="https://img.shields.io/github/license/mashape/apistatus.svg?maxAge=2592000" alt="License"></a>
</p>

## What is HeimGuard?

HeimGuard is a small and simple library, inspired by [PolicyServer](https://policyserver.io/) and [this talk](https://www.youtube.com/watch?v=Dlrf85NTuAU) by Dominick Baier, built to allow you to easily manage permissions in your .NET projects.

>  ⭐️ HeimGuard is simple and flexible by design and I hope that it will help many development teams in the .NET community. With that said, it is quite bare bones. If you need a more robust solution, I strongly recommend checking out the commercial version of [Policy Server](https://solliance.net/products/policyserver) for a much more expansive list of features and capabilities.

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
  "aud": ["api1", "api2"],
  "role": ["Chef"]
}
```

Now I'm going to implement an interface from HeimGuard called `IUserPolicyHandler`. This handler is responsible for implementing your permissions lookup for your user. It should return an `IEnumerable<string>` that stores all of the permissions that your user has available to them. 

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
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) throw new ArgumentNullException(nameof(user));
				
      	// this gets the user's role(s) from their ClaimsPrincipal
        var roles = user.Claims
          .Where(c => c.Type == ClaimTypes.Role)
          .Select(r => r.Value)
          .ToArray();
      
      	// this gets their permissions based on their roles. in this example, it's just using a static list
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
      .AutomaticallyCheckPermissions()
      .MapAuthorizationPolicies();
}
```

You'll notice two other methods extending `AddHeimGuard`. Nether are required, but they do make your life easier. For more details, check out [HeimGuard Enhancements](#heimguard-enhancements).

## Introduction

Let's start by differentiating 3 different levels of permissions:

- **Application access**: these are generally configured in your auth server and passed along in your token (e.g. using audience (`aud`) claim to determine what apis a token can be used in).
- **Feature access**: permission specific check in a particular application boundary (e.g. can a user perform some action).
- **Application logic**: custom business logic specific to your application (e.g. given this certain set of criteria, can a user perform some action).

The goal with HeimGuard is to easily manage user’s permissions around the feature access scope of permissions in your .NET apps using the built in .NET policies you’re familiar with.

Out of the box with .NET, we can easily decorate our controllers like this `[Authorize(Policy = "RecipesFullAccess")]` and register it in `AddAuthorization`, but there's a gap here, **how do we check if the user has that claim?**

One of the most common solutions to this is to load up your policies in your security token.

Identity is the input to your permissions that, together, determine a user's permissions.

```json
{
  "sub": "145hfy662",
  "name": "John Smith",
  "aud": ["api1", "api2"],
  "permission": [
    "ManageRecipe",
    "CreateNewRecipe",
    "UpdateIngredients"
  ]
}
```

This can work, but but there are some downside here:

- Your JWT gets quickly overloaded, potentially to the point of being too big to even put into a cookie. Ideally, your token is only passing along user identity information only.
- You don't have boundary permission context. Let's look at a couple examples:
  - As mentioned above, we generally use the `aud` claim (or maybe some custom one) to determine what apis your security token can be used in. So in the example above we have `  "aud": ["api1", "api2"],` and one of my permissions is `ManageRecipe`. What if I am allowed to manage recipes in `api1` but not `api2`? You could prefix them with something like `api1.ManageRecipe`, but that adds coupling, domain logic, and becomes a huge multipler in the amount of claims being passed around.
  - Say I have a permission `CanDrinkAlcohol` but depending on where I’m at in the world it may or may not be true based on my age. I could tag it with something like `US.CanDrink`, `UK.CanDrink`, etc. but this would be far from ideal for a variety of reasons.
- Tokens are only given at authentication time, so if you need to update permissions, you need to invalidate all the issued tokens every time you make an update. You could also make token lifetimes very short to get more up to date info more often, but that is not ideal either and still has coupling of identity and permissions.

So, what do we do? Well we can still get identity state from our identity server like we usually do. Usually, that should include some kind of role or set of roles that the user has been assigned to. These roles can then be mapped to permissions and used as a reference to a group of permissions.

> It’s important to note that these roles should be identity based and make sense across your whole domain, not just a particular boundary. For instance, something like `InventoryManager` would be better than something like `Approver`.

So we have our user and their identity roles from our auth token, but how do we know what permissions go with our roles? Well, this can be done in a variety of ways to whatever suits your needs best for your api. 

If you have a simple API or an API that rarely has modified permissions, maybe you just want keep a static list of role to permissions mappings in a class in your project or in your appsettings. More commonly, you'll probably want to persist them in a database somewhere. This could be in your boundary/application database or it could be in a separate administration boundary. Maybe you have both and use eventual consistency to keep them in sync. You could even add a caching layer on top of this as well and reference that.

At the end of the day, you can store your permission to role mappings anywhere you want, but you still need a way to easily access them and integrate them into your permissions pipeline. This is where HeimGuard comes in.

## Getting Started

### Prerequisites

Before you get HeimGuard set up, make sure that your authorization policies are set up properly. There are two important items here:

1. Add an authorization attribute (e.g. `[Authorize(Policy = "RecipesFullAccess")]`) to your controller so HeimGuard knows what policy to check against.

2. Reigster your policy

   ```C#
   services.AddAuthorization(options =>
   {
       options.AddPolicy("RecipesFullAccess",
           policy => policy.RequireClaim("permission", "RecipesFullAccess"));
   });
   ```

> 🎉 Note that #2 isn't required if you are using [MapAuthorizationPolicies](#mapauthorizationpolicies).

So for this example, let's say we have a controller like so:

```c#
using HeimGuard;
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
    	return Ok()
    }
}
```



### Setting Up a Permissions Store

To start out, you're going to set up whatever store you want to use for your roles. This could take pretty much whatever structure you want, the only requirement here is that **a permission must be able to be narrowed down to a string that can be used in our authorization attribute.**

Let's look at a couple different examples of how we might store our permissions.

> 🔮 The examples below are mapping permissions to roles, but this isn't a requirement. You could just as easily associate permissions to users or even apply permissions to users as well as roles.

#### Simple Static Class Store

As shown in the quickstart, maybe we have a really simple policy that we just want to store in our project. We could just make a `Permission` class that has some roles associated to it and a store to access it. 

You could also make static strings that get used here and throughout your app to prevent spelling issues. Again, lots of flexibility here.

```c#
public class Permission 
{
    public string Name { get; set; }
    public List<string> Roles { get; set; }
}

public static class SimplePermissionStore
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
```

#### Database Store

We could also have some entities that we are storing in our application database or maybe in a separate administartion boundary. Notice here how our permissions have a Guid as their key, but we can still get a string out of it using `Name` for our authorization attribute.

```c#
using System.ComponentModel.DataAnnotations;

public class Role
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public class Permission
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public class RolePermission
{
    [Key]
    public Guid Id { get; set; }
  
    [JsonIgnore]
    [IgnoreDataMember]
    [ForeignKey("Role")]
    public Guid RoleId { get; set; }
    public Role Role { get; set; }

    [JsonIgnore]
    [IgnoreDataMember]
    [ForeignKey("Permission")]
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; }
}
```



### Implementing a Policy Handler

Now that we have a store set up, we need to determine how we get our final list of permissions for a given user. 

To do this, we are going to create a class that inherits from HeimGaurd's `IUserPolicyHandler` and implements a method called `GetUserPermissions`. This method will do whatever logic you need to perform to get the permissions for you user. It could do any of, but not limited to the following:

1. Check a static file or database for permissions assigned to a user
2. Get a user's roles and then reach 
3. ping a database and

Again, the goal here is to get a list of permissions for my user, particularly as an `IEnumerable<string>`.

#### Simple Static Class IUserPolicyHandler Example

For our [Simple Static Class Store](#simple-static-class-store) example above, we have 3 main steps:

1. Get out user from our `ClaimsPrincipal` using `IHttpContextAccessor`. You could inject a `CurrentUserService` or whatever else here to accomplish this.
2. Get the given roles for that user from their token. Again, these roles could instead be stored in a database or static file as well. You could not even use roles at all and map permissions directly to a user.
3. Get the permissions assigned to that role from our static list.

```c#

public class SimpleUserPolicyHandler : IUserPolicyHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserPolicyHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<IEnumerable<string>> GetUserPermissions()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) throw new ArgumentNullException(nameof(user));
				
      	// this gets the user's role(s) from their ClaimsPrincipal
        var roles = user
          .Claims.Where(c => c.Type == ClaimTypes.Role)
          .Select(r => r.Value)
          .ToArray();
      
      	// this gets their permissions based on their roles. in this example, it's just using a static list
        var permissions = SimplePermissionStore.GetPermissions()
            .Where(p => p.Roles.Any(r => roles.Contains(r)))
            .Select(p => p.Name)
            .ToArray();

        return await Task.FromResult(permissions.Distinct());
    }
}
```

#### Database Static Class IUserPolicyHandler Example

For our [Database Store](#database-store) example above, we have the same 3 steps, just implemented slightly differently to accomodate our database. As a matter of fact, the only difference here is the `var permissions` assignment and injecting my `DbContext`. This is only because I have a similar pattern for both stores, yours could look very different depending on your schema. You could also use a repo, built in method, etc to perform this action and make it more testable.

**However it is implemented, the only thing that matters here is returning a list of strings as your final permissions.**

```c#

public class DatabaseUserPolicyHandler : IUserPolicyHandler
{
    private readonly RecipesDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserPolicyHandler(RecipesDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<IEnumerable<string>> GetUserPermissions()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) throw new ArgumentNullException(nameof(user));
				
        var roles = user.Claims
          .Where(c => c.Type == ClaimTypes.Role)
          .Select(r => r.Value)
          .ToArray();
      
        var permissions = await _dbContext.RolePermissions
            .Where(rp => roles.Contains(rp.Role.Name))
            .Select(rp => rp.Permission.Name)
            .ToArrayAsync();

        return await Task.FromResult(permissions.Distinct());
    }
}
```

### Registering HeimGuard

Once you have your `IUserPolicyHandler` implementation set up, just go to your service builder and register HeimGuard like so:

```c#
public void ConfigureServices(IServiceCollection services)
{
    //...
    services.AddHeimGuard<SimpleUserPolicyHandler>()
      .AutomaticallyCheckPermissions()
      .MapAuthorizationPolicies();
	  // OR...
    services.AddHeimGuard<DatabaseUserPolicyHandler>()
      .AutomaticallyCheckPermissions()
      .MapAuthorizationPolicies();
}
```

And that's it! I've added a couple extension methods on here as they are recommended by default, but they are not required. For more details, check out [HeimGuard Enhancements](#heimguard-enhancements).

## HeimGuard Enhancements

There are currently two extensions on HeimGuard that are both optional but highly recommended, especially `AutomaticallyCheckPermissions`.

### AutomaticallyCheckPermissions

- `AutomaticallyCheckPermissions` will automatically checks user permissions when an authorization attribute is used. Again, this is optional, but without this, we would need to add something like this to our controller:

  ```csharp
  using HeimGuard;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  
  [ApiController]
  [Route("recipes")]
  [Authorize]
  public class RecipesController : ControllerBase
  {
      private readonly IHeimGuardClient _heimGuard;
  
      public RecipesController(IHeimGuardClient heimGuard)
      {
          _heimGuard = heimGuard;
      }
      
      [HttpGet]
      public IActionResult Get()
      {
          return _heimGuard.HasPermissionAsync("RecipesFullAccess") 
            ? Ok()
            : Forbidden();
      }
  }
  ```

### MapAuthorizationPolicies

- `MapAuthorizationPolicies` will automatically map authorization attributes to ASP.NET Core authorization policies that haven't already been mapped. That means you don't have to do something like this for all your policies:

  ```csharp
  services.AddAuthorization(options =>
  {
      options.AddPolicy("RecipesFullAccess",
          policy => policy.RequireClaim("permission", "RecipesFullAccess"));
  });
  ```

> 🧳 Note that if you manually register anything in here it will take presidence over the dynamically added policy.

## Custom Policies

Custom policies can still be written and used as they normally would be in .NET. **Be careful here in that these can get to the grey area of business logic vs authorization.**

* [Microsoft's docs for one requirement](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-6.0#use-a-handler-for-one-requirement)
* [Microsoft's docs for multiple requirements](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-6.0#use-a-handler-for-multiple-requirements)

Generally:

1. Write a custom requirement that extends Microsoft's `IAuthorizationRequirement`
2. Write a handler for that requirement so that any involked policy that has the custom requirement in it will leverage it.
   * You can use HeimGuard DI in these handlers to easily check if the given user has the permission at all and then perform your custom requirement checks.
3. Register that handler in startup
4. Set up your controller

### ❗️ Important Note

It's important to note that custom policies can not be automically resolved with `AutomaticallyCheckPermissions`. That doens't mean that you have to remove `AutomaticallyCheckPermissions` if you use any custom policies, but you'll need to be deliberate with how you set up your controllers. Sepcifically, you can still add the `Authorize` attribute, but you won't pass it a policy like you normally would. Instead, you'll build the custom requirement and involk your custom handler, which could (and likely should) leverage HeimGuard with DI.

```c#
using HeimGuard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("recipes")]
public class RecipesController : ControllerBase
{
    private readonly IAuthorizationService _authService;

    public RecipesController(IAuthorizationService authService)
    {
        _authService = authService;
    }
    
    [HttpGet]
    [Authorize(Policy = "RecipesFullAccess")]
    public IActionResult Get()
    {
      	return Ok();
    }
    
    [HttpGet]
		[Authorize]
    public IActionResult Get()
    {
      	var requirement = new CustomRequirement();
      
      	// this call with involk the custom handler that uses HeimGuard
      	var result = _authService.AuthorizeAsync(User, null, requirement);
      	
      	return result.Succedded 
          ? Ok()
          : Forbidden();
    }
}
```



## Tenants

When working in a multitenant app, you might end up having different roles across different tenants. For example, say I am an `Admin` in Organization 1, but a `User` in Organization 2. The `Admin` role will likely add a lot of permissions that the user role wouldnt have, but how do we check what organization the user is in for that particular request?

If your token is configured to have only your current tenant context (e.g. when I logged in my token only got populated with my roles for `Organization 1`, even though I have access ti other organiations), you can grab that claim from your token and use it in your `IUserPolicyHandler` implementation.

Many times this won't be the case though. If you don't know what your tenant context until later in the process, it will generally be easiest to check permissions without using the `Authorize` attribute at all and strictly checking using this method as a stand alone option. Otherwise, you don't have the context to know what tenant you are working with.

For example, you could add a method to you `IUserPolicyHandler` (or a new service) that can take in a user and get their permissions based on their tenant (i.e. organization).

```c#
public bool GetUserPermissionsByTenant(Guid tenantId)
{
        var userId = _currentUserService.GetUserId();
        if (userId == null) throw new ArgumentNullException(nameof(userId));
				
        var roles = _dbContext.UserTenantRoles
          .Where(utr => utr.TenantId == tenantId && utr.UserId == userId)
          .Select(utr => utr.Role.Name)
          .ToArray();
      
        var permissions = await _dbContext.RolePermissions
            .Where(rp => roles.Contains(rp.Role.Name))
            .Select(rp => rp.Permission.Name)
            .ToArrayAsync();

        return await Task.FromResult(permissions.Distinct());
}
```

Then you could call this inside of your controller or in your CQRS handler.

> 🧢 It's worth noting that at the end of the day, this approach isn't leveraging anything in HeimGuard, so if you need something like this throughout your whole app, then it's probably not even worth bothering with HeimGuard.

## Caching

A potentialy downside to this approach of permission mapping is that it can get chatty. If this is causing performance issues for you, one option might be to use a redis cache in your `IUserPolicyHandler` implementation.

## Multiple Policies Per Attribute

What if you want to assign multiple policies to a single authorization attribute? At that point, your going to want to build a [custom policy assertion](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies) using a function.

```cs
options.AddPolicy("ThisThingOrThatThing", policy =>
    policy.RequireAssertion(context =>
        context.User.HasClaim(c =>
            (c.Type == "ThisThing" ||
             c.Type == "ThatThing"))));
```

## Example
Check out [this example project](https://github.com/pdevito3/HeimGuardExamplePermissions) for one of many options for setting up HeimGuard
