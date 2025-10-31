using EPIFlow.BiometriaSvc;

var builder = Host.CreateApplicationBuilder(args);

// Configura o serviço Windows
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "EPIFlow Biometria Service";
});

// Registra o Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
