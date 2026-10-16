# Arquitetura

## Princípios

- local-first;
- domínio sem dependência da UI ou do banco;
- regras de negócio na camada de aplicação/domínio;
- persistência isolada;
- UI WPF com MVVM;
- autorização não pode existir apenas visualmente;
- dados do usuário fora do diretório de instalação;
- evolução incremental, sem framework interno desnecessário.

## Projetos

### LocalErp.Domain

Entidades, value objects, invariantes e regras de domínio puras.

Não referencia EF Core, WPF, logging ou filesystem.

### LocalErp.Application

Casos de uso e contratos necessários para executar regras do sistema.

Pode referenciar Domain. Não conhece WPF nem detalhes de SQLite.

### LocalErp.Infrastructure

Implementações técnicas de contratos da Application que não pertencem à persistência: clock, paths locais, filesystem, integrações futuras.

### LocalErp.Persistence

EF Core, SQLite, mappings, migrations e serviços de persistência.

A UI não deve executar SQL nem manipular `DbContext` diretamente.

### LocalErp.Desktop

Shell WPF, Views, ViewModels, composição de dependências e experiência desktop.

## Dependências permitidas

```text
Desktop ──> Application
Desktop ──> Infrastructure
Desktop ──> Persistence
Infrastructure ──> Application
Persistence ──> Application
Persistence ──> Domain
Application ──> Domain
Domain ──> (nenhum projeto interno)
```

Dependências inversas serão introduzidas por interfaces quando houver um consumidor real.

## Dados locais

Diretório padrão:

```text
%LocalAppData%\LocalErp\
  Data\localerp.db
  Logs\
  Backups\
  Attachments\
```

O diretório de instalação é tratado como descartável/substituível durante atualização.

## Concorrência e rede

Fase single-PC: SQLite local.

Fase futura multi-PC: migrar dados para PostgreSQL em servidor local. Não será permitido usar SQLite em uma pasta compartilhada por vários clientes.

A migração futura deverá preservar os contratos de Application e Domain; mudanças provider-specific ficam em Persistence.

## Auditoria

Ainda não implementada. O desenho futuro deve capturar usuário, ação, timestamp UTC, entidade, registro, valor anterior/novo e origem. A inclusão será feita antes dos módulos operacionais entrarem em produção.

## Data/hora

Persistir timestamps técnicos em UTC. Conversão para horário local pertence à borda/UI.
