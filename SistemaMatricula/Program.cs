using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Habilitar CORS para permitir peticiones desde el frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

// Configuración del pipeline de solicitudes HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Lógica para abrir la segunda pestaña (aspirante.html) automáticamente
    // La primera se abre mediante launchSettings.json
    try
    {
        var urlAspirante = "http://localhost:5262/aspirante.html";
        Process.Start(new ProcessStartInfo
        {
            FileName = urlAspirante,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"No se pudo abrir la pestaña del aspirante: {ex.Message}");
    }
}

app.UseCors("AllowAll"); // Aplicar CORS
app.UseAuthorization();

// Importante: El orden permite que se sirvan los archivos de wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();