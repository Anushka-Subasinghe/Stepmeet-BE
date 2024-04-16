using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Google.Cloud.Firestore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore.V1;
using Grpc.Core;
using System.Net;
using Grpc.Auth;
using Google.Cloud.Storage.V1; // Import the namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Stepmeet BE", Version = "v1" });
});

string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "firebase-service-account.json");
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", jsonPath);

// Initialize Firestore
builder.Services.AddSingleton<FirestoreDb>(provider =>
{
    FirestoreClient client = new FirestoreClientBuilder
    {
        CredentialsPath = jsonPath
    }.Build();
    FirestoreDb firestoreDb = FirestoreDb.Create("stepmeet-8ed5d", client);
    return firestoreDb;
});

// Initialize Firebase Admin SDK
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(jsonPath)
});

// Add StorageClient as a singleton service
builder.Services.AddSingleton(provider =>
{
    var credential = GoogleCredential.FromFile(jsonPath);
    var storageClient = StorageClient.Create(credential);
    return storageClient;
});

var app = builder.Build();

app.UseCors("AllowOrigin");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stepmeet BE"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();
