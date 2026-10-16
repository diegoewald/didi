# ADR-0005: Representação de valores monetários no SQLite

## Status

Aceito para a camada de persistência inicial. A política de arredondamento de cada fluxo de negócio continua sendo definida pela regra correspondente.

## Contexto

O domínio do ERP precisa representar valores monetários sem perda binária de precisão. O provider SQLite do EF Core consegue ler e gravar `decimal`, porém operações relacionais como ordenação e comparação não possuem suporte nativo equivalente e podem exigir avaliação no cliente.

Converter dinheiro para `double` simplificaria consultas, mas introduziria representação binária aproximada, inadequada para valores financeiros.

Também queremos preservar uma rota clara para PostgreSQL no futuro.

## Opções consideradas

1. `decimal` convertido para `double` no SQLite.
2. `decimal` convertido para `TEXT` canônico.
3. inteiro escalado (`INTEGER`) no SQLite.
4. adotar PostgreSQL desde a primeira instalação, eliminando o banco embarcado.

## Decisão

Para campos de valor monetário que precisem participar de filtros, ordenação, agregação ou comparação no banco, a persistência SQLite usará **inteiro assinado de 64 bits com escala fixa de 6 casas decimais**.

Exemplo conceitual:

```text
R$ 12,345678 -> 12_345_678 unidades escaladas
```

No domínio e na camada de aplicação, valores continuam sendo tratados como `decimal`/value objects apropriados. A conversão para inteiro escalado pertence à persistência.

A escala 6 foi escolhida para comportar totais monetários e preços unitários com precisão maior que centavos, cenário comum em compras, custos e preços por unidade/milhar. Ela não define a regra de arredondamento comercial de documentos; cada fluxo deverá explicitar quando e como arredondar.

Campos percentuais, quantidades e medidas não devem reutilizar automaticamente esta estratégia apenas por também possuírem casas decimais.

## Limites e invariantes

Com escala 6, `long` suporta magnitude aproximada de 9,22 trilhões de unidades monetárias antes do ponto decimal, suficiente para o escopo de um ERP desktop local. Conversões deverão usar `checked` e rejeitar overflow.

Nenhum valor deverá ser convertido para `double` no caminho de persistência monetária.

A moeda (`BRL`, `USD`, etc.) será modelada separadamente quando os fluxos multi-moeda forem implementados; não será inferida do locale da máquina.

## Migração futura para PostgreSQL

Na migração para PostgreSQL, os inteiros escalados poderão ser convertidos deterministicamente para `numeric(..., 6)` ou permanecer em representação inteira conforme a estratégia definitiva do provider. A ferramenta de migração deverá validar contagens e valores antes/depois.

## Consequências

### Positivas

- aritmética persistida exata;
- ordenação e comparação nativas sobre `INTEGER` no SQLite;
- ausência de erro binário de ponto flutuante;
- migração determinística para `numeric` em PostgreSQL.

### Negativas

- exige conversor explícito e testes de overflow/arredondamento;
- escala é parte do contrato de armazenamento;
- SQL de diagnóstico precisa considerar a escala.

## Fontes

- https://learn.microsoft.com/ef/core/providers/sqlite/limitations
- https://www.sqlite.org/datatype3.html
- https://www.sqlite.org/c3ref/int64.html
