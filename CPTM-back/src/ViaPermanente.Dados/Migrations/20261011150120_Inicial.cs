using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ARMAZEM",
                columns: table => new
                {
                    id_armazem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    linha = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    km = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    endereco = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ARMAZEM", x => x.id_armazem);
                });

            migrationBuilder.CreateTable(
                name: "EMPRESA",
                columns: table => new
                {
                    id_empresa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    cnpj = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    endereco = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    telefone = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ativo = table.Column<bool>(type: "bit", nullable: false),
                    papel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPRESA", x => x.id_empresa);
                });

            migrationBuilder.CreateTable(
                name: "ESPECIE",
                columns: table => new
                {
                    id_especie = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome_cientifico = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    familia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    nome_popular = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    classe = table.Column<byte>(type: "tinyint", nullable: false),
                    ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESPECIE", x => x.id_especie);
                    table.CheckConstraint("CK_ESPECIE_classe", "[classe] IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "ESTACAO",
                columns: table => new
                {
                    id_estacao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    linha = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    km = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    ordem = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESTACAO", x => x.id_estacao);
                });

            migrationBuilder.CreateTable(
                name: "INTEGRACAO_LOG",
                columns: table => new
                {
                    id_log = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sistema = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    direcao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    mensagem = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INTEGRACAO_LOG", x => x.id_log);
                });

            migrationBuilder.CreateTable(
                name: "PASSAGEM",
                columns: table => new
                {
                    id_passagem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    veiculo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    data_inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    data_fim = table.Column<DateTime>(type: "datetime2", nullable: true),
                    km_inicio = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    km_fim = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PASSAGEM", x => x.id_passagem);
                });

            migrationBuilder.CreateTable(
                name: "PESSOA",
                columns: table => new
                {
                    id_pessoa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    data_nascimento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PESSOA", x => x.id_pessoa);
                });

            migrationBuilder.CreateTable(
                name: "SETOR",
                columns: table => new
                {
                    id_setor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SETOR", x => x.id_setor);
                });

            migrationBuilder.CreateTable(
                name: "TAMANHO_DORMENTE",
                columns: table => new
                {
                    id_tamanho = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    comprimento_m = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    largura_m = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    altura_m = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    medida_provisoria = table.Column<bool>(type: "bit", nullable: false),
                    ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAMANHO_DORMENTE", x => x.id_tamanho);
                });

            migrationBuilder.CreateTable(
                name: "TIPO_LEITOR",
                columns: table => new
                {
                    id_tipo_leitor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIPO_LEITOR", x => x.id_tipo_leitor);
                });

            migrationBuilder.CreateTable(
                name: "CONTRATO",
                columns: table => new
                {
                    id_contrato = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_empresa = table.Column<int>(type: "int", nullable: false),
                    numero_ata = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    numero_contrato = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    vigencia_inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    vigencia_fim = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONTRATO", x => x.id_contrato);
                    table.ForeignKey(
                        name: "FK_CONTRATO_EMPRESA_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "EMPRESA",
                        principalColumn: "id_empresa",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TRECHO",
                columns: table => new
                {
                    id_trecho = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_estacao_inicio = table.Column<int>(type: "int", nullable: false),
                    id_estacao_fim = table.Column<int>(type: "int", nullable: false),
                    classe_exigida = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRECHO", x => x.id_trecho);
                    table.CheckConstraint("CK_TRECHO_classe_exigida", "[classe_exigida] IN (1, 2)");
                    table.CheckConstraint("CK_TRECHO_estacoes_diferentes", "[id_estacao_inicio] <> [id_estacao_fim]");
                    table.ForeignKey(
                        name: "FK_TRECHO_ESTACAO_id_estacao_fim",
                        column: x => x.id_estacao_fim,
                        principalTable: "ESTACAO",
                        principalColumn: "id_estacao",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TRECHO_ESTACAO_id_estacao_inicio",
                        column: x => x.id_estacao_inicio,
                        principalTable: "ESTACAO",
                        principalColumn: "id_estacao",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_pessoa = table.Column<int>(type: "int", nullable: false),
                    tipo_cargo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    data_cadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    senha_hash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.id_usuario);
                    table.ForeignKey(
                        name: "FK_USUARIO_PESSOA_id_pessoa",
                        column: x => x.id_pessoa,
                        principalTable: "PESSOA",
                        principalColumn: "id_pessoa",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CODIGO_MATERIAL",
                columns: table => new
                {
                    id_codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_tamanho = table.Column<int>(type: "int", nullable: false),
                    classe = table.Column<byte>(type: "tinyint", nullable: false),
                    codigo_erp = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CODIGO_MATERIAL", x => x.id_codigo);
                    table.CheckConstraint("CK_CODIGO_MATERIAL_classe", "[classe] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_CODIGO_MATERIAL_TAMANHO_DORMENTE_id_tamanho",
                        column: x => x.id_tamanho,
                        principalTable: "TAMANHO_DORMENTE",
                        principalColumn: "id_tamanho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OFERTA_FORNECEDOR",
                columns: table => new
                {
                    id_oferta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_empresa = table.Column<int>(type: "int", nullable: false),
                    id_especie = table.Column<int>(type: "int", nullable: false),
                    id_tamanho = table.Column<int>(type: "int", nullable: false),
                    volume_m3_disponivel = table.Column<decimal>(type: "decimal(12,5)", precision: 12, scale: 5, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OFERTA_FORNECEDOR", x => x.id_oferta);
                    table.ForeignKey(
                        name: "FK_OFERTA_FORNECEDOR_EMPRESA_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "EMPRESA",
                        principalColumn: "id_empresa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OFERTA_FORNECEDOR_ESPECIE_id_especie",
                        column: x => x.id_especie,
                        principalTable: "ESPECIE",
                        principalColumn: "id_especie",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OFERTA_FORNECEDOR_TAMANHO_DORMENTE_id_tamanho",
                        column: x => x.id_tamanho,
                        principalTable: "TAMANHO_DORMENTE",
                        principalColumn: "id_tamanho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PILHA",
                columns: table => new
                {
                    id_pilha = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_armazem = table.Column<int>(type: "int", nullable: false),
                    setor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    codigo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    id_tamanho = table.Column<int>(type: "int", nullable: false),
                    classe = table.Column<byte>(type: "tinyint", nullable: false),
                    ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PILHA", x => x.id_pilha);
                    table.CheckConstraint("CK_PILHA_classe", "[classe] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_PILHA_ARMAZEM_id_armazem",
                        column: x => x.id_armazem,
                        principalTable: "ARMAZEM",
                        principalColumn: "id_armazem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PILHA_TAMANHO_DORMENTE_id_tamanho",
                        column: x => x.id_tamanho,
                        principalTable: "TAMANHO_DORMENTE",
                        principalColumn: "id_tamanho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LEITOR",
                columns: table => new
                {
                    id_leitor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_tipo_leitor = table.Column<int>(type: "int", nullable: false),
                    id_setor = table.Column<int>(type: "int", nullable: true),
                    codigo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    numero_serie = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    versao_hardware = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    versao_firmware = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEITOR", x => x.id_leitor);
                    table.ForeignKey(
                        name: "FK_LEITOR_SETOR_id_setor",
                        column: x => x.id_setor,
                        principalTable: "SETOR",
                        principalColumn: "id_setor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEITOR_TIPO_LEITOR_id_tipo_leitor",
                        column: x => x.id_tipo_leitor,
                        principalTable: "TIPO_LEITOR",
                        principalColumn: "id_tipo_leitor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MODELO_OCR",
                columns: table => new
                {
                    id_modelo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    versao = table.Column<int>(type: "int", nullable: false),
                    modelo_base = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    treinado_em = table.Column<DateTime>(type: "datetime2", nullable: true),
                    amostras = table.Column<int>(type: "int", nullable: false),
                    acerto_teste = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: true),
                    ativo = table.Column<bool>(type: "bit", nullable: false),
                    id_usuario_ativou = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MODELO_OCR", x => x.id_modelo);
                    table.ForeignKey(
                        name: "FK_MODELO_OCR_USUARIO_id_usuario_ativou",
                        column: x => x.id_usuario_ativou,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MODO_LEITURA",
                columns: table => new
                {
                    id_modo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    potencia_dbm = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    folga_rssi_db = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    antena = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    exige_alvo_unico = table.Column<bool>(type: "bit", nullable: false),
                    alterado_por = table.Column<int>(type: "int", nullable: true),
                    alterado_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MODO_LEITURA", x => x.id_modo);
                    table.ForeignKey(
                        name: "FK_MODO_LEITURA_USUARIO_alterado_por",
                        column: x => x.alterado_por,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PARAMETRO",
                columns: table => new
                {
                    chave = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    valor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    alterado_por = table.Column<int>(type: "int", nullable: true),
                    alterado_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARAMETRO", x => x.chave);
                    table.ForeignKey(
                        name: "FK_PARAMETRO_USUARIO_alterado_por",
                        column: x => x.alterado_por,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PEDIDO",
                columns: table => new
                {
                    id_pedido = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_empresa = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_contrato = table.Column<int>(type: "int", nullable: true),
                    codigo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    data_compra = table.Column<DateOnly>(type: "date", nullable: false),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    criado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEDIDO", x => x.id_pedido);
                    table.ForeignKey(
                        name: "FK_PEDIDO_CONTRATO_id_contrato",
                        column: x => x.id_contrato,
                        principalTable: "CONTRATO",
                        principalColumn: "id_contrato",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PEDIDO_EMPRESA_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "EMPRESA",
                        principalColumn: "id_empresa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PEDIDO_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RELATORIO_AMBIENTAL",
                columns: table => new
                {
                    id_relatorio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mes_referencia = table.Column<DateOnly>(type: "date", nullable: false),
                    gerado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    enviado_em = table.Column<DateTime>(type: "datetime2", nullable: true),
                    arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RELATORIO_AMBIENTAL", x => x.id_relatorio);
                    table.ForeignKey(
                        name: "FK_RELATORIO_AMBIENTAL_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CONFERENCIA_ARMAZEM",
                columns: table => new
                {
                    id_conferencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_armazem = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_leitor = table.Column<int>(type: "int", nullable: true),
                    iniciada_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    concluida_em = table.Column<DateTime>(type: "datetime2", nullable: true),
                    esperadas = table.Column<int>(type: "int", nullable: true),
                    lidas = table.Column<int>(type: "int", nullable: true),
                    faltando = table.Column<int>(type: "int", nullable: true),
                    a_mais = table.Column<int>(type: "int", nullable: true),
                    observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONFERENCIA_ARMAZEM", x => x.id_conferencia);
                    table.ForeignKey(
                        name: "FK_CONFERENCIA_ARMAZEM_ARMAZEM_id_armazem",
                        column: x => x.id_armazem,
                        principalTable: "ARMAZEM",
                        principalColumn: "id_armazem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CONFERENCIA_ARMAZEM_LEITOR_id_leitor",
                        column: x => x.id_leitor,
                        principalTable: "LEITOR",
                        principalColumn: "id_leitor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CONFERENCIA_ARMAZEM_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SENSOR",
                columns: table => new
                {
                    id_sensor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_leitor = table.Column<int>(type: "int", nullable: false),
                    tipo_sensor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    id_sensor_rw = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SENSOR", x => x.id_sensor);
                    table.ForeignKey(
                        name: "FK_SENSOR_LEITOR_id_leitor",
                        column: x => x.id_leitor,
                        principalTable: "LEITOR",
                        principalColumn: "id_leitor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LOTE",
                columns: table => new
                {
                    id_lote = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_empresa = table.Column<int>(type: "int", nullable: false),
                    id_pedido = table.Column<int>(type: "int", nullable: false),
                    codigo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    data_criacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOTE", x => x.id_lote);
                    table.ForeignKey(
                        name: "FK_LOTE_EMPRESA_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "EMPRESA",
                        principalColumn: "id_empresa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LOTE_PEDIDO_id_pedido",
                        column: x => x.id_pedido,
                        principalTable: "PEDIDO",
                        principalColumn: "id_pedido",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NOTA_FISCAL",
                columns: table => new
                {
                    id_nota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_pedido = table.Column<int>(type: "int", nullable: false),
                    numero = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    data_emissao = table.Column<DateOnly>(type: "date", nullable: false),
                    valor = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    origem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    status_conferencia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    id_usuario_conferiu = table.Column<int>(type: "int", nullable: true),
                    data_conferencia = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTA_FISCAL", x => x.id_nota);
                    table.ForeignKey(
                        name: "FK_NOTA_FISCAL_PEDIDO_id_pedido",
                        column: x => x.id_pedido,
                        principalTable: "PEDIDO",
                        principalColumn: "id_pedido",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NOTA_FISCAL_USUARIO_id_usuario_conferiu",
                        column: x => x.id_usuario_conferiu,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PEDIDO_HISTORICO",
                columns: table => new
                {
                    id_historico = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_pedido = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    retrato_antes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    retrato_depois = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEDIDO_HISTORICO", x => x.id_historico);
                    table.ForeignKey(
                        name: "FK_PEDIDO_HISTORICO_PEDIDO_id_pedido",
                        column: x => x.id_pedido,
                        principalTable: "PEDIDO",
                        principalColumn: "id_pedido",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PEDIDO_HISTORICO_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PEDIDO_ITEM",
                columns: table => new
                {
                    id_pedido_item = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_pedido = table.Column<int>(type: "int", nullable: false),
                    id_tamanho = table.Column<int>(type: "int", nullable: false),
                    classe = table.Column<byte>(type: "tinyint", nullable: false),
                    id_especie = table.Column<int>(type: "int", nullable: true),
                    quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEDIDO_ITEM", x => x.id_pedido_item);
                    table.CheckConstraint("CK_PEDIDO_ITEM_classe", "[classe] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_PEDIDO_ITEM_ESPECIE_id_especie",
                        column: x => x.id_especie,
                        principalTable: "ESPECIE",
                        principalColumn: "id_especie",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PEDIDO_ITEM_PEDIDO_id_pedido",
                        column: x => x.id_pedido,
                        principalTable: "PEDIDO",
                        principalColumn: "id_pedido",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PEDIDO_ITEM_TAMANHO_DORMENTE_id_tamanho",
                        column: x => x.id_tamanho,
                        principalTable: "TAMANHO_DORMENTE",
                        principalColumn: "id_tamanho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RELATORIO_AMBIENTAL_ITEM",
                columns: table => new
                {
                    id_item = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_relatorio = table.Column<int>(type: "int", nullable: false),
                    id_especie = table.Column<int>(type: "int", nullable: false),
                    volume_m3_estoque = table.Column<decimal>(type: "decimal(12,5)", precision: 12, scale: 5, nullable: false),
                    volume_m3_entrada = table.Column<decimal>(type: "decimal(12,5)", precision: 12, scale: 5, nullable: false),
                    volume_m3_saida = table.Column<decimal>(type: "decimal(12,5)", precision: 12, scale: 5, nullable: false),
                    idade_media_dias = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RELATORIO_AMBIENTAL_ITEM", x => x.id_item);
                    table.ForeignKey(
                        name: "FK_RELATORIO_AMBIENTAL_ITEM_ESPECIE_id_especie",
                        column: x => x.id_especie,
                        principalTable: "ESPECIE",
                        principalColumn: "id_especie",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RELATORIO_AMBIENTAL_ITEM_RELATORIO_AMBIENTAL_id_relatorio",
                        column: x => x.id_relatorio,
                        principalTable: "RELATORIO_AMBIENTAL",
                        principalColumn: "id_relatorio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NOTA_FISCAL_ITEM",
                columns: table => new
                {
                    id_nota_item = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_nota = table.Column<int>(type: "int", nullable: false),
                    id_especie = table.Column<int>(type: "int", nullable: false),
                    id_tamanho = table.Column<int>(type: "int", nullable: false),
                    classe = table.Column<byte>(type: "tinyint", nullable: false),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    volume_m3 = table.Column<decimal>(type: "decimal(12,5)", precision: 12, scale: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTA_FISCAL_ITEM", x => x.id_nota_item);
                    table.CheckConstraint("CK_NOTA_FISCAL_ITEM_classe", "[classe] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_NOTA_FISCAL_ITEM_ESPECIE_id_especie",
                        column: x => x.id_especie,
                        principalTable: "ESPECIE",
                        principalColumn: "id_especie",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NOTA_FISCAL_ITEM_NOTA_FISCAL_id_nota",
                        column: x => x.id_nota,
                        principalTable: "NOTA_FISCAL",
                        principalColumn: "id_nota",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NOTA_FISCAL_ITEM_TAMANHO_DORMENTE_id_tamanho",
                        column: x => x.id_tamanho,
                        principalTable: "TAMANHO_DORMENTE",
                        principalColumn: "id_tamanho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RECEBIMENTO",
                columns: table => new
                {
                    id_recebimento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_pedido = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_nota = table.Column<int>(type: "int", nullable: true),
                    documento_origem_tipo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    documento_origem_numero = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    data_recebimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    quantidade_recebida = table.Column<int>(type: "int", nullable: false),
                    observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    id_armazem = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RECEBIMENTO", x => x.id_recebimento);
                    table.ForeignKey(
                        name: "FK_RECEBIMENTO_ARMAZEM_id_armazem",
                        column: x => x.id_armazem,
                        principalTable: "ARMAZEM",
                        principalColumn: "id_armazem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RECEBIMENTO_NOTA_FISCAL_id_nota",
                        column: x => x.id_nota,
                        principalTable: "NOTA_FISCAL",
                        principalColumn: "id_nota",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RECEBIMENTO_PEDIDO_id_pedido",
                        column: x => x.id_pedido,
                        principalTable: "PEDIDO",
                        principalColumn: "id_pedido",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RECEBIMENTO_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FEIXE",
                columns: table => new
                {
                    id_feixe = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_recebimento = table.Column<int>(type: "int", nullable: false),
                    numero_feixe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    id_especie = table.Column<int>(type: "int", nullable: false),
                    id_tamanho = table.Column<int>(type: "int", nullable: false),
                    tratamento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    id_empresa_tratamento = table.Column<int>(type: "int", nullable: true),
                    id_empresa_inspecao = table.Column<int>(type: "int", nullable: true),
                    ano_inspecao = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FEIXE", x => x.id_feixe);
                    table.ForeignKey(
                        name: "FK_FEIXE_EMPRESA_id_empresa_inspecao",
                        column: x => x.id_empresa_inspecao,
                        principalTable: "EMPRESA",
                        principalColumn: "id_empresa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FEIXE_EMPRESA_id_empresa_tratamento",
                        column: x => x.id_empresa_tratamento,
                        principalTable: "EMPRESA",
                        principalColumn: "id_empresa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FEIXE_ESPECIE_id_especie",
                        column: x => x.id_especie,
                        principalTable: "ESPECIE",
                        principalColumn: "id_especie",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FEIXE_RECEBIMENTO_id_recebimento",
                        column: x => x.id_recebimento,
                        principalTable: "RECEBIMENTO",
                        principalColumn: "id_recebimento",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FEIXE_TAMANHO_DORMENTE_id_tamanho",
                        column: x => x.id_tamanho,
                        principalTable: "TAMANHO_DORMENTE",
                        principalColumn: "id_tamanho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DORMENTE",
                columns: table => new
                {
                    id_dormente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_lote = table.Column<int>(type: "int", nullable: false),
                    id_feixe = table.Column<int>(type: "int", nullable: true),
                    tag_fornecedor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    tag_fornecedor_leitura = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    km_projeto = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    id_trecho = table.Column<int>(type: "int", nullable: true),
                    frente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    id_pilha = table.Column<int>(type: "int", nullable: true),
                    entrada_armazem_em = table.Column<DateTime>(type: "datetime2", nullable: true),
                    retirada_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DORMENTE", x => x.id_dormente);
                    table.ForeignKey(
                        name: "FK_DORMENTE_FEIXE_id_feixe",
                        column: x => x.id_feixe,
                        principalTable: "FEIXE",
                        principalColumn: "id_feixe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DORMENTE_LOTE_id_lote",
                        column: x => x.id_lote,
                        principalTable: "LOTE",
                        principalColumn: "id_lote",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DORMENTE_PILHA_id_pilha",
                        column: x => x.id_pilha,
                        principalTable: "PILHA",
                        principalColumn: "id_pilha",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DORMENTE_TRECHO_id_trecho",
                        column: x => x.id_trecho,
                        principalTable: "TRECHO",
                        principalColumn: "id_trecho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LEITURA_OCR",
                columns: table => new
                {
                    id_leitura_ocr = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    origem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    foto_arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    texto_lido = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    confianca_media = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: true),
                    id_modelo = table.Column<int>(type: "int", nullable: false),
                    id_recebimento = table.Column<int>(type: "int", nullable: true),
                    id_feixe = table.Column<int>(type: "int", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEITURA_OCR", x => x.id_leitura_ocr);
                    table.ForeignKey(
                        name: "FK_LEITURA_OCR_FEIXE_id_feixe",
                        column: x => x.id_feixe,
                        principalTable: "FEIXE",
                        principalColumn: "id_feixe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEITURA_OCR_MODELO_OCR_id_modelo",
                        column: x => x.id_modelo,
                        principalTable: "MODELO_OCR",
                        principalColumn: "id_modelo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEITURA_OCR_RECEBIMENTO_id_recebimento",
                        column: x => x.id_recebimento,
                        principalTable: "RECEBIMENTO",
                        principalColumn: "id_recebimento",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEITURA_OCR_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CLASSIFICACAO_MADEIRA",
                columns: table => new
                {
                    id_classificacao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_dormente = table.Column<int>(type: "int", nullable: false),
                    id_especie = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    anterior = table.Column<int>(type: "int", nullable: true),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    metodo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    metodo_captura = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLASSIFICACAO_MADEIRA", x => x.id_classificacao);
                    table.ForeignKey(
                        name: "FK_CLASSIFICACAO_MADEIRA_CLASSIFICACAO_MADEIRA_anterior",
                        column: x => x.anterior,
                        principalTable: "CLASSIFICACAO_MADEIRA",
                        principalColumn: "id_classificacao",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CLASSIFICACAO_MADEIRA_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CLASSIFICACAO_MADEIRA_ESPECIE_id_especie",
                        column: x => x.id_especie,
                        principalTable: "ESPECIE",
                        principalColumn: "id_especie",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CLASSIFICACAO_MADEIRA_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DETECCAO_IMAGEM",
                columns: table => new
                {
                    id_deteccao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_passagem = table.Column<int>(type: "int", nullable: false),
                    id_dormente = table.Column<int>(type: "int", nullable: true),
                    km_medido = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    quadro_arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    camera = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    caixa_x = table.Column<int>(type: "int", nullable: false),
                    caixa_y = table.Column<int>(type: "int", nullable: false),
                    caixa_largura = table.Column<int>(type: "int", nullable: false),
                    caixa_altura = table.Column<int>(type: "int", nullable: false),
                    defeito = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    grau = table.Column<byte>(type: "tinyint", nullable: false),
                    confianca = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    versao_modelo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DETECCAO_IMAGEM", x => x.id_deteccao);
                    table.CheckConstraint("CK_DETECCAO_IMAGEM_confianca", "[confianca] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DETECCAO_IMAGEM_grau", "[grau] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_DETECCAO_IMAGEM_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DETECCAO_IMAGEM_PASSAGEM_id_passagem",
                        column: x => x.id_passagem,
                        principalTable: "PASSAGEM",
                        principalColumn: "id_passagem",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EVENTO_POSICAO",
                columns: table => new
                {
                    id_evento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_dormente = table.Column<int>(type: "int", nullable: false),
                    id_passagem = table.Column<int>(type: "int", nullable: false),
                    faixa_anterior = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    faixa_nova = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    desvio_mm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EVENTO_POSICAO", x => x.id_evento);
                    table.ForeignKey(
                        name: "FK_EVENTO_POSICAO_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EVENTO_POSICAO_PASSAGEM_id_passagem",
                        column: x => x.id_passagem,
                        principalTable: "PASSAGEM",
                        principalColumn: "id_passagem",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LEITURA_DORMENTE",
                columns: table => new
                {
                    id_leitura = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_dormente = table.Column<int>(type: "int", nullable: true),
                    id_leitor = table.Column<int>(type: "int", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    metodo_captura = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    leitura = table.Column<bool>(type: "bit", nullable: false),
                    data_leitura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    umidade = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    temperatura = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    tag_lida = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    id_modo = table.Column<int>(type: "int", nullable: true),
                    potencia_dbm = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    rssi_dbm = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    tags_vistas = table.Column<int>(type: "int", nullable: true),
                    ambigua = table.Column<bool>(type: "bit", nullable: false),
                    id_conferencia = table.Column<int>(type: "int", nullable: true),
                    km = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    aviso = table.Column<bool>(type: "bit", nullable: false),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEITURA_DORMENTE", x => x.id_leitura);
                    table.ForeignKey(
                        name: "FK_LEITURA_DORMENTE_CONFERENCIA_ARMAZEM_id_conferencia",
                        column: x => x.id_conferencia,
                        principalTable: "CONFERENCIA_ARMAZEM",
                        principalColumn: "id_conferencia",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEITURA_DORMENTE_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEITURA_DORMENTE_LEITOR_id_leitor",
                        column: x => x.id_leitor,
                        principalTable: "LEITOR",
                        principalColumn: "id_leitor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEITURA_DORMENTE_MODO_LEITURA_id_modo",
                        column: x => x.id_modo,
                        principalTable: "MODO_LEITURA",
                        principalColumn: "id_modo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEITURA_DORMENTE_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LEITURA_VIA",
                columns: table => new
                {
                    id_leitura_via = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_passagem = table.Column<int>(type: "int", nullable: false),
                    id_dormente = table.Column<int>(type: "int", nullable: true),
                    leitura = table.Column<bool>(type: "bit", nullable: false),
                    km_medido = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    desvio_mm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    umidade = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    temperatura = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    rachadura_suspeita = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEITURA_VIA", x => new { x.id_leitura_via, x.data });
                    table.ForeignKey(
                        name: "FK_LEITURA_VIA_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEITURA_VIA_PASSAGEM_id_passagem",
                        column: x => x.id_passagem,
                        principalTable: "PASSAGEM",
                        principalColumn: "id_passagem",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "POSICAO_ATUAL",
                columns: table => new
                {
                    id_dormente = table.Column<int>(type: "int", nullable: false),
                    id_passagem = table.Column<int>(type: "int", nullable: false),
                    km_medido = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    desvio_mm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    faixa = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSICAO_ATUAL", x => x.id_dormente);
                    table.ForeignKey(
                        name: "FK_POSICAO_ATUAL_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_POSICAO_ATUAL_PASSAGEM_id_passagem",
                        column: x => x.id_passagem,
                        principalTable: "PASSAGEM",
                        principalColumn: "id_passagem",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RFID",
                columns: table => new
                {
                    id_rfid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_dormente = table.Column<int>(type: "int", nullable: false),
                    tag_rfid = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ativo = table.Column<bool>(type: "bit", nullable: false),
                    vinculada_em = table.Column<DateTime>(type: "datetime2", nullable: true),
                    id_usuario_vinculou = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RFID", x => x.id_rfid);
                    table.ForeignKey(
                        name: "FK_RFID_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RFID_USUARIO_id_usuario_vinculou",
                        column: x => x.id_usuario_vinculou,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VERIFICACAO_QUALIDADE",
                columns: table => new
                {
                    id_verificacao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_dormente = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ataque_biologico = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    rachadura = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    placa_antirrachante = table.Column<bool>(type: "bit", nullable: false),
                    placa_confere = table.Column<bool>(type: "bit", nullable: false),
                    resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    foto_arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    destino = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VERIFICACAO_QUALIDADE", x => x.id_verificacao);
                    table.CheckConstraint("CK_VERIFICACAO_QUALIDADE_reprovada", "[resultado] <> 'Reprovada' OR ([motivo] IS NOT NULL AND [foto_arquivo] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_VERIFICACAO_QUALIDADE_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VERIFICACAO_QUALIDADE_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OCR_CAMPO",
                columns: table => new
                {
                    id_campo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_leitura_ocr = table.Column<int>(type: "int", nullable: false),
                    campo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    valor_lido = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    confianca = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: true),
                    confere_pedido = table.Column<bool>(type: "bit", nullable: true),
                    valor_confirmado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    corrigido = table.Column<bool>(type: "bit", nullable: true),
                    caixa_x = table.Column<int>(type: "int", nullable: true),
                    caixa_y = table.Column<int>(type: "int", nullable: true),
                    caixa_largura = table.Column<int>(type: "int", nullable: true),
                    caixa_altura = table.Column<int>(type: "int", nullable: true),
                    id_modelo_treino = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCR_CAMPO", x => x.id_campo);
                    table.ForeignKey(
                        name: "FK_OCR_CAMPO_LEITURA_OCR_id_leitura_ocr",
                        column: x => x.id_leitura_ocr,
                        principalTable: "LEITURA_OCR",
                        principalColumn: "id_leitura_ocr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OCR_CAMPO_MODELO_OCR_id_modelo_treino",
                        column: x => x.id_modelo_treino,
                        principalTable: "MODELO_OCR",
                        principalColumn: "id_modelo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ALERTA",
                columns: table => new
                {
                    id_alerta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    origem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    id_leitura_via = table.Column<long>(type: "bigint", nullable: true),
                    id_deteccao = table.Column<int>(type: "int", nullable: true),
                    id_dormente = table.Column<int>(type: "int", nullable: true),
                    id_usuario_decisao = table.Column<int>(type: "int", nullable: true),
                    tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    desvio_mm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    data_decisao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    motivo_descarte = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ALERTA", x => x.id_alerta);
                    table.CheckConstraint("CK_ALERTA_origem", "([origem] = 'Sensor' AND [id_leitura_via] IS NOT NULL) OR ([origem] = 'Imagem' AND [id_deteccao] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ALERTA_DETECCAO_IMAGEM_id_deteccao",
                        column: x => x.id_deteccao,
                        principalTable: "DETECCAO_IMAGEM",
                        principalColumn: "id_deteccao",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ALERTA_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ALERTA_USUARIO_id_usuario_decisao",
                        column: x => x.id_usuario_decisao,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GRAVACAO_TAG",
                columns: table => new
                {
                    id_gravacao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_rfid = table.Column<int>(type: "int", nullable: true),
                    tag_gravada = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    tag_relida = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tags_vistas = table.Column<int>(type: "int", nullable: false),
                    rssi_dbm = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    id_modo = table.Column<int>(type: "int", nullable: false),
                    id_leitor = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GRAVACAO_TAG", x => x.id_gravacao);
                    table.ForeignKey(
                        name: "FK_GRAVACAO_TAG_LEITOR_id_leitor",
                        column: x => x.id_leitor,
                        principalTable: "LEITOR",
                        principalColumn: "id_leitor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GRAVACAO_TAG_MODO_LEITURA_id_modo",
                        column: x => x.id_modo,
                        principalTable: "MODO_LEITURA",
                        principalColumn: "id_modo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GRAVACAO_TAG_RFID_id_rfid",
                        column: x => x.id_rfid,
                        principalTable: "RFID",
                        principalColumn: "id_rfid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GRAVACAO_TAG_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "INSPECAO",
                columns: table => new
                {
                    id_inspecao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_dormente = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_alerta = table.Column<int>(type: "int", nullable: true),
                    id_inspecao_origem = table.Column<int>(type: "int", nullable: true),
                    iniciado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    finalizado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    duracao_s = table.Column<int>(type: "int", nullable: false),
                    versao_checklist = table.Column<int>(type: "int", nullable: false),
                    estado_conservacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    rachadura = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ataque_biologico = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    umidade_pct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    vida_util_meses = table.Column<int>(type: "int", nullable: true),
                    tipo_madeira_confere = table.Column<bool>(type: "bit", nullable: false),
                    resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    foto_arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INSPECAO", x => x.id_inspecao);
                    table.ForeignKey(
                        name: "FK_INSPECAO_ALERTA_id_alerta",
                        column: x => x.id_alerta,
                        principalTable: "ALERTA",
                        principalColumn: "id_alerta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSPECAO_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSPECAO_INSPECAO_id_inspecao_origem",
                        column: x => x.id_inspecao_origem,
                        principalTable: "INSPECAO",
                        principalColumn: "id_inspecao",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSPECAO_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VISTORIA_ALERTA",
                columns: table => new
                {
                    id_vistoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_alerta = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    viu = table.Column<bool>(type: "bit", nullable: false),
                    tag_lida = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    foto_arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VISTORIA_ALERTA", x => x.id_vistoria);
                    table.ForeignKey(
                        name: "FK_VISTORIA_ALERTA_ALERTA_id_alerta",
                        column: x => x.id_alerta,
                        principalTable: "ALERTA",
                        principalColumn: "id_alerta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VISTORIA_ALERTA_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TROCA",
                columns: table => new
                {
                    id_troca = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_dormente_saiu = table.Column<int>(type: "int", nullable: false),
                    id_dormente_entrou = table.Column<int>(type: "int", nullable: true),
                    id_inspecao = table.Column<int>(type: "int", nullable: true),
                    id_trecho = table.Column<int>(type: "int", nullable: true),
                    id_usuario_pediu = table.Column<int>(type: "int", nullable: false),
                    id_usuario_aprovou = table.Column<int>(type: "int", nullable: true),
                    data_aprovacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    motivo_reprovacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    id_usuario_separou = table.Column<int>(type: "int", nullable: true),
                    data_separacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    id_usuario_fez = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    origem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    foto_arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    cancelamento = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    data_pedido = table.Column<DateTime>(type: "datetime2", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: true),
                    tag_entrou = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    km = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    equipe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TROCA", x => x.id_troca);
                    table.ForeignKey(
                        name: "FK_TROCA_DORMENTE_id_dormente_entrou",
                        column: x => x.id_dormente_entrou,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TROCA_DORMENTE_id_dormente_saiu",
                        column: x => x.id_dormente_saiu,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TROCA_INSPECAO_id_inspecao",
                        column: x => x.id_inspecao,
                        principalTable: "INSPECAO",
                        principalColumn: "id_inspecao",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TROCA_TRECHO_id_trecho",
                        column: x => x.id_trecho,
                        principalTable: "TRECHO",
                        principalColumn: "id_trecho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TROCA_USUARIO_id_usuario_aprovou",
                        column: x => x.id_usuario_aprovou,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TROCA_USUARIO_id_usuario_fez",
                        column: x => x.id_usuario_fez,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TROCA_USUARIO_id_usuario_pediu",
                        column: x => x.id_usuario_pediu,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TROCA_USUARIO_id_usuario_separou",
                        column: x => x.id_usuario_separou,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "INSPECAO_CORRECAO",
                columns: table => new
                {
                    id_correcao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_inspecao = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    retrato_antes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    retrato_depois = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    id_troca_cancelada = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INSPECAO_CORRECAO", x => x.id_correcao);
                    table.ForeignKey(
                        name: "FK_INSPECAO_CORRECAO_INSPECAO_id_inspecao",
                        column: x => x.id_inspecao,
                        principalTable: "INSPECAO",
                        principalColumn: "id_inspecao",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSPECAO_CORRECAO_TROCA_id_troca_cancelada",
                        column: x => x.id_troca_cancelada,
                        principalTable: "TROCA",
                        principalColumn: "id_troca",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSPECAO_CORRECAO_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MOVIMENTACAO",
                columns: table => new
                {
                    id_movimentacao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_dormente = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    id_pilha_origem = table.Column<int>(type: "int", nullable: true),
                    id_pilha_destino = table.Column<int>(type: "int", nullable: true),
                    id_troca = table.Column<int>(type: "int", nullable: true),
                    id_leitura = table.Column<int>(type: "int", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_leitor = table.Column<int>(type: "int", nullable: true),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MOVIMENTACAO", x => x.id_movimentacao);
                    table.ForeignKey(
                        name: "FK_MOVIMENTACAO_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MOVIMENTACAO_LEITOR_id_leitor",
                        column: x => x.id_leitor,
                        principalTable: "LEITOR",
                        principalColumn: "id_leitor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MOVIMENTACAO_LEITURA_DORMENTE_id_leitura",
                        column: x => x.id_leitura,
                        principalTable: "LEITURA_DORMENTE",
                        principalColumn: "id_leitura",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MOVIMENTACAO_PILHA_id_pilha_destino",
                        column: x => x.id_pilha_destino,
                        principalTable: "PILHA",
                        principalColumn: "id_pilha",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MOVIMENTACAO_PILHA_id_pilha_origem",
                        column: x => x.id_pilha_origem,
                        principalTable: "PILHA",
                        principalColumn: "id_pilha",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MOVIMENTACAO_TROCA_id_troca",
                        column: x => x.id_troca,
                        principalTable: "TROCA",
                        principalColumn: "id_troca",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MOVIMENTACAO_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RECUSA_TROCA",
                columns: table => new
                {
                    id_recusa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_troca = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    foto_arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    decisao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    id_usuario_decisao = table.Column<int>(type: "int", nullable: true),
                    data_decisao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    resposta = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    registrado_em = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RECUSA_TROCA", x => x.id_recusa);
                    table.CheckConstraint("CK_RECUSA_TROCA_manter_com_resposta", "[decisao] IS NULL OR [decisao] <> 'Manter' OR [resposta] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_RECUSA_TROCA_TROCA_id_troca",
                        column: x => x.id_troca,
                        principalTable: "TROCA",
                        principalColumn: "id_troca",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RECUSA_TROCA_USUARIO_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RECUSA_TROCA_USUARIO_id_usuario_decisao",
                        column: x => x.id_usuario_decisao,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MODO_LEITURA",
                columns: new[] { "id_modo", "alterado_em", "alterado_por", "antena", "exige_alvo_unico", "folga_rssi_db", "nome", "potencia_dbm" },
                values: new object[,]
                {
                    { 1, null, null, "Curta", true, 12m, "Armazém", 10m },
                    { 2, null, null, "Unica", true, 9m, "Normal", 18m },
                    { 3, null, null, "Longa", false, 6m, "Via", 27m },
                    { 4, null, null, "Unica", true, 8m, "Personalizado", 20m }
                });

            migrationBuilder.InsertData(
                table: "PARAMETRO",
                columns: new[] { "chave", "alterado_em", "alterado_por", "valor" },
                values: new object[,]
                {
                    { "alerta_imagem_confianca_minima", null, null, "0.80" },
                    { "limite_segunda_classe_pct", null, null, "20" },
                    { "limite_segunda_classe_unidade", null, null, "m3" },
                    { "ocr_confianca_minima", null, null, "97" },
                    { "troca_quem_aprova", null, null, "supervisor" }
                });

            migrationBuilder.InsertData(
                table: "TAMANHO_DORMENTE",
                columns: new[] { "id_tamanho", "altura_m", "ativo", "comprimento_m", "largura_m", "medida_provisoria" },
                values: new object[,]
                {
                    { 1, 0.17m, true, 2.80m, 0.24m, true },
                    { 2, 0.17m, true, 3.00m, 0.24m, true },
                    { 3, 0.17m, true, 4.00m, 0.24m, true },
                    { 4, 0.17m, true, 4.80m, 0.24m, true }
                });

            migrationBuilder.InsertData(
                table: "TIPO_LEITOR",
                columns: new[] { "id_tipo_leitor", "descricao", "nome" },
                values: new object[,]
                {
                    { 1, "Crava a tag e manda o que os sensores mediram", "Pistola" },
                    { 2, "Módulo RFID UHF ligado pelo USB-C", "Módulo do celular" },
                    { 3, "Carro de inspeção da varredura noturna", "Carro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_id_deteccao",
                table: "ALERTA",
                column: "id_deteccao");

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_id_dormente",
                table: "ALERTA",
                column: "id_dormente");

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_id_leitura_via",
                table: "ALERTA",
                column: "id_leitura_via");

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_id_usuario_decisao",
                table: "ALERTA",
                column: "id_usuario_decisao");

            migrationBuilder.CreateIndex(
                name: "IX_ARMAZEM_nome",
                table: "ARMAZEM",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CLASSIFICACAO_MADEIRA_anterior",
                table: "CLASSIFICACAO_MADEIRA",
                column: "anterior");

            migrationBuilder.CreateIndex(
                name: "IX_CLASSIFICACAO_MADEIRA_id_cliente",
                table: "CLASSIFICACAO_MADEIRA",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CLASSIFICACAO_MADEIRA_id_dormente",
                table: "CLASSIFICACAO_MADEIRA",
                column: "id_dormente");

            migrationBuilder.CreateIndex(
                name: "IX_CLASSIFICACAO_MADEIRA_id_especie",
                table: "CLASSIFICACAO_MADEIRA",
                column: "id_especie");

            migrationBuilder.CreateIndex(
                name: "IX_CLASSIFICACAO_MADEIRA_id_usuario",
                table: "CLASSIFICACAO_MADEIRA",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_CODIGO_MATERIAL_id_tamanho_classe",
                table: "CODIGO_MATERIAL",
                columns: new[] { "id_tamanho", "classe" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CONFERENCIA_ARMAZEM_id_armazem",
                table: "CONFERENCIA_ARMAZEM",
                column: "id_armazem");

            migrationBuilder.CreateIndex(
                name: "IX_CONFERENCIA_ARMAZEM_id_cliente",
                table: "CONFERENCIA_ARMAZEM",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CONFERENCIA_ARMAZEM_id_leitor",
                table: "CONFERENCIA_ARMAZEM",
                column: "id_leitor");

            migrationBuilder.CreateIndex(
                name: "IX_CONFERENCIA_ARMAZEM_id_usuario",
                table: "CONFERENCIA_ARMAZEM",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_CONTRATO_id_empresa",
                table: "CONTRATO",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_CONTRATO_numero_contrato",
                table: "CONTRATO",
                column: "numero_contrato",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DETECCAO_IMAGEM_id_dormente",
                table: "DETECCAO_IMAGEM",
                column: "id_dormente");

            migrationBuilder.CreateIndex(
                name: "IX_DETECCAO_IMAGEM_id_passagem",
                table: "DETECCAO_IMAGEM",
                column: "id_passagem");

            migrationBuilder.CreateIndex(
                name: "IX_DORMENTE_id_feixe",
                table: "DORMENTE",
                column: "id_feixe");

            migrationBuilder.CreateIndex(
                name: "IX_DORMENTE_id_lote",
                table: "DORMENTE",
                column: "id_lote");

            migrationBuilder.CreateIndex(
                name: "IX_DORMENTE_id_pilha",
                table: "DORMENTE",
                column: "id_pilha");

            migrationBuilder.CreateIndex(
                name: "IX_DORMENTE_id_trecho",
                table: "DORMENTE",
                column: "id_trecho");

            migrationBuilder.CreateIndex(
                name: "IX_ESPECIE_nome_cientifico",
                table: "ESPECIE",
                column: "nome_cientifico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ESTACAO_linha_ordem",
                table: "ESTACAO",
                columns: new[] { "linha", "ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EVENTO_POSICAO_id_dormente",
                table: "EVENTO_POSICAO",
                column: "id_dormente");

            migrationBuilder.CreateIndex(
                name: "IX_EVENTO_POSICAO_id_passagem",
                table: "EVENTO_POSICAO",
                column: "id_passagem");

            migrationBuilder.CreateIndex(
                name: "IX_FEIXE_id_empresa_inspecao",
                table: "FEIXE",
                column: "id_empresa_inspecao");

            migrationBuilder.CreateIndex(
                name: "IX_FEIXE_id_empresa_tratamento",
                table: "FEIXE",
                column: "id_empresa_tratamento");

            migrationBuilder.CreateIndex(
                name: "IX_FEIXE_id_especie",
                table: "FEIXE",
                column: "id_especie");

            migrationBuilder.CreateIndex(
                name: "IX_FEIXE_id_recebimento",
                table: "FEIXE",
                column: "id_recebimento");

            migrationBuilder.CreateIndex(
                name: "IX_FEIXE_id_tamanho",
                table: "FEIXE",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_GRAVACAO_TAG_id_cliente",
                table: "GRAVACAO_TAG",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GRAVACAO_TAG_id_leitor",
                table: "GRAVACAO_TAG",
                column: "id_leitor");

            migrationBuilder.CreateIndex(
                name: "IX_GRAVACAO_TAG_id_modo",
                table: "GRAVACAO_TAG",
                column: "id_modo");

            migrationBuilder.CreateIndex(
                name: "IX_GRAVACAO_TAG_id_rfid",
                table: "GRAVACAO_TAG",
                column: "id_rfid");

            migrationBuilder.CreateIndex(
                name: "IX_GRAVACAO_TAG_id_usuario",
                table: "GRAVACAO_TAG",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_GRAVACAO_TAG_tag_gravada",
                table: "GRAVACAO_TAG",
                column: "tag_gravada");

            migrationBuilder.CreateIndex(
                name: "IX_INSPECAO_id_alerta",
                table: "INSPECAO",
                column: "id_alerta");

            migrationBuilder.CreateIndex(
                name: "IX_INSPECAO_id_cliente",
                table: "INSPECAO",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_INSPECAO_id_dormente",
                table: "INSPECAO",
                column: "id_dormente");

            migrationBuilder.CreateIndex(
                name: "IX_INSPECAO_id_inspecao_origem",
                table: "INSPECAO",
                column: "id_inspecao_origem");

            migrationBuilder.CreateIndex(
                name: "IX_INSPECAO_id_usuario",
                table: "INSPECAO",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_INSPECAO_CORRECAO_id_inspecao",
                table: "INSPECAO_CORRECAO",
                column: "id_inspecao");

            migrationBuilder.CreateIndex(
                name: "IX_INSPECAO_CORRECAO_id_troca_cancelada",
                table: "INSPECAO_CORRECAO",
                column: "id_troca_cancelada");

            migrationBuilder.CreateIndex(
                name: "IX_INSPECAO_CORRECAO_id_usuario",
                table: "INSPECAO_CORRECAO",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_LEITOR_codigo",
                table: "LEITOR",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LEITOR_id_setor",
                table: "LEITOR",
                column: "id_setor");

            migrationBuilder.CreateIndex(
                name: "IX_LEITOR_id_tipo_leitor",
                table: "LEITOR",
                column: "id_tipo_leitor");

            migrationBuilder.CreateIndex(
                name: "IX_LEITOR_numero_serie",
                table: "LEITOR",
                column: "numero_serie",
                unique: true,
                filter: "[numero_serie] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_DORMENTE_id_cliente",
                table: "LEITURA_DORMENTE",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_DORMENTE_id_conferencia",
                table: "LEITURA_DORMENTE",
                column: "id_conferencia");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_DORMENTE_id_dormente",
                table: "LEITURA_DORMENTE",
                column: "id_dormente");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_DORMENTE_id_leitor",
                table: "LEITURA_DORMENTE",
                column: "id_leitor");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_DORMENTE_id_modo",
                table: "LEITURA_DORMENTE",
                column: "id_modo");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_DORMENTE_id_usuario",
                table: "LEITURA_DORMENTE",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_DORMENTE_tag_lida",
                table: "LEITURA_DORMENTE",
                column: "tag_lida");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_OCR_id_cliente",
                table: "LEITURA_OCR",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_OCR_id_feixe",
                table: "LEITURA_OCR",
                column: "id_feixe");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_OCR_id_modelo",
                table: "LEITURA_OCR",
                column: "id_modelo");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_OCR_id_recebimento",
                table: "LEITURA_OCR",
                column: "id_recebimento");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_OCR_id_usuario",
                table: "LEITURA_OCR",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_VIA_id_dormente_data",
                table: "LEITURA_VIA",
                columns: new[] { "id_dormente", "data" });

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_VIA_id_passagem",
                table: "LEITURA_VIA",
                column: "id_passagem");

            migrationBuilder.CreateIndex(
                name: "IX_LOTE_codigo",
                table: "LOTE",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LOTE_id_empresa",
                table: "LOTE",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_LOTE_id_pedido",
                table: "LOTE",
                column: "id_pedido");

            migrationBuilder.CreateIndex(
                name: "IX_MODELO_OCR_id_usuario_ativou",
                table: "MODELO_OCR",
                column: "id_usuario_ativou");

            migrationBuilder.CreateIndex(
                name: "IX_MODELO_OCR_nome_versao",
                table: "MODELO_OCR",
                columns: new[] { "nome", "versao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MODELO_OCR_um_ativo",
                table: "MODELO_OCR",
                column: "ativo",
                unique: true,
                filter: "[ativo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MODO_LEITURA_alterado_por",
                table: "MODO_LEITURA",
                column: "alterado_por");

            migrationBuilder.CreateIndex(
                name: "IX_MODO_LEITURA_nome",
                table: "MODO_LEITURA",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MOVIMENTACAO_id_cliente",
                table: "MOVIMENTACAO",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MOVIMENTACAO_id_dormente",
                table: "MOVIMENTACAO",
                column: "id_dormente");

            migrationBuilder.CreateIndex(
                name: "IX_MOVIMENTACAO_id_leitor",
                table: "MOVIMENTACAO",
                column: "id_leitor");

            migrationBuilder.CreateIndex(
                name: "IX_MOVIMENTACAO_id_leitura",
                table: "MOVIMENTACAO",
                column: "id_leitura");

            migrationBuilder.CreateIndex(
                name: "IX_MOVIMENTACAO_id_pilha_destino",
                table: "MOVIMENTACAO",
                column: "id_pilha_destino");

            migrationBuilder.CreateIndex(
                name: "IX_MOVIMENTACAO_id_pilha_origem",
                table: "MOVIMENTACAO",
                column: "id_pilha_origem");

            migrationBuilder.CreateIndex(
                name: "IX_MOVIMENTACAO_id_troca",
                table: "MOVIMENTACAO",
                column: "id_troca");

            migrationBuilder.CreateIndex(
                name: "IX_MOVIMENTACAO_id_usuario",
                table: "MOVIMENTACAO",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_NOTA_FISCAL_id_pedido",
                table: "NOTA_FISCAL",
                column: "id_pedido");

            migrationBuilder.CreateIndex(
                name: "IX_NOTA_FISCAL_id_usuario_conferiu",
                table: "NOTA_FISCAL",
                column: "id_usuario_conferiu");

            migrationBuilder.CreateIndex(
                name: "IX_NOTA_FISCAL_ITEM_id_especie",
                table: "NOTA_FISCAL_ITEM",
                column: "id_especie");

            migrationBuilder.CreateIndex(
                name: "IX_NOTA_FISCAL_ITEM_id_nota",
                table: "NOTA_FISCAL_ITEM",
                column: "id_nota");

            migrationBuilder.CreateIndex(
                name: "IX_NOTA_FISCAL_ITEM_id_tamanho",
                table: "NOTA_FISCAL_ITEM",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_OCR_CAMPO_id_leitura_ocr",
                table: "OCR_CAMPO",
                column: "id_leitura_ocr");

            migrationBuilder.CreateIndex(
                name: "IX_OCR_CAMPO_id_modelo_treino",
                table: "OCR_CAMPO",
                column: "id_modelo_treino");

            migrationBuilder.CreateIndex(
                name: "IX_OFERTA_FORNECEDOR_id_empresa",
                table: "OFERTA_FORNECEDOR",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_OFERTA_FORNECEDOR_id_especie",
                table: "OFERTA_FORNECEDOR",
                column: "id_especie");

            migrationBuilder.CreateIndex(
                name: "IX_OFERTA_FORNECEDOR_id_tamanho",
                table: "OFERTA_FORNECEDOR",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_PARAMETRO_alterado_por",
                table: "PARAMETRO",
                column: "alterado_por");

            migrationBuilder.CreateIndex(
                name: "IX_PASSAGEM_codigo",
                table: "PASSAGEM",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_codigo",
                table: "PEDIDO",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_id_contrato",
                table: "PEDIDO",
                column: "id_contrato");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_id_empresa",
                table: "PEDIDO",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_id_usuario",
                table: "PEDIDO",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_HISTORICO_id_pedido",
                table: "PEDIDO_HISTORICO",
                column: "id_pedido");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_HISTORICO_id_usuario",
                table: "PEDIDO_HISTORICO",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_ITEM_id_especie",
                table: "PEDIDO_ITEM",
                column: "id_especie");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_ITEM_id_pedido",
                table: "PEDIDO_ITEM",
                column: "id_pedido");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_ITEM_id_tamanho",
                table: "PEDIDO_ITEM",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_PESSOA_email",
                table: "PESSOA",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PILHA_id_armazem_codigo",
                table: "PILHA",
                columns: new[] { "id_armazem", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PILHA_id_tamanho",
                table: "PILHA",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_POSICAO_ATUAL_id_passagem",
                table: "POSICAO_ATUAL",
                column: "id_passagem");

            migrationBuilder.CreateIndex(
                name: "IX_RECEBIMENTO_id_armazem",
                table: "RECEBIMENTO",
                column: "id_armazem");

            migrationBuilder.CreateIndex(
                name: "IX_RECEBIMENTO_id_nota",
                table: "RECEBIMENTO",
                column: "id_nota");

            migrationBuilder.CreateIndex(
                name: "IX_RECEBIMENTO_id_pedido",
                table: "RECEBIMENTO",
                column: "id_pedido");

            migrationBuilder.CreateIndex(
                name: "IX_RECEBIMENTO_id_usuario",
                table: "RECEBIMENTO",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_RECUSA_TROCA_id_cliente",
                table: "RECUSA_TROCA",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RECUSA_TROCA_id_troca",
                table: "RECUSA_TROCA",
                column: "id_troca");

            migrationBuilder.CreateIndex(
                name: "IX_RECUSA_TROCA_id_usuario",
                table: "RECUSA_TROCA",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_RECUSA_TROCA_id_usuario_decisao",
                table: "RECUSA_TROCA",
                column: "id_usuario_decisao");

            migrationBuilder.CreateIndex(
                name: "IX_RELATORIO_AMBIENTAL_id_usuario",
                table: "RELATORIO_AMBIENTAL",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_RELATORIO_AMBIENTAL_ITEM_id_especie",
                table: "RELATORIO_AMBIENTAL_ITEM",
                column: "id_especie");

            migrationBuilder.CreateIndex(
                name: "IX_RELATORIO_AMBIENTAL_ITEM_id_relatorio_id_especie",
                table: "RELATORIO_AMBIENTAL_ITEM",
                columns: new[] { "id_relatorio", "id_especie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RFID_id_usuario_vinculou",
                table: "RFID",
                column: "id_usuario_vinculou");

            migrationBuilder.CreateIndex(
                name: "IX_RFID_tag_rfid",
                table: "RFID",
                column: "tag_rfid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RFID_uma_ativa_por_peca",
                table: "RFID",
                column: "id_dormente",
                unique: true,
                filter: "[ativo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SENSOR_id_leitor",
                table: "SENSOR",
                column: "id_leitor");

            migrationBuilder.CreateIndex(
                name: "IX_TAMANHO_DORMENTE_comprimento_m",
                table: "TAMANHO_DORMENTE",
                column: "comprimento_m",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TIPO_LEITOR_nome",
                table: "TIPO_LEITOR",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TRECHO_id_estacao_fim",
                table: "TRECHO",
                column: "id_estacao_fim");

            migrationBuilder.CreateIndex(
                name: "IX_TRECHO_id_estacao_inicio_id_estacao_fim",
                table: "TRECHO",
                columns: new[] { "id_estacao_inicio", "id_estacao_fim" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TROCA_id_dormente_entrou",
                table: "TROCA",
                column: "id_dormente_entrou");

            migrationBuilder.CreateIndex(
                name: "IX_TROCA_id_inspecao",
                table: "TROCA",
                column: "id_inspecao");

            migrationBuilder.CreateIndex(
                name: "IX_TROCA_id_trecho",
                table: "TROCA",
                column: "id_trecho");

            migrationBuilder.CreateIndex(
                name: "IX_TROCA_id_usuario_aprovou",
                table: "TROCA",
                column: "id_usuario_aprovou");

            migrationBuilder.CreateIndex(
                name: "IX_TROCA_id_usuario_fez",
                table: "TROCA",
                column: "id_usuario_fez");

            migrationBuilder.CreateIndex(
                name: "IX_TROCA_id_usuario_pediu",
                table: "TROCA",
                column: "id_usuario_pediu");

            migrationBuilder.CreateIndex(
                name: "IX_TROCA_id_usuario_separou",
                table: "TROCA",
                column: "id_usuario_separou");

            migrationBuilder.CreateIndex(
                name: "UX_TROCA_uma_aberta_por_peca",
                table: "TROCA",
                column: "id_dormente_saiu",
                unique: true,
                filter: "[status] IN ('Pedida', 'Aprovada', 'AguardandoPeca', 'Separada', 'Recusada')");

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_id_pessoa",
                table: "USUARIO",
                column: "id_pessoa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VERIFICACAO_QUALIDADE_id_cliente",
                table: "VERIFICACAO_QUALIDADE",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VERIFICACAO_QUALIDADE_id_dormente",
                table: "VERIFICACAO_QUALIDADE",
                column: "id_dormente");

            migrationBuilder.CreateIndex(
                name: "IX_VERIFICACAO_QUALIDADE_id_usuario",
                table: "VERIFICACAO_QUALIDADE",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_VISTORIA_ALERTA_id_alerta",
                table: "VISTORIA_ALERTA",
                column: "id_alerta");

            migrationBuilder.CreateIndex(
                name: "IX_VISTORIA_ALERTA_id_cliente",
                table: "VISTORIA_ALERTA",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VISTORIA_ALERTA_id_usuario",
                table: "VISTORIA_ALERTA",
                column: "id_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CLASSIFICACAO_MADEIRA");

            migrationBuilder.DropTable(
                name: "CODIGO_MATERIAL");

            migrationBuilder.DropTable(
                name: "EVENTO_POSICAO");

            migrationBuilder.DropTable(
                name: "GRAVACAO_TAG");

            migrationBuilder.DropTable(
                name: "INSPECAO_CORRECAO");

            migrationBuilder.DropTable(
                name: "INTEGRACAO_LOG");

            migrationBuilder.DropTable(
                name: "LEITURA_VIA");

            migrationBuilder.DropTable(
                name: "MOVIMENTACAO");

            migrationBuilder.DropTable(
                name: "NOTA_FISCAL_ITEM");

            migrationBuilder.DropTable(
                name: "OCR_CAMPO");

            migrationBuilder.DropTable(
                name: "OFERTA_FORNECEDOR");

            migrationBuilder.DropTable(
                name: "PARAMETRO");

            migrationBuilder.DropTable(
                name: "PEDIDO_HISTORICO");

            migrationBuilder.DropTable(
                name: "PEDIDO_ITEM");

            migrationBuilder.DropTable(
                name: "POSICAO_ATUAL");

            migrationBuilder.DropTable(
                name: "RECUSA_TROCA");

            migrationBuilder.DropTable(
                name: "RELATORIO_AMBIENTAL_ITEM");

            migrationBuilder.DropTable(
                name: "SENSOR");

            migrationBuilder.DropTable(
                name: "VERIFICACAO_QUALIDADE");

            migrationBuilder.DropTable(
                name: "VISTORIA_ALERTA");

            migrationBuilder.DropTable(
                name: "RFID");

            migrationBuilder.DropTable(
                name: "LEITURA_DORMENTE");

            migrationBuilder.DropTable(
                name: "LEITURA_OCR");

            migrationBuilder.DropTable(
                name: "TROCA");

            migrationBuilder.DropTable(
                name: "RELATORIO_AMBIENTAL");

            migrationBuilder.DropTable(
                name: "CONFERENCIA_ARMAZEM");

            migrationBuilder.DropTable(
                name: "MODO_LEITURA");

            migrationBuilder.DropTable(
                name: "MODELO_OCR");

            migrationBuilder.DropTable(
                name: "INSPECAO");

            migrationBuilder.DropTable(
                name: "LEITOR");

            migrationBuilder.DropTable(
                name: "ALERTA");

            migrationBuilder.DropTable(
                name: "SETOR");

            migrationBuilder.DropTable(
                name: "TIPO_LEITOR");

            migrationBuilder.DropTable(
                name: "DETECCAO_IMAGEM");

            migrationBuilder.DropTable(
                name: "DORMENTE");

            migrationBuilder.DropTable(
                name: "PASSAGEM");

            migrationBuilder.DropTable(
                name: "FEIXE");

            migrationBuilder.DropTable(
                name: "LOTE");

            migrationBuilder.DropTable(
                name: "PILHA");

            migrationBuilder.DropTable(
                name: "TRECHO");

            migrationBuilder.DropTable(
                name: "ESPECIE");

            migrationBuilder.DropTable(
                name: "RECEBIMENTO");

            migrationBuilder.DropTable(
                name: "TAMANHO_DORMENTE");

            migrationBuilder.DropTable(
                name: "ESTACAO");

            migrationBuilder.DropTable(
                name: "ARMAZEM");

            migrationBuilder.DropTable(
                name: "NOTA_FISCAL");

            migrationBuilder.DropTable(
                name: "PEDIDO");

            migrationBuilder.DropTable(
                name: "CONTRATO");

            migrationBuilder.DropTable(
                name: "USUARIO");

            migrationBuilder.DropTable(
                name: "EMPRESA");

            migrationBuilder.DropTable(
                name: "PESSOA");
        }
    }
}
