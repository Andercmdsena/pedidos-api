# Plataforma de Gestión de Pedidos – API REST (.NET 8) con CI/CD en AWS

## Estructura

```
src/Pedidos.Api/            API (Controllers → Services → Repositories)
tests/Pedidos.Api.Tests/    Pruebas unitarias xUnit
buildspec.yml               Definición de build para AWS CodeBuild
infra/setup-aws.sh          Crea bucket S3, Lambdas (staging/prod) con URL pública y el CodePipeline
infra/pipeline.json         Definición del CodePipeline V2
.github/workflows/pipeline.yml  CI en GitHub Actions + entrega a CodePipeline
pruebas.http                Peticiones de ejemplo
```

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/` y `/health` | Estado, versión y entorno |
| GET | `/swagger` | Documentación interactiva |
| POST | `/api/pedidos` | Crear pedido (queda `Pendiente`) |
| GET | `/api/pedidos` | Listar. Filtros: `estado`, `cliente`, `desde`, `hasta`. Orden: `ordenarPor=fecha\|estado\|total\|cliente`, `direccion=asc\|desc` |
| GET | `/api/pedidos/{id}` | Obtener un pedido |
| PATCH | `/api/pedidos/{id}/estado` | Avanzar estado: `Pendiente → EnProceso → Enviado → Entregado` (saltar etapas devuelve 409) |

## Correr en local

```bash
dotnet test tests/Pedidos.Api.Tests/Pedidos.Api.Tests.csproj
dotnet run --project src/Pedidos.Api      # http://localhost:5000/swagger
```

## Flujo DevOps

```
git push (main)
   └─► GitHub Actions: restore → build → test (xUnit) → publish → zip
          └─► S3 (pedidos-api.zip, versionado) + start-pipeline-execution
                 └─► AWS CodePipeline V2 (RoleForCodepipeline)
                        Source (S3) → Deploy Lambda staging → Aprobación manual → Deploy Lambda producción
                                                     Logs en CloudWatch
```

`buildspec.yml` deja lista la etapa de build para AWS CodeBuild: en AWS Academy ningún rol
confía en `codebuild.amazonaws.com` y no se pueden crear roles, por eso el build corre en GitHub Actions.
