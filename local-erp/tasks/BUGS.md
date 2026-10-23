# Bugs

Nenhum bug funcional confirmado no baseline atual.

## Validação atual

O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O baseline técnico atual concluiu com sucesso build Release, build da ferramenta de benchmark, **271/271 testes** (Domain 75, Application 130, Persistence 59, Infrastructure 7), smoke WPF automatizado, verificação de snapshot EF e ciclo apply -> rollback 0 -> reapply das **17 migrations**.

O smoke WPF automatizado usa banco e diretório de dados descartáveis e validou bootstrap do primeiro administrador, autenticação automática pós-bootstrap, abertura da `MainWindow`, navegação pelas abas `Solicitações`, `Cotações` e `Pedidos`, logout, login explícito, retorno ao shell e encerramento sem log fatal. O CI técnico #199 validou esse fluxo; o smoke visual/interativo manual completo continua obrigatório antes de release.

Os cinco slices atuais de Compras — Solicitação, RFQ, Comparação, Adjudicação e Pedido de Compra — permanecem implementados sem introduzir aprovação, recebimento, estoque, financeiro, fiscal ou integrações externas por suposição.

Categoria, Unidade de Medida, Produto, Material, Centro de Custo, Empresa, Filial e Fornecedor permanecem integrados com suas permissões, persistência e interfaces já validadas. Nenhum vínculo automático não especificado entre os cadastros foi introduzido.

As salvaguardas de segurança anteriores permanecem cobertas: preservação da última capacidade efetiva de `roles.manage`, rollback conjunto da auditoria administrativa e compare-and-set de credencial na troca própria de senha.

O aviso conhecido no rollback de `AddAuthenticationThrottling` sobre `PRAGMA foreign_keys = 0` permanece dívida de migration já registrada. O rollback completa com sucesso; portanto, neste momento, ele não é classificado como bug funcional confirmado.

Pendências de smoke visual WPF completo, validação do diálogo fatal em build de diagnóstico e benchmark em hardware alvo são validações manuais/ambientais, não bugs confirmados.
