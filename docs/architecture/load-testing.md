# Plano executável de carga

`backend/tests/load/catalog.js` fornece smoke, A (200 VUs), B (1.000 VUs) e rampa C (até 5.000 VUs) **apenas para catálogo**. Usa [cenários](https://grafana.com/docs/k6/latest/using-k6/scenarios/) e [thresholds](https://grafana.com/docs/k6/latest/using-k6/thresholds/) do k6. As metas p95 correspondem à resposta HTTP medida pelo gerador, que inclui rede/proxy; medir a latência da API separadamente para comparar com as metas internas.

Exemplo PowerShell: `docker run --rm -i -e MODE=smoke -e BASE_URL=http://host.docker.internal:3000 -v "${PWD}/backend/tests/load:/scripts:ro" grafana/k6 run /scripts/catalog.js`. Execute A/B/C apenas em ambiente isolado, com dataset representativo e limites monitorados. Defina `QUERY`, `CATEGORY` e `BRAND` com termos existentes; o default não é representativo. O limitador por IP atual (120/min por instância) pode fazer um gerador único parecer saturado por 429 antes da aplicação; documente origem dos VUs e política de rate limit no relatório. Não desabilite proteção silenciosamente.

Ainda faltam os cenários de pedidos e D. Para D, reinicie uma API, interrompa uma réplica, Redis ou broker somente depois de adotados, e simule lentidão de gateway com fake controlado. Registre resultados de pedidos, pagamentos e estoque por identificador. Não aplique injeção de falhas aos containers de desenvolvimento existentes ou à produção sem janela planejada.

Cada relatório deve conter: commit, imagens, hardware, número de instâncias, pool, dataset (contas, produtos, aplicações, carrinhos), distribuição de filtros, rede, rampa/duração, RPS, p50/p95/p99 por rota, taxa de 429/5xx, CPU/RAM, conexões/locks do PostgreSQL e ponto de saturação. Nenhum resultado de capacidade foi medido nesta etapa.

O endpoint de facetas e os filtros de preço/disponibilidade foram verificados funcionalmente com PostgreSQL descartável, mas não receberam `EXPLAIN` nem carga representativa. O script k6 atual mede a listagem, não as facetas. Incluir mistura real de facetas e páginas de produto antes de decidir índices ou cache.

## Smoke exploratório de 16/11/2026

Executado contra os containers de desenvolvimento **já em execução**, sem reconstruí-los com as mudanças deste trabalho: 5 VUs por 30 s, `QUERY=filtro` padrão, sem dataset controlado. Foram 75 requisições, 0 erros, p95 de catálogo simples 17,69 ms e p95 de busca 511,02 ms (threshold de 500 ms falhou). A amostra é pequena e mistura aquecimento/ambiente local; não prova capacidade nem identifica causa. Próximo passo: repetir com dataset e termo representativos, capturar planos `EXPLAIN (ANALYZE, BUFFERS)` e métricas do banco antes de alterar índices.

## Verificação isolada de failover de processo

O overlay `compose.verify.yaml` foi usado com projeto Compose `brasil57-verify`, banco e volumes descartáveis e imagens locais construídas nesta execução. A migration única terminou antes das duas APIs; readiness e catálogo retornaram 200. Após parar `api-1` e esperar 12 s pelos health checks do Caddy, ambos continuaram retornando 200 pela outra API. A instância parada foi religada e o stack descartável foi removido. O 503 observado imediatamente após a primeira subida cessou quando ambas as APIs passaram a responder readiness; não foi medido tempo exato de recuperação nem taxa de erros durante a transição. Isso verifica falha de **processo no mesmo host**, não HA entre hosts.

Para repetir, construa `brasil57-api:validation` e `brasil57-worker:validation` com os respectivos Dockerfiles, disponha da imagem local `57brasil-storefront:latest`, defina `POSTGRES_PASSWORD` de teste e use `docker compose -p brasil57-verify -f compose.yaml -f compose.verify.yaml up -d --no-build`. O overlay publica `localhost:18088`. Remova apenas esse projeto descartável com `docker compose -p brasil57-verify -f compose.yaml -f compose.verify.yaml down --volumes` após a verificação.
