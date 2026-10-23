# Pacote Windows de pré-release

## Objetivo

O script `scripts/package-release.ps1` gera um pacote portátil para QA e smoke manual em Windows real.

Ele **não é o instalador de produção**. O instalador/updater final continuará seguindo `docs/INSTALLATION.md`, `docs/UPDATE-STRATEGY.md` e o ADR-0003, com Velopack, assinatura de código e fluxo de atualização seguro quando essa fase for implementada.

## Conteúdo

O pacote `LocalErp-<versão>-<runtime>.zip` contém:

- `app/`: publicação WPF self-contained para o RID escolhido;
- `database/LocalErp.DatabaseMigrator.exe`: migration bundle self-contained da mesma release;
- `database/Initialize-FreshDatabase.ps1`: inicialização segura **somente para banco novo**;
- `release-info.json`: versão, runtime, configuração, commit e tipo do pacote;
- `SHA256SUMS.txt`: SHA-256 de todos os arquivos internos;
- `README.md`: esta orientação.

Ao lado do ZIP é gerado `LocalErp-<versão>-<runtime>.zip.sha256`.

## Gerar

No PowerShell, a partir da raiz `local-erp`:

```powershell
./scripts/package-release.ps1 -Configuration Release -Runtime win-x64
```

A versão padrão vem de `VersionPrefix` em `Directory.Build.props`.

Saída padrão:

```text
artifacts/release/
  LocalErp-0.1.0-win-x64/
  LocalErp-0.1.0-win-x64.zip
  LocalErp-0.1.0-win-x64.zip.sha256
```

## Primeira execução em uma máquina limpa

1. Extraia o ZIP.
2. Execute `database/Initialize-FreshDatabase.ps1` no PowerShell.
3. Confirme que `%LOCALAPPDATA%\LocalErp\Data\localerp.db` foi criado.
4. Inicie `app/LocalErp.Desktop.exe`.
5. Faça o bootstrap do primeiro administrador.

Para isolar um ambiente de QA, defina `LOCALERP_DATA_ROOT` antes de inicializar e abrir o ERP.

Exemplo:

```powershell
$env:LOCALERP_DATA_ROOT = 'C:\LocalErp-QA'
./database/Initialize-FreshDatabase.ps1
./app/LocalErp.Desktop.exe
```

## Proteção de dados existentes

`Initialize-FreshDatabase.ps1` recusa operar quando `localerp.db` já existe. Isso é intencional.

Uma atualização de banco existente deve seguir o fluxo definido no ADR-0006: backup validado, migration bundle versionado, health check e recuperação planejada. O pacote portátil atual não deve ser apresentado como mecanismo de upgrade de produção.

## Integridade

Antes de usar o pacote fora do CI, compare o SHA-256 do ZIP com o arquivo `.zip.sha256`. O manifesto `SHA256SUMS.txt` permite verificar também os arquivos internos após a extração.

## Limites atuais

- pacote sem assinatura de código;
- sem instalador Velopack;
- sem atualização automática;
- sem backup/orquestração de upgrade de banco existente;
- destinado a QA, smoke manual e validação pré-release.

Um release empresarial final só deve ser publicado depois da implementação e validação dos itens acima.
