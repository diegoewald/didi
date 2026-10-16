# ADR-0001: Stack de UI desktop

## Status

Aceito — 2026-10-15

## Contexto

ERP local para Windows, orientado a uso intensivo com teclado, mouse, tabelas e formulários.

## Opções consideradas

1. WPF em .NET 10;
2. WinUI 3 / Windows App SDK;
3. Avalonia;
4. .NET MAUI / Blazor Hybrid;
5. Electron/Tauri.

## Decisão

Usar **WPF em .NET 10 LTS** com MVVM.

## Motivos

- maturidade e estabilidade;
- integração direta com Windows;
- bom suporte para DataGrid, binding e atalhos;
- menor complexidade de distribuição que WinUI 3 unpackaged;
- produto não possui requisito multiplataforma.

## Consequências

- aplicação será Windows-only;
- UX moderna dependerá de styling próprio sem sacrificar densidade;
- migração futura de UI é possível porque regras de negócio ficam fora do projeto Desktop.
