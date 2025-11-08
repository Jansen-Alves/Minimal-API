using MinimalApi.DTOs;
using MinimalApi.Dominio.Entidades;

namespace MinimalApi.Dominio.Interfaces;
public interface IVeiculoServico{
    List<Veiculo> Veiculos(int? pagina = 1, string nome = null, string marca =null);
    Veiculo? BuscaPorId(int id);
    void Adicionar(Veiculo veiculo);
    void Atualizar(Veiculo veiculo);
    void Deletar (Veiculo veiculo);
} 