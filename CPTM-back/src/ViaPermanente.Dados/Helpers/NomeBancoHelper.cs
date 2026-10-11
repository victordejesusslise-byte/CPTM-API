using System.Text;

namespace ViaPermanente.Dados.Helpers;

/// <summary>Nomes e tamanhos das colunas no SQL Server, iguais aos do cérebro do projeto.</summary>
public static class NomeBancoHelper
{
    /// <summary>"ClassificacaoMadeira" vira "CLASSIFICACAO_MADEIRA".</summary>
    public static string Tabela(string nomeClasse) => Snake(nomeClasse).ToUpperInvariant();

    /// <summary>"VolumeM3Estoque" vira "volume_m3_estoque".</summary>
    public static string Coluna(string nomePropriedade) => Snake(nomePropriedade);

    /// <summary>Precisão e escala do decimal pela unidade que o nome da coluna indica.</summary>
    public static (int Precisao, int Escala) PrecisaoDecimal(string coluna)
    {
        if (coluna.EndsWith("_m")) return (5, 3);              // metros: comprimento, largura, altura
        if (coluna.Contains("km")) return (9, 3);              // quilômetro da linha: 213,120
        if (coluna.Contains("m3")) return (12, 5);             // m³: 0,11424
        if (coluna.EndsWith("_mm")) return (8, 2);             // desvio em milímetros
        if (coluna.Contains("dbm") || coluna.EndsWith("_db")) return (5, 1);
        if (coluna.Contains("confianca") || coluna.Contains("acerto")) return (7, 4);
        if (coluna.StartsWith("umidade") || coluna == "temperatura") return (5, 2);
        if (coluna == "valor") return (14, 2);
        return (12, 4);
    }

    /// <summary>Tamanho do texto pela função da coluna. Nulo: sem limite (nvarchar(max)).</summary>
    public static int? TamanhoTexto(string coluna)
    {
        if (coluna.StartsWith("retrato_")) return null;        // JSON do antes e depois
        if (coluna.Contains("arquivo")) return 260;            // caminho de foto ou planilha
        if (coluna is "observacao" or "descricao" or "mensagem" or "resposta" or "texto_lido" or "cancelamento"
            || coluna.StartsWith("motivo")) return 1000;
        return 200;
    }

    private static string Snake(string nome)
    {
        var texto = new StringBuilder(nome.Length + 8);
        for (var i = 0; i < nome.Length; i++)
        {
            var c = nome[i];
            if (i > 0 && char.IsUpper(c))
            {
                var anterior = nome[i - 1];
                var proximaMinuscula = i + 1 < nome.Length && char.IsLower(nome[i + 1]);
                if (char.IsLower(anterior) || char.IsDigit(anterior) || (char.IsUpper(anterior) && proximaMinuscula))
                    texto.Append('_');
            }
            texto.Append(char.ToLowerInvariant(c));
        }
        return texto.ToString();
    }
}