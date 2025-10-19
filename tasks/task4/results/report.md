# Task 4: CI/CD для booking-service с Helm и Minikube

## Автор
Швецов Александр  
**Дата:** 19.10.2025

## Реализация
- **booking-service**: Go-сервис на порту 8080 (/ping → "pong", /feature если `ENABLE_FEATURE_X=true`). Dockerfile: Go build, curl (для probes), check-dns.sh.
- **Helm-чарт**: Deployment (liveness/readiness probes на /ping, env для flag, resources requests/limits), Service (ClusterIP, порт 80 → 8080). 
  - [values-staging.yaml](values-staging.yaml): 1 реплика, низкие ресурсы `ENABLE_FEATURE_X=false`.
  - [values-prod.yaml](values-prod.yaml): 3 реплики, высокие ресурсы, `ENABLE_FEATURE_X=true`.
- **CI/CD** (.gitlab-ci.yml): Стадии build (Docker), test (run container + curl /ping+/feature), deploy (Minikube image load + Helm upgrade staging/prod), tag (git tag с timestamp).
  - Локальная симуляция: `gitlab-ci-local` (лог: [gitlab-ci-local_b_t_d_t.txt](gitlab-ci-local_b_t_d_t.txt)).
- **Проверки**:
  - [check-dns.sh](check_dns.txt): In-cluster curl `http://booking-staging:80/ping` → "✅ Success".
  - [check-status.sh](check_pods.txt, check_service.txt): kubectl get pods/svc + Helm list.
- **Makefile**: Targets для ci (gitlab-ci-local), build, deploy, test-dns/status.

## Тесты
- **CI**: `make ci` → Build/test/deploy/tag OK ([ci-log.txt](gitlab-ci-local_b_t_d_t.txt)).
- **DNS**: `./check-dns.sh` → "✅ Success" ([check_dns.txt](check_dns.txt)).
- **Status**: `./check-status.sh` → Deployments/Services/Helm OK ([check_pods.txt](check_pods.txt), [check_service.txt](check_service.txt)).
- **Feature flag**:
  - Staging: `/feature` 404 (flag=false).
  - Prod: Port-forward `kubectl port-forward svc/booking-prod 8080:80`, curl → "Feature X is enabled!" ([feature-enabled.txt](feature-enabled.txt)).
- **Probes**: Liveness/readiness на /ping ([check_describe_pod.txt](check_describe_pod.txt)).

## Результаты
- **[ci-log.txt](gitlab-ci-local_b_t_d_t.txt)**: Логи gitlab-ci-local (build/test/deploy/tag).
- **[kubectl-get.txt](kubectl-get.txt)**: `kubectl get pods,svc`.
- **[feature-enabled.txt](feature-enabled.txt)**: Curl /feature (prod).
- **[check_dns.txt](check_dns.txt)**: DNS test output.
- **[values-staging.yaml](values-staging.yaml)**, **[values-prod.yaml](values-prod.yaml)**: Helm configs.

## Структура results/task4/
- ci-log.txt
- kubectl-get.txt
- feature-enabled.txt
- check_dns.txt
- check_pods.txt
- check_service.txt
- check_describe_pod.txt
- values-staging.yaml
- values-prod.yaml
- report.md

