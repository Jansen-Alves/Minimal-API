using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Entidades;
namespace MinimalAPI.Infraestrutura.Db;

public class DbContexto :DbContext{

    private readonly IConfiguration _configurationAppSettings;
    

    public DbContexto(IConfiguration configurationAppSettings){
        _configurationAppSettings = configurationAppSettings;
    }
    public DbSet<Administrador> Administradores { get; set; } = default!;
    public DbSet<Veiculo> Veiculos { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) //Prepara os registros para migration
    {
        modelBuilder.Entity<Administrador>().HasData(new Administrador
        {
            id = 1,
            Email = "administrador@teste.com",
            Senha = "123456",
            Perfil = "Adm"
        }
        );
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var stringConexao = _configurationAppSettings.GetConnectionString("MySql")?.ToString();

            if (!string.IsNullOrEmpty(stringConexao))
            {
                optionsBuilder.UseMySql(stringConexao,
                ServerVersion.AutoDetect(stringConexao));
                return;
            }
        }
    }
}