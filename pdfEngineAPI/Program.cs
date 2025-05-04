using Microsoft.EntityFrameworkCore;
using ModelLibrary.Model;
using pdfEngineAPI.Data;
using pdfEngineAPI.Middleware;
using pdfEngineAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<PdfDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DocumentDatabase")));

//
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>();
builder.Services.AddHttpClient<IAnswerGenerationService, AnswerGenerationService>();
builder.Services.AddScoped<IQueryClassifierService, QueryClassifierService>();

//chatHistory
builder.Services.AddSingleton<IChatHistoryService, InMemoryChatHistoryService>();

var app = builder.Build();

//
app.UseMiddleware<ExceptionMiddleware>();

//HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
