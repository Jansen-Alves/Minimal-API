using MinimalApi.DTOs;
using MinimalApi.Dominio.Entidades;

namespace MinimalApi.Dominio.Interfaces;
public interface IAdministradorServico{
    Administrador Login(LoginDTO loginDT);
    Administrador Adicionar(Administrador administrador);
    List<Administrador> Todos(int? pagina);
    Administrador? BuscaPorId(int id);

}