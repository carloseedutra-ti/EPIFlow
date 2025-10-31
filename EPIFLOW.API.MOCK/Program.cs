var builder = WebApplication.CreateBuilder(args);

// Configura os serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configura o pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();
// Define a porta 5000 explicitamente
app.Urls.Clear();
app.Urls.Add("http://localhost:5000");

app.Run();
