using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ViaPermanente.Api.Via;
using ViaPermanente.Api.Comum;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Api.Models;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Ambiental;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Armazens;
using ViaPermanente.Dados.Campo;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Chegadas;
using ViaPermanente.Dados.Indicadores;
using ViaPermanente.Dados.Integracoes;
using ViaPermanente.Dados.Laudos;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pedidos;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dados.Via;
using ViaPermanente.Dominio.Ambiental;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Armazens;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Indicadores;
using ViaPermanente.Dominio.Integracoes;
using ViaPermanente.Dominio.Laudos;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Via;

// Segredos (conexão do banco, chave do JWT, supervisor inicial) vêm do .env, que fica fora do git.
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// No Docker, o log sai no console em JSON e o próprio Docker guarda (docker compose logs).
if (builder.Configuration.GetValue<bool>("Logs:Json"))
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(o => o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffzzz ");
}

var conexao = builder.Configuration.GetConnectionString("ViaPermanente");
if (string.IsNullOrWhiteSpace(conexao))
    throw new InvalidOperationException("Falta ConnectionStrings__ViaPermanente no .env (veja .env.exemplo).");

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOpcoes>() ?? new JwtOpcoes();
if (jwt.Chave.Length < 32)
    throw new InvalidOperationException("Jwt__Chave no .env precisa ter pelo menos 32 caracteres.");
builder.Services.AddSingleton(jwt);

builder.Services.AddDbContext<ViaPermanenteContext>(o => o.UseSqlServer(conexao));

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<IParametroRepository, ParametroRepository>();
builder.Services.AddScoped<IApresentacaoRepository, ApresentacaoRepository>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IOfertaRepository, OfertaRepository>();
builder.Services.AddScoped<INotaFiscalRepository, NotaFiscalRepository>();
builder.Services.AddScoped<IChegadaRepository, ChegadaRepository>();
builder.Services.AddScoped<IPecaRepository, PecaRepository>();
builder.Services.AddScoped<IClassificacaoRepository, ClassificacaoRepository>();
builder.Services.AddScoped<IFotoPecaRepository, FotoPecaRepository>();
builder.Services.AddScoped<ISinaisPecaRepository, SinaisPecaRepository>();
builder.Services.AddScoped<IPassagemConsultaRepository, PassagemConsultaRepository>();
builder.Services.AddScoped<ITrechoMapaRepository, TrechoMapaRepository>();
builder.Services.AddScoped<IVarreduraRepository, VarreduraRepository>();
builder.Services.AddScoped<IAlertaRepository, AlertaRepository>();
builder.Services.AddScoped<IInspecaoRepository, InspecaoRepository>();
builder.Services.AddScoped<ITrocaRepository, TrocaRepository>();
builder.Services.AddScoped<IRelatorioAmbientalRepository, RelatorioAmbientalRepository>();
builder.Services.AddScoped<IParticaoRepository, ParticaoRepository>();
builder.Services.AddScoped<IArmazemRepository, ArmazemRepository>();
builder.Services.AddScoped<IHistoricoRepository, HistoricoRepository>();
builder.Services.AddScoped<ICadastroRepository, CadastroRepository>();
builder.Services.AddScoped<IPlanilhaNotaRepository, PlanilhaNotaRepository>();
builder.Services.AddScoped<IIntegracaoLogRepository, IntegracaoLogRepository>();
builder.Services.AddScoped<IPlanilhaExportacaoRepository, PlanilhaExportacaoRepository>();
builder.Services.AddScoped<IDocumentoPdfRepository, DocumentoPdfRepository>();
builder.Services.AddScoped<IIndicadorRepository, IndicadorRepository>();
builder.Services.AddScoped<ILeituraOcrRepository, LeituraOcrRepository>();
// O tessdata e a pasta das fotos ficam na raiz do CPTM-back, a não ser que o .env diga outra coisa.
var raiz = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
builder.Services.AddSingleton<IOcrRepository>(new TesseractOcrRepository(builder.Configuration["Ocr:Tessdata"] ?? Path.Combine(raiz, "tessdata")));
builder.Services.AddSingleton<IFotoRepository>(new FotoRepository(builder.Configuration["Fotos:Pasta"] ?? Path.Combine(raiz, "fotos")));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<ParametroService>();
builder.Services.AddScoped<ApresentacaoService>();
builder.Services.AddScoped<DemonstracaoService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<OfertaService>();
builder.Services.AddScoped<NotaFiscalService>();
builder.Services.AddScoped<ChegadaService>();
builder.Services.AddScoped<PecaService>();
builder.Services.AddScoped<ClassificacaoService>();
builder.Services.AddScoped<FotoPecaService>();
builder.Services.AddScoped<PassagemConsultaService>();
builder.Services.AddScoped<TrechoMapaService>();
builder.Services.AddScoped<LeituraService>();
builder.Services.AddScoped<QualidadeService>();
builder.Services.AddScoped<ArmazemService>();
builder.Services.AddScoped<VarreduraService>();
builder.Services.AddScoped<AlertaService>();
builder.Services.AddScoped<InspecaoService>();
builder.Services.AddScoped<TrocaService>();
builder.Services.AddScoped<RelatorioAmbientalService>();
builder.Services.AddScoped<ManutencaoLeituraViaService>();
builder.Services.AddScoped<ConferenciaService>();
builder.Services.AddScoped<VinculoTagService>();
builder.Services.AddScoped<HistoricoService>();
builder.Services.AddScoped<CadastroService>();
builder.Services.AddScoped<ImportacaoNotaService>();
builder.Services.AddScoped<ExportacaoService>();
builder.Services.AddScoped<LaudoService>();
builder.Services.AddScoped<IndicadorService>();
builder.Services.AddScoped<OcrPlacaService>();
builder.Services.AddHostedService<ManutencaoDiariaHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = TokenHelper.Parametros(jwt);
    })
    .AddScheme<AuthenticationSchemeOptions, ChaveCarroHandler>(ChaveCarroHandler.Esquema, null);
// Tudo exige login, a não ser o que tem [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Origens do appsettings; em desenvolvimento (Cors:RedeLocal) também o front aberto pelo celular no mesmo Wi-Fi.
var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
var redeLocal = builder.Configuration.GetValue<bool>("Cors:RedeLocal");
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .SetIsOriginAllowed(origem => origens.Contains(origem) || (redeLocal && RedeLocalHelper.EhFrontDaRede(origem)))
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErroHandler>();
// Enum como texto no JSON: o Angular manda e recebe "Tecnico", não 1.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Via Permanente API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Cole o token de POST /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Primeira subida: aplica as migrações (no Docker) e cria o supervisor do .env se o banco ainda não tem usuário.
using (var escopo = app.Services.CreateScope())
{
    if (builder.Configuration.GetValue<bool>("Banco:MigrarAoIniciar"))
        await escopo.ServiceProvider.GetRequiredService<ViaPermanenteContext>().Database.MigrateAsync();
    var inicial = builder.Configuration.GetSection("UsuarioInicial");
    await escopo.ServiceProvider.GetRequiredService<UsuarioService>()
        .GarantirSupervisorInicial(inicial["Nome"], inicial["Email"], inicial["Senha"]);
}

app.Run();
