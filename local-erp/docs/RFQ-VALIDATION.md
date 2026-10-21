# RFQ — registro de revalidação

Data: 2026-10-21.

O segundo slice de Compras já contém especificação, Domain/Application, persistência EF com a migration `20261021132811_AddRequestForQuotations`, testes de persistência, lookups e workspace WPF integrado ao shell.

O CI permanente #159 falhou no `Build Release` exclusivamente pela regra CA1859 em `RequestForQuotationsWorkspaceViewModel`, antes da execução da suíte. O commit `9198316983218399588e7c874ee3ef8eb3c4c6a5` altera o campo de referências de fornecedor para o tipo concreto `Dictionary<Guid, RequestForQuotationSupplierOption>`, conforme o analyzer.

Este registro acompanha o commit de usuário usado para disparar a revalidação completa do código corrigido no CI permanente. O resultado final deve ser registrado no roadmap/backlog somente depois de observado no runner.
