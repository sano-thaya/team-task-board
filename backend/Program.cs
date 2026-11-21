
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using TeamTaskBoard.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers and OpenAPI.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Initialize Firebase Admin using the service account credentials.
var firebaseApp = FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.GetApplicationDefault()
});

// Make Firebase services available through dependency injection.
builder.Services.AddSingleton(firebaseApp);
builder.Services.AddSingleton(FirebaseAuth.GetAuth(firebaseApp));

// Register our token verification service.
builder.Services.AddScoped<FirebaseTokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
