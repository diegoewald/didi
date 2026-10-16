# ADR-0003: Instalação e atualização

## Status

Aceito como direção inicial — 2026-10-15

## Contexto

Aplicação Windows precisa ser instalável, atualizável e preservar dados locais.

## Opções consideradas

1. Velopack;
2. MSIX + App Installer;
3. WiX puro;
4. Inno Setup.

## Decisão

Adotar **Velopack** na fase de distribuição, com publicação self-contained. Para ambientes per-machine, avaliar MSI do próprio fluxo Velopack/WiX.

## Motivos

- suporte explícito a WPF/.NET;
- updater integrado;
- setup simples;
- possibilidade de MSI;
- fluxo de update controlável pela aplicação.

## Consequências

- requer disciplina de code signing e hospedagem de releases;
- dados devem ficar fora da pasta substituída pelo updater;
- MSIX pode ser reavaliado se um cliente exigir gerenciamento corporativo específico.
