# Smoke WPF em Windows

Este documento descreve o smoke automatizado mínimo do Desktop WPF. Ele existe para detectar regressões de inicialização, composição de dependências, bootstrap/autenticação, abertura do shell e navegação básica antes que uma mudança chegue ao smoke manual.

## Execução

No diretório `local-erp`, depois de compilar a solução em Windows:

```powershell
./scripts/smoke-wpf.ps1 -Configuration Release
```

O CI permanente executa o mesmo script em Windows Server 2025 depois do build e da suíte automatizada.

## Isolamento

O script cria um diretório temporário exclusivo e define `LOCALERP_DATA_ROOT` somente para o processo de smoke. `AppDataPaths.CreateDefault()` respeita esse override explícito; quando a variável não existe ou está vazia, o aplicativo continua usando o diretório normal em `%LOCALAPPDATA%\LocalErp`.

O banco do smoke é criado no diretório temporário, recebe as migrations reais e é removido no `finally`. Nenhum banco ou arquivo normal do usuário deve ser reutilizado por esse fluxo.

## Fluxo coberto

O smoke usa Windows UI Automation para validar, em sequência:

1. abertura da configuração inicial;
2. criação do primeiro administrador;
3. autenticação automática após o bootstrap e abertura da `MainWindow`;
4. entrada no módulo `Compras`;
5. seleção das abas `Solicitações`, `Cotações` e `Pedidos`;
6. logout e abertura da janela de login;
7. login explícito com a conta criada;
8. retorno à `MainWindow`;
9. segundo logout e encerramento pelo cancelamento da janela de login;
10. ausência de entrada fatal (`[FTL]`) no log e presença das rotas de startup esperadas.

Se o harness falhar, ele imprime as janelas WPF visíveis e o log do aplicativo antes de limpar o diretório temporário.

## O que este gate não valida

Este smoke não é uma suíte end-to-end completa de Compras e não substitui validação humana. Permanecem manuais antes de release:

- criação e edição real de Solicitação, RFQ, respostas e Pedido de Compra;
- Comparação de Cotações e Adjudicação com dados completos;
- estados de loading/erro/vazio e permissões insuficientes em todas as telas;
- foco, teclado, Enter/Escape, fechamento durante mutação, DPI, escala e ergonomia;
- aparência em hardware/Windows representativo;
- diálogo fatal em build de diagnóstico;
- benchmark de senha em hardware alvo.

A automação deve crescer somente quando o custo de manutenção se justificar e sem mover regras de negócio para o harness de UI.
