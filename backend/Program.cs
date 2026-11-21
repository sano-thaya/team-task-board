
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using TeamTaskBoard.Api.Services;
using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json.Serialization;
using Google.Cloud.Firestore;


var builder = WebApplication.CreateBuilder(args);

// Configure MySQL database connection.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string is missing."
    );

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 13))));

// Register MVC controllers and OpenAPI.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Firebase";
    options.DefaultChallengeScheme = "Firebase";
})
.AddScheme<FirebaseAuthenticationOptions, FirebaseAuthenticationHandler>("Firebase", _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager"));
});

// Initialize Firebase Admin using the service account credentials.
var firebaseProjectId = builder.Configuration["Firebase:ProjectId"]
    ?? builder.Configuration["FIREBASE_PROJECT_ID"];

var firebaseApp = FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.GetApplicationDefault(),
    ProjectId = firebaseProjectId
});

// Make Firebase services available through dependency injection.
builder.Services.AddSingleton(firebaseApp);
builder.Services.AddSingleton(FirebaseAuth.GetAuth(firebaseApp));
builder.Services.AddSingleton(FirestoreDb.Create(firebaseProjectId));

// Register our token verification service.
builder.Services.AddScoped<FirebaseTokenService>();
builder.Services.AddScoped<IUserSyncService, UserSyncService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
