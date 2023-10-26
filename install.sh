helm repo add grafana https://grafana.github.io/helm-charts &&
  helm repo update &&
  helm upgrade --install grafana-k8s-monitoring grafana/k8s-monitoring \
    --namespace "grafana" --create-namespace --values - <<EOF
cluster:
  name: kind
externalServices:
  prometheus:
    host: https://prometheus-prod-24-prod-eu-west-2.grafana.net
    basicAuth:
      username: "1251691"
      password: glc_eyJvIjoiOTczNTUyIiwibiI6ImtpbmQtZ3JhZmFuYSIsImsiOiJKMDl3ZFcycFU4UXE3Mm5qcTU3MDV5clAiLCJtIjp7InIiOiJwcm9kLWV1LXdlc3QtMiJ9fQ==
  loki:
    host: https://logs-prod-012.grafana.net
    basicAuth:
      username: "724891"
      password: glc_eyJvIjoiOTczNTUyIiwibiI6ImtpbmQtZ3JhZmFuYSIsImsiOiJKMDl3ZFcycFU4UXE3Mm5qcTU3MDV5clAiLCJtIjp7InIiOiJwcm9kLWV1LXdlc3QtMiJ9fQ==
opencost:
  opencost:
    exporter:
      defaultClusterId: kind
    prometheus:
      external:
        url: https://prometheus-prod-24-prod-eu-west-2.grafana.net/api/prom
EOF