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

# Function to create table if it doesn't exist
create_table_if_not_exists() {
    TABLE_NAME=$1
    ATTR_DEFS=$2
    KEY_SCHEMA=$3
    TTL_ATTR=$4

    echo "Checking table $TABLE_NAME..."
    if aws dynamodb describe-table --table-name "$TABLE_NAME" --endpoint-url "$ENDPOINT_URL" --region "$REGION" > /dev/null 2>&1; then
        echo "  [SKIP] Table '$TABLE_NAME' already exists."
    else
        echo "  [CREATE] Table '$TABLE_NAME'..."
        aws dynamodb create-table \
            --table-name "$TABLE_NAME" \
            --attribute-definitions $ATTR_DEFS \
            --key-schema $KEY_SCHEMA \
            --billing-mode PAY_PER_REQUEST \
            --endpoint-url "$ENDPOINT_URL" \
            --region "$REGION"
        
        echo "  [UPDATE] Enabling TTL for '$TABLE_NAME' on attribute '$TTL_ATTR'..."
        aws dynamodb update-time-to-live \
            --table-name "$TABLE_NAME" \
            --time-to-live-specification "Enabled=true, AttributeName=$TTL_ATTR" \
            --endpoint-url "$ENDPOINT_URL" \
            --region "$REGION"
    fi
}

# inventory-event-logs
create_table_if_not_exists "inventory-event-logs" \
    "AttributeName=EventType,AttributeType=S AttributeName=MessageId,AttributeType=S" \
    "AttributeName=EventType,KeyType=HASH AttributeName=MessageId,KeyType=RANGE" \
    "TTL"

# inventoryalert-company-news
create_table_if_not_exists "inventoryalert-company-news" \
    "AttributeName=PK,AttributeType=S AttributeName=SK,AttributeType=S" \
    "AttributeName=PK,KeyType=HASH AttributeName=SK,KeyType=RANGE" \
    "TTL"

# inventoryalert-market-news
create_table_if_not_exists "inventoryalert-market-news" \
    "AttributeName=PK,AttributeType=S AttributeName=SK,AttributeType=S" \
    "AttributeName=PK,KeyType=HASH AttributeName=SK,KeyType=RANGE" \
    "Ttl"

# inventory-price-history
create_table_if_not_exists "inventory-price-history" \
    "AttributeName=TickerSymbol,AttributeType=S AttributeName=Timestamp,AttributeType=S" \
    "AttributeName=TickerSymbol,KeyType=HASH AttributeName=Timestamp,KeyType=RANGE" \
    "Ttl"

# inventory-recommendations
create_table_if_not_exists "inventory-recommendations" \
    "AttributeName=Symbol,AttributeType=S AttributeName=Period,AttributeType=S" \
    "AttributeName=Symbol,KeyType=HASH AttributeName=Period,KeyType=RANGE" \
    "Ttl"

# inventory-earnings
create_table_if_not_exists "inventory-earnings" \
    "AttributeName=Symbol,AttributeType=S AttributeName=Period,AttributeType=S" \
    "AttributeName=Symbol,KeyType=HASH AttributeName=Period,KeyType=RANGE" \
    "Ttl"

echo "=== Combined Init Complete ==="
exit 0
