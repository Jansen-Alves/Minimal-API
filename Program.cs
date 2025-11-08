using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enums;
using MinimalApi.Dominio.Interfaces; 
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using MinimalApi.DTOs;
using MinimalAPI.Infraestrutura.Db;

#region Builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if(string.IsNullOrEmpty(key)){
    key = "123456";
}
builder.Services.AddAuthentication(option => {
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;}).AddJwtBearer(option =>
    {
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = true,
            //ValidateAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT no formato **Bearer {token}** para acessar esta API"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme{
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"

                }
        },
            new string[]{}
        }
    });
});

builder.Services.AddDbContext<DbContexto>(options => {
    options.UseMySql(
        builder.Configuration.GetConnectionString("MySql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySql"))
    );
});

var app = builder.Build();
#endregion


#region home
app.MapGet("/", () => Results.Json(new Home())).WithTags("Home").AllowAnonymous();
#endregion

#region Administradores 

string GerarTokenJWT(Administrador adm){ // Aqui você coloca as credenciais de login
    if(!string.IsNullOrEmpty(key)){
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    
        var claims = new List<Claim>();
        {
            new Claim("Email", adm.Email);
            new Claim(ClaimTypes.Role, adm.Perfil);
            new Claim("Perfil", adm.Perfil);
        };
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);

    }
    else
    {
        return string.Empty;
    }
}
app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
    var administrador = administradorServico.Login(loginDTO);
    if (administrador != null)
    {
        string token = GerarTokenJWT(administrador);
        return Results.Ok(new AdministradorLogado{
            Email = administrador.Email,
            Perfil = administrador.Perfil,
            Token = token
        });
    }
    else
    {
        return Results.Unauthorized();
    }
}).AllowAnonymous().WithTags("Administrador");

app.MapGet("/administrador", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
{
    var adms = new List<AdministradorMV>();
    var administradores = administradorServico.Todos(pagina);

    foreach(var adm in administradores){
        adms.Add(new AdministradorMV
        {
            Id = adm.id,
            Email = adm.Email,
            Perfil = adm.Perfil
        });
    }
    return Results.Ok(adms);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"}).WithTags("Administrador");

app.MapGet("/administrador/{id}", ([FromRoute]int id, IAdministradorServico administradorServico) => {
    var admin = administradorServico.BuscaPorId(id);
    if(admin == null) return Results.NotFound();
    
    return Results.Ok(new AdministradorMV
        {
            Id = admin.id,
            Email = admin.Email,
            Perfil = admin.Perfil
        });
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"}).WithTags("Administrador");

app.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) => {
  var validacao = new ErrosDeValidacao {
      mensagens = new List<string>()
  };

  if(string.IsNullOrEmpty(administradorDTO.Email)){
      validacao.mensagens.Add("O email é obrigatório.");
  }
  
  if(string.IsNullOrEmpty(administradorDTO.Senha)){
      validacao.mensagens.Add("A senha não pode ser vazia.");
  }
  
  if(administradorDTO.Perfil == null){
      validacao.mensagens.Add("O perfil é obrigatório.");
  }
  
    if(validacao.mensagens.Count > 0)
    {
        return Results.BadRequest(validacao);
    }
    
    var adm = new Administrador{
        Email = administradorDTO.Email,
        Senha = administradorDTO.Senha,
        Perfil =  administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
    };
    administradorServico.Adicionar(adm);

    return Results.Created($"/administrador/{adm.id}", new AdministradorMV
        {
            Id = adm.id,
            Email = adm.Email,
            Perfil = adm.Perfil
        });
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"}).WithTags("Administrador");
#endregion

#region Veiculos
 ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO){
    var Mensagem = new ErrosDeValidacao();
    if(string.IsNullOrEmpty(veiculoDTO.Nome)){
        Mensagem.mensagens.Add("O nome do veiculo é obrigatório.");
    }
    if(string.IsNullOrEmpty(veiculoDTO.Marca)){
        Mensagem.mensagens.Add("A marca do veiculo é obrigatória.");
    }
    if(veiculoDTO.Ano <= 1960){
        Mensagem.mensagens.Add("O ano do veiculo é inaceitavel.");
    }

    return Mensagem;
}

app.MapPost("/Veiculos", ([FromBody] VeiculoDTO veiculosDTO, IVeiculoServico veiculoServico) => {

   var validacao = validaDTO(veiculosDTO);
    if(validacao.mensagens.Count > 0)
    {
        return Results.BadRequest(validacao);
    }
    
    var veiculo = new Veiculo{
        Nome = veiculosDTO.Nome,
        Marca = veiculosDTO.Marca,
        Ano = veiculosDTO.Ano
    };
    veiculoServico.Adicionar(veiculo);

    return Results.Created($"/Veiculos/{veiculo.id}", veiculo);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm,Editor"})
.WithTags("Veiculos");

app.MapGet("/Veiculos", ([FromQuery]int? pagina, IVeiculoServico veiculoServico) => {
    var veiculos = veiculoServico.Veiculos(pagina);
    return Results.Ok(veiculos);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Veiculos");

app.MapGet("/Veiculos/{id}", ([FromRoute]int id, IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscaPorId(id);
    if(veiculo == null) return Results.NotFound();
    
    return Results.Ok(veiculo);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"}).WithTags("Veiculos");

app.MapPut("/Veiculos/{id}", ([FromRoute]int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => {
    var validacao = validaDTO(veiculoDTO);
    if (validacao.mensagens.Count > 0)
    {
        return Results.BadRequest(validacao);
    }
    
    
    var veiculo = veiculoServico.BuscaPorId(id);
    if(veiculo == null) return Results.NotFound();


    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);
    return Results.Ok(veiculo);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"}).WithTags("Veiculos");

app.MapDelete("/Veiculos/{id}", ([FromRoute]int id, IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscaPorId(id);
    if(veiculo == null) return Results.NotFound();

    veiculoServico.Deletar(veiculo);
    return Results.NoContent();
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"}).WithTags("Veiculos");
#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion