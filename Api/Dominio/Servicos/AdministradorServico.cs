using MinimalApi.Dominio.Entidades;
using MinimalApi.DTOs;
using MinimalApi.Dominio.Interfaces;
using MinimalAPI.Infraestrutura.Db;

namespace MinimalApi.Dominio.Servicos;

public class AdministradorServico : IAdministradorServico
{
    private readonly DbContexto _contexto;
    public AdministradorServico(DbContexto db)
    {
        _contexto = db;

    }

    public Administrador Adicionar(Administrador administrador)
    {
        
        _contexto.Administradores.Add(administrador);
        _contexto.SaveChanges();
        return administrador;
    }

    public Administrador Login(LoginDTO loginDT)
    {
        var adm = _contexto.Administradores.Where(a => a.Email == loginDT.Email && a.Senha == loginDT.Senha).FirstOrDefault();
        return adm;
    }

    public List<Administrador> Todos(int? pagina)
    {
      var query = _contexto.Administradores.AsQueryable();
       
        int itensPaginasqtd = 10;
        if(pagina !=  null){
            query = query.Skip(((int)pagina - 1) * itensPaginasqtd).Take(itensPaginasqtd);
        }
       return query.ToList();
    }

    public Administrador? BuscaPorId(int id){
         return _contexto.Administradores.Where(v => v.id == id).FirstOrDefault();
   
    }
}