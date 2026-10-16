# Instalação

## Direção escolhida

Velopack é a opção inicial para empacotamento/instalação devido à integração com WPF/.NET e ao mecanismo de atualização.

A decisão de distribuição final permanece condicionada aos requisitos reais de TI:

- per-user sem elevação: Setup do Velopack;
- per-machine/Group Policy: MSI gerado via WiX/Velopack;
- ambiente gerenciado com package identity/Intune/Store: reconsiderar MSIX.

## Regra de dados

Instalador e desinstalador não devem remover automaticamente:

- banco;
- anexos;
- backups;
- configurações persistentes importantes.

Binários e dados permanecem separados.

## Assinatura

Release de produção deverá ser assinada com certificado adequado. Não publicar instalador final não assinado como se fosse versão pronta para uso empresarial.
