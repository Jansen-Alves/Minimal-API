namespace MinimalApi.Dominio.ModelViews;

public struct ErrosDeValidacao{
    public List<string> mensagens { get; set; }

    public ErrosDeValidacao(){
        mensagens = new List<string>();
    }
}