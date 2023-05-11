using Microsoft.AspNetCore.Http.HttpResults;

namespace VkApi;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapUserApi(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/user");
        group.MapGet("/", async (IUserService userService) => {
            var users = await userService.GetAll();
            return Results.Ok(users);
        });

        group.MapGet("/{login}", async (IUserService userService, string login) =>
        {
            var user = await userService.Get(login);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        group.MapPost("/", async (IUserService userService, User user) =>
        {
            var (newUser, errorMsg) = await userService.Create(user);
            return newUser is not null ? Results.Created($"/${newUser.Login}", newUser) : Results.BadRequest(errorMsg);
        });

        group.MapDelete("/{login}", async (IUserService userService, string login) =>
        {
            var deleted = await userService.Delete(login);
            return deleted ? Results.Ok() : Results.BadRequest();
        });
        
        return builder;
    }
}