namespace ViaPermanente.Dominio.Pessoas.Models;

/// <summary>Os cinco cargos do sistema. Viram as permissões (roles) do login.</summary>
public enum Cargo
{
    /// <summary>Supervisor: pedido, aprovação da troca, auditoria, indicadores e o cadastro dos usuários.</summary>
    Supervisor,
    /// <summary>Técnico: passagem, alertas e inspeção; pede a troca e acompanha os quatro passos.</summary>
    Tecnico,
    /// <summary>Especialista: verificação de qualidade na chegada, classificação e relatório ambiental.</summary>
    Especialista,
    /// <summary>Operador de pátio: pedido, chegada, cadastro com a tag, armazém e separação da peça da troca.</summary>
    OperadorPatio,
    /// <summary>Operador da via: passagem noturna, alertas (registra o que viu) e a troca no trecho.</summary>
    OperadorVia
}