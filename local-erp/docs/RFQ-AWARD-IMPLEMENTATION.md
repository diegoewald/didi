# Implementação da adjudicação de RFQ

Status em 2026-10-22: backend e persistência validados; UI WPF implementada e aguardando validação completa do CI.

Baseline já observado no CI #179: 253/253 testes (Domain 69, Application 124, Persistence 54, Infrastructure 6), build Release sem warnings/erros, snapshot EF sem mudanças pendentes e 16 migrations em apply -> rollback 0 -> reapply.

A UI em validação mantém as regras de `docs/RFQ-AWARD-SPEC.md`: adjudicação manual apenas em RFQ `Closed`, uma oferta finalizada por linha inteira, linhas opcionais sem adjudicação, nenhuma recomendação/ranking automático, nenhuma conversão cambial e nenhum Pedido de Compra gerado por este slice.
