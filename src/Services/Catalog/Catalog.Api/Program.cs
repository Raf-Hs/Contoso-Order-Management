using Catalog.Application.Products;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.DecreaseStock;
using Catalog.Application.Products.GetProduct;
using Catalog.Application.Products.GetProducts;
using Catalog.Application.Products.IncreaseStock;
using Catalog.Application.Products.UpdateProduct;
using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Catalog.Api.Errors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CatalogExceptionHandler>();
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
    options.AddPolicy("Catalog.Create", policy =>
        policy.RequireClaim("permission", "catalog.create"));
    options.AddPolicy("Catalog.Update", policy =>
        policy.RequireClaim("permission", "catalog.update"));
    options.AddPolicy("Catalog.Stock", policy =>
        policy.RequireClaim("permission", "catalog.stock"));
});

builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CatalogDb"));
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<GetProductHandler>();
builder.Services.AddScoped<GetProductsHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<IncreaseStockHandler>();
builder.Services.AddScoped<DecreaseStockHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program;
