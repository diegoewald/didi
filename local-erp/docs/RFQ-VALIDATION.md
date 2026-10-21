# RFQ — registro de revalidação

Data: 2026-10-21.

O segundo slice de Compras já contém especificação, Domain/Application, persistência EF com a migration `20261021132811_AddRequestForQuotations`, testes de persistência, lookups e workspace WPF integrado ao shell.

O CI permanente #159 falhou no `Build Release` exclusivamente pela regra CA1859 em `RequestForQuotationsWorkspaceViewModel`, antes da execução da suíte. O commit `9198316983218399588e7c874ee3ef8eb3c4c6a5` altera o campo de referências de fornecedor para o tipo concreto `Dictionary<Guid, RequestForQuotationSupplierOption>`, conforme o analyzer.

Este registro acompanha o commit de usuário usado para disparar a revalidação completa do código corrigido no CI permanente. O resultado final deve ser registrado no roadmap/backlog somente depois de observado no runner.

## Resultado final — CI #160

A revalidação completa terminou com sucesso no run `39622565165` sobre o commit `fdf715308533211014c51f8ce6564f095512d3ce`:

- Build Release: 0 warnings / 0 errors;
- benchmark tool: 0 warnings / 0 errors;
- Domain: 63/63;
- Application: 115/115;
- Persistence: 51/51;
- Infrastructure: 6/6;
- total: 235/235 testes;
- snapshot EF sem mudanças pendentes;
- 15 migrations aplicadas, revertidas a zero e reaplicadas com sucesso;
- migration RFQ validada: `20261021132811_AddRequestForQuotations`.

O aviso histórico de `PRAGMA foreign_keys = 0` ao reverter `AddAuthenticationThrottling` continua conhecido e não bloqueante; deve ser tratado antes do release sem reescrever migrations antigas de forma insegura.
