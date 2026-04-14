#!/bin/sh
set -e

# AWS_ENDPOINT_URL is injected via docker-compose environment
ACCOUNT_ID="123456789012"
REGION="us-east-1"
ENDPOINT_URL="${AWS_ENDPOINT_URL:-http://moto:5000}"

echo "=== InventoryAlert Moto Init (Combined) ==="

# Wait for Moto to be responsive
echo "Waiting for Moto at $ENDPOINT_URL..."
for i in $(seq 1 30); do
    if aws sqs list-queues --endpoint-url "$ENDPOINT_URL" --region "$REGION" > /dev/null 2>&1; then
        echo "Moto is ready!"
        break
    fi
    echo "Moto not ready yet (attempt $i/30)..."
    sleep 2
done

# --------------------------------------------------
# 1. SQS initialization
# --------------------------------------------------
echo "Initializing SQS..."

# Dead Letter Queue
echo "Checking DLQ..."
if aws sqs get-queue-url --queue-name inventory-event-dlq --endpoint-url "$ENDPOINT_URL" > /dev/null 2>&1; then
    echo "  [SKIP] inventory-event-dlq already exists."
    DLQ_URL=$(aws sqs get-queue-url --queue-name inventory-event-dlq --endpoint-url "$ENDPOINT_URL" --query 'QueueUrl' --output text)
else
    echo "  [CREATE] inventory-event-dlq..."
    aws sqs create-queue \
        --queue-name inventory-event-dlq \
        --attributes '{"ReceiveMessageWaitTimeSeconds":"1","VisibilityTimeout":"30"}' \
        --endpoint-url "$ENDPOINT_URL"
    DLQ_URL=$(aws sqs get-queue-url --queue-name inventory-event-dlq --endpoint-url "$ENDPOINT_URL" --query 'QueueUrl' --output text)
fi

DLQ_ARN=$(aws sqs get-queue-attributes \
    --queue-url "$DLQ_URL" \
    --attribute-names QueueArn \
    --query 'Attributes.QueueArn' \
    --output text \
    --endpoint-url "$ENDPOINT_URL")
echo "  DLQ ARN: $DLQ_ARN"

# Main queue
echo "Checking main event-queue..."
if aws sqs get-queue-url --queue-name event-queue --endpoint-url "$ENDPOINT_URL" > /dev/null 2>&1; then
    echo "  [SKIP] event-queue already exists."
else
    echo "  [CREATE] event-queue..."
    # Explicit JSON attributes for redrive policy to be robust
    aws sqs create-queue \
        --queue-name event-queue \
        --attributes "{\"VisibilityTimeout\":\"30\",\"ReceiveMessageWaitTimeSeconds\":\"5\",\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"$DLQ_ARN\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\"}" \
        --endpoint-url "$ENDPOINT_URL"
fi
QUEUE_ARN="arn:aws:sqs:${REGION}:${ACCOUNT_ID}:event-queue"

# --------------------------------------------------
# 2. SNS initialization
# --------------------------------------------------
echo "Initializing SNS..."
TOPIC_ARN=$(aws sns list-topics \
    --query "Topics[?ends_with(TopicArn, ':inventory-events')].TopicArn" \
    --output text \
    --endpoint-url "$ENDPOINT_URL")

if [ -n "$TOPIC_ARN" ] && [ "$TOPIC_ARN" != "None" ]; then
    echo "  [SKIP] inventory-events topic already exists: $TOPIC_ARN"
else
    echo "  [CREATE] inventory-events topic..."
    TOPIC_ARN=$(aws sns create-topic \
        --name inventory-events \
        --query TopicArn \
        --output text \
        --endpoint-url "$ENDPOINT_URL")
    echo "  Topic ARN: $TOPIC_ARN"

    sleep 1

    echo "  Subscribing event-queue to inventory-events..."
    aws sns subscribe \
        --topic-arn "$TOPIC_ARN" \
        --protocol sqs \
        --notification-endpoint "$QUEUE_ARN" \
        --endpoint-url "$ENDPOINT_URL"
fi

# --------------------------------------------------
# 3. DynamoDB initialization
# --------------------------------------------------
echo "Initializing DynamoDB..."
# Create table (ignore error if already exists)
aws dynamodb create-table \
    --table-name inventory-event-logs \
    --attribute-definitions \
        AttributeName=EventType,AttributeType=S \
        AttributeName=MessageId,AttributeType=S \
    --key-schema \
    AttributeName=EventType,KeyType=HASH \
    AttributeName=MessageId,KeyType=RANGE \
    --billing-mode PAY_PER_REQUEST \
    --endpoint-url "$ENDPOINT_URL" \
    --region "$REGION" || echo "Table 'inventory-event-logs' might already exist"

aws dynamodb update-time-to-live \
    --table-name inventory-event-logs \
    --time-to-live-specification "Enabled=true, AttributeName=TTL" \
    --endpoint-url "$ENDPOINT_URL" \
    --region "$REGION" || echo "TTL for 'inventory-event-logs' might already be enabled"

# Create news table
aws dynamodb create-table \
    --table-name inventoryalert-company-news \
    --attribute-definitions \
        AttributeName=PK,AttributeType=S \
        AttributeName=SK,AttributeType=S \
    --key-schema \
    AttributeName=PK,KeyType=HASH \
    AttributeName=SK,KeyType=RANGE \
    --billing-mode PAY_PER_REQUEST \
    --endpoint-url "$ENDPOINT_URL" \
    --region "$REGION" || echo "Table 'inventoryalert-company-news' might already exist"

aws dynamodb update-time-to-live \
    --table-name inventoryalert-company-news \
    --time-to-live-specification "Enabled=true, AttributeName=TTL" \
    --endpoint-url "$ENDPOINT_URL" \
    --region "$REGION" || echo "TTL for 'inventoryalert-company-news' might already be enabled"

# inventoryalert-market-news
aws dynamodb create-table \
  --table-name inventoryalert-market-news \
  --attribute-definitions \
    AttributeName=PK,AttributeType=S \
    AttributeName=SK,AttributeType=S \
  --key-schema \
    AttributeName=PK,KeyType=HASH \
    AttributeName=SK,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$REGION" || echo "Table 'inventoryalert-market-news' might already exist"

aws dynamodb update-time-to-live \
  --table-name inventoryalert-market-news \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$REGION" || echo "TTL for 'inventoryalert-market-news' might already be enabled"

# inventory-price-history
aws dynamodb create-table \
  --table-name inventory-price-history \
  --attribute-definitions \
    AttributeName=TickerSymbol,AttributeType=S \
    AttributeName=Timestamp,AttributeType=S \
  --key-schema \
    AttributeName=TickerSymbol,KeyType=HASH \
    AttributeName=Timestamp,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$REGION" || echo "Table 'inventory-price-history' might already exist"

aws dynamodb update-time-to-live \
  --table-name inventory-price-history \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$REGION" || echo "TTL for 'inventory-price-history' might already be enabled"

# inventory-recommendations
aws dynamodb create-table \
  --table-name inventory-recommendations \
  --attribute-definitions \
    AttributeName=Symbol,AttributeType=S \
    AttributeName=Period,AttributeType=S \
  --key-schema \
    AttributeName=Symbol,KeyType=HASH \
    AttributeName=Period,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$REGION" || echo "Table 'inventory-recommendations' might already exist"

aws dynamodb update-time-to-live \
  --table-name inventory-recommendations \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$REGION" || echo "TTL for 'inventory-recommendations' might already be enabled"

# inventory-earnings
aws dynamodb create-table \
  --table-name inventory-earnings \
  --attribute-definitions \
    AttributeName=Symbol,AttributeType=S \
    AttributeName=Period,AttributeType=S \
  --key-schema \
    AttributeName=Symbol,KeyType=HASH \
    AttributeName=Period,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$REGION" || echo "Table 'inventory-earnings' might already exist"

aws dynamodb update-time-to-live \
  --table-name inventory-earnings \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$REGION" || echo "TTL for 'inventory-earnings' might already be enabled"

echo "=== Combined Init Complete ==="
exit 0
