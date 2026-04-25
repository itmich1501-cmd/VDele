using Microsoft.AspNetCore.Identity;
using Osnovanie.Api.Configuration;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddScoped<AuthDbContext>(_ => new AuthDbContext(builder.Configuration.GetConnectionString("Database")!));

builder.Services.Configure<IdentityOptions>(option =>
{
    option.Password.RequireDigit = false;
    option.Password.RequireNonAlphanumeric = false;
    option.User.RequireUniqueEmail = true;
});

builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AuthDbContext>();

var app = builder.Build();

app.Configure();

app.Run();

