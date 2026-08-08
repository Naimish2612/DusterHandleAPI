using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.CacheMemory;
using DUSTER.EComm.Data.Helpers.Cloudinary;
using DUSTER.EComm.Data.Helpers.Dates;
using DUSTER.EComm.Data.Helpers.DBConnection;
using DUSTER.EComm.Data.Helpers.Filters;
using DUSTER.EComm.Data.Helpers.Logger;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Data.Services;
using DUSTER.EComm.Services.CommonServices;
using DUSTER.EComm.Services.Modules.AlertEngine;
using DUSTER.EComm.Services.Modules.Auth;
using DUSTER.EComm.Services.Modules.Catelog;
using DUSTER.EComm.Services.Modules.Customers;
using DUSTER.EComm.Services.Modules.DeliveryPolicy;
using DUSTER.EComm.Services.Modules.ImportEngine;
using DUSTER.EComm.Services.Modules.ImportEngine.Models;
using DUSTER.EComm.Services.Modules.ImportEngine.Strategy;
using DUSTER.EComm.Services.Modules.Masters;
using DUSTER.EComm.Services.Modules.Notification;
using DUSTER.EComm.Services.Modules.Notifications;
using DUSTER.EComm.Services.Modules.Notifications.Models;
using DUSTER.EComm.Services.Modules.Orders;
using DUSTER.EComm.Services.Modules.PrimaryDocuments;
using DUSTER.EComm.Services.Modules.RightsMasters;
using DUSTER.EComm.Services.Modules.Support;
using DUSTER.EComm.Services.Modules.SystemConfig;
using DUSTER.EComm.Services.Modules.Tax;
using Microsoft.Extensions.FileProviders;
using Sats.PostgreSqlDistributedCache;

var builder = WebApplication.CreateBuilder(args);

#region Distributed Cache

//system memory cache implementation
//builder.Services.AddSingleton<IDistributedCacheWrapper, DistributedCacheWrapper>();

#endregion

#region Register PostgresDistributedCache

builder.Services.AddPostgresDistributedCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("dev");
    options.SchemaName = "public";
    options.TableName = "SiteCache";
});

builder.Services.AddSingleton<IPostgresDistributedCache, PostgresDistributedCache>();

#endregion

#region Gmail Config

builder.Services.Configure<EmailGmail>(builder.Configuration.GetSection("GmailMailSettings"));

#endregion

#region CloudinarySettings

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

#endregion

#region Database Connection Settings & Register generic infrastructure

builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IEIPLRepository<>), typeof(EIPLRepository<>));

#endregion

#region Register eRP Services

builder.Services.AddScoped<IErrorLogger, ErrorLogger>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IWelcomeService, WelcomeService>();
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<ILoginServices, LoginServices>();
builder.Services.AddScoped<IRightsMasterServices, RightsMasterServices>();
builder.Services.AddScoped<IEmailNotificationServices, EmailNotificationServices>();
builder.Services.AddScoped<IMasterServices, MasterServices>();
builder.Services.AddScoped<ICatelogServices, CatelogServices>();
builder.Services.AddScoped<ICustomerServices, CustomerServices>();
builder.Services.AddScoped<IOrderServices, OrderServices>();
builder.Services.AddScoped<ICouponServices, CouponServices>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddScoped<ITaxServices, TaxServices>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IPrimaryDocumentService, PrimaryDocumentService>();
builder.Services.AddScoped<IAlertEngineService, AlertEngineService>();
builder.Services.AddScoped<ISmtpConfigurationService, SmtpConfigurationService>();
builder.Services.AddScoped<ISmtpDeliveryService, SmtpDeliveryService>();
builder.Services.AddScoped<IAlertTemplateService, AlertTemplateService>();
builder.Services.AddScoped<IImportServices, ImportServices>();
// Register the Alert Engine Background Worker
builder.Services.AddHostedService<AEBackgroundWorker>();

#endregion

#region Import Strategy handlers & Background Worker

// Register the Import Background Worker
builder.Services.AddHostedService<ImportBackgroundWorker>();

//Register the specific import handlers
builder.Services.AddScoped<IImportHandler, ProductCategoryImportHandler>();
builder.Services.AddScoped<IImportHandler, ProductSubCategoryImportHandler>();

#endregion

// Add services to the container.
builder.Services.AddControllers(x =>
{
    x.Filters.Add<JwtAuthorizationFilter>();
    x.Filters.Add<ActionFilter>();
    //x.Filters.Add<ResponseWrapperFilter>();
    x.Filters.Add<GlobalExceptionFilter>();

}).AddJsonOptions(x =>
{
    x.JsonSerializerOptions.Converters.Add(new CustomDateTimeConverter());
});

builder.Services.AddScoped<JwtAuthorizationFilter>();
builder.Services.AddScoped<ActionFilter>();
//builder.Services.AddScoped<ResponseWrapperFilter>();
builder.Services.AddScoped<GlobalExceptionFilter>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

//// 1. Ensure the directory exists so the app doesn't crash on startup
var filesPath = Path.Combine(builder.Environment.ContentRootPath, "Files");
if (!Directory.Exists(filesPath))
{
    Directory.CreateDirectory(filesPath);
}

// 2. TELL .NET TO SERVE FILES FROM THIS FOLDER
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(filesPath),
    RequestPath = "/Files" // This matches your URL: http://...:8090/Files/...
});


// Configure the HTTP request pipeline.
app.UseRouting();

///  Enable CORS for all origins, methods, headers, and credentials
///  Note: In production, you should restrict the allowed origins, methods, and headers for security reasons.
///  https://jasonwatmore.com/post/2020/05/20/aspnet-core-api-allow-cors-requests-from-any-origin-and-with-credentials
app.UseCors(builder => builder.SetIsOriginAllowed(o => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes
app.MapControllers();

// Run the application
app.Run();
