using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Orders.Api.Errors;
using Orders.Application.Orders;
using Orders.Application.Orders.ApproveOrder;
using Orders.Application.Orders.CancelOrder;
using Orders.Application.Orders.CreateOrder;
using Orders.Application.Orders.GetOrder;
using Orders.Application.Orders.GetOrders;
using Orders.Application.Orders.RejectOrder;
using Orders.Application.Orders.StartPreparingOrder;
using Orders.Application.Security;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Integrations;
using Orders.Api.Security;
using Orders.Api.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddCheck<OrdersDatabaseHealthCheck>(
        "database",
        tags: ["ready"]);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Orders.Read", policy =>
        policy.RequireClaim("permission", "orders.read"));
    options.AddPolicy("Orders.Create", policy =>
        policy.RequireClaim("permission", "orders.create"));
    options.AddPolicy("Orders.Approve", policy =>
        policy.RequireClaim("permission", "orders.approve"));
    options.AddPolicy("Orders.Reject", policy =>
        policy.RequireClaim("permission", "orders.reject"));
    options.AddPolicy("Orders.Cancel", policy =>
        policy.RequireClaim("permission", "orders.cancel"));
    options.AddPolicy("Orders.StartPreparing", policy =>
        policy.RequireClaim("permission", "orders.start-preparing"));
});

builder.Services.AddDbContext<OrdersDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("OrdersDb"));
});

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<GetOrderHandler>();
builder.Services.AddScoped<GetOrdersHandler>();
builder.Services.AddScoped<ApproveOrderHandler>();
builder.Services.AddScoped<RejectOrderHandler>();
builder.Services.AddScoped<CancelOrderHandler>();
builder.Services.AddScoped<StartPreparingOrderHandler>();
builder.Services.AddHttpClient<IProductCatalog, CatalogProductClient>(client =>
{
    var catalogBaseUrl = builder.Configuration["Services:Catalog:BaseUrl"]
        ?? "http://localhost:5276/";
    client.BaseAddress = new Uri(catalogBaseUrl);
});

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Trace-Id"] = traceId;
        return Task.CompletedTask;
    });
    await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program;
