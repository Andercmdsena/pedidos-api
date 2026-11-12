#!/usr/bin/env bash
# Crea toda la infraestructura en el Lab 13.1 (AWS Academy Cloud Developing).
# Ejecutar desde CloudShell, dentro de la carpeta del repo:   bash infra/setup-aws.sh
set -uo pipefail
export AWS_PAGER=""
REGION=us-east-1
ACC=$(aws sts get-caller-identity --query Account --output text)
BUCKET="pedidos-api-artefactos-${ACC}"
LAMBDA_ROLE="arn:aws:iam::${ACC}:role/lab7-lambda-role"
cd "$(dirname "$0")"

echo "==> 1. Bucket de artefactos (con versionado): $BUCKET"
aws s3 mb "s3://${BUCKET}" --region $REGION 2>/dev/null || echo "   (ya existía)"
aws s3api put-bucket-versioning --bucket "$BUCKET" --versioning-configuration Status=Enabled

echo "==> 2. Funciones Lambda (staging y prod)"
TMP=$(mktemp -d); echo "placeholder" > "$TMP/LEEME.txt"; (cd "$TMP" && zip -q inicial.zip LEEME.txt)
for ENV in staging prod; do
  FN="pedidos-api-${ENV}"
  if aws lambda get-function --function-name "$FN" >/dev/null 2>&1; then
    echo "   $FN ya existe"
  else
    aws lambda create-function --function-name "$FN" \
      --runtime dotnet8 --handler Pedidos.Api --role "$LAMBDA_ROLE" \
      --memory-size 512 --timeout 30 --architectures x86_64 \
      --environment "Variables={APP_ENV=${ENV}}" \
      --zip-file "fileb://$TMP/inicial.zip" --query FunctionArn --output text
    aws lambda wait function-active-v2 --function-name "$FN"
    aws lambda publish-version --function-name "$FN" --query Version --output text
  fi
  # URL pública de la función (HTTPS)
  aws lambda create-function-url-config --function-name "$FN" --auth-type NONE >/dev/null 2>&1 || true
  aws lambda add-permission --function-name "$FN" --statement-id url-publica \
    --action lambda:InvokeFunctionUrl --principal "*" --function-url-auth-type NONE >/dev/null 2>&1 || true
  aws lambda add-permission --function-name "$FN" --statement-id invoke-publica \
    --action lambda:InvokeFunction --principal "*" >/dev/null 2>&1 || true
done

echo "==> 3. AWS CodePipeline (V2) con RoleForCodepipeline"
sed -e "s/__ACCOUNT__/${ACC}/g" -e "s/__BUCKET__/${BUCKET}/g" pipeline.json > "$TMP/pipeline.json"
if aws codepipeline get-pipeline --name pedidos-api-pipeline >/dev/null 2>&1; then
  aws codepipeline update-pipeline --cli-input-json "file://$TMP/pipeline.json" --query pipeline.name --output text
else
  aws codepipeline create-pipeline --cli-input-json "file://$TMP/pipeline.json" --query pipeline.name --output text
fi

echo
echo "================ LISTO ================"
echo "Bucket:   $BUCKET"
for ENV in staging prod; do
  echo "URL $ENV: $(aws lambda get-function-url-config --function-name pedidos-api-${ENV} --query FunctionUrl --output text 2>/dev/null)"
done
echo "Pipeline: https://${REGION}.console.aws.amazon.com/codesuite/codepipeline/pipelines/pedidos-api-pipeline/view"
