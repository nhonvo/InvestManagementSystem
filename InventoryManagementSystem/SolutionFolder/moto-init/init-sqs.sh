#!/bin/bash
set -ex

# AWS_ENDPOINT_URL is injected via docker-compose environment
ACCOUNT_ID="123456789012"
REGION="us-east-1"

echo "=== InventoryAlert Moto Init ==="

# --------------------------------------------------
# 1. Dead Letter Queue for event-queue
# --------------------------------------------------
echo "Checking DLQ..."
if aws sqs get-queue-url --queue-name inventory-event-dlq --endpoint-url "$AWS_ENDPOINT_URL" > /dev/null 2>&1; then
    echo "  [SKIP] inventory-event-dlq already exists."
    DLQ_URL=$(aws sqs get-queue-url --queue-name inventory-event-dlq --endpoint-url "$AWS_ENDPOINT_URL" --query 'QueueUrl' --output text)
else
    echo "  [CREATE] inventory-event-dlq..."
    aws sqs create-queue \
        --queue-name inventory-event-dlq \
        --attributes '{"ReceiveMessageWaitTimeSeconds":"1","VisibilityTimeout":"30"}' \
        --endpoint-url "$AWS_ENDPOINT_URL"
    DLQ_URL=$(aws sqs get-queue-url --queue-name inventory-event-dlq --endpoint-url "$AWS_ENDPOINT_URL" --query 'QueueUrl' --output text)
fi

DLQ_ARN=$(aws sqs get-queue-attributes \
    --queue-url "$DLQ_URL" \
    --attribute-names QueueArn \
    --query 'Attributes.QueueArn' \
    --output text \
    --endpoint-url "$AWS_ENDPOINT_URL")
echo "  DLQ ARN: $DLQ_ARN"

# --------------------------------------------------
# 2. Main event queue (with DLQ redrive policy)
# --------------------------------------------------
echo "Checking main event-queue..."
if aws sqs get-queue-url --queue-name event-queue --endpoint-url "$AWS_ENDPOINT_URL" > /dev/null 2>&1; then
    echo "  [SKIP] event-queue already exists."
else
    echo "  [CREATE] event-queue..."
    aws sqs create-queue \
        --queue-name event-queue \
        --attributes "{\"VisibilityTimeout\":\"30\",\"ReceiveMessageWaitTimeSeconds\":\"5\",\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"$DLQ_ARN\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\"}" \
        --endpoint-url "$AWS_ENDPOINT_URL"
fi
QUEUE_ARN="arn:aws:sqs:${REGION}:${ACCOUNT_ID}:event-queue"

# --------------------------------------------------
# 3. SNS inventory-events topic
# --------------------------------------------------
echo "Checking SNS topic..."
TOPIC_ARN=$(aws sns list-topics \
    --query "Topics[?ends_with(TopicArn, ':inventory-events')].TopicArn" \
    --output text \
    --endpoint-url "$AWS_ENDPOINT_URL")

if [ -n "$TOPIC_ARN" ] && [ "$TOPIC_ARN" != "None" ]; then
    echo "  [SKIP] inventory-events topic already exists: $TOPIC_ARN"
else
    echo "  [CREATE] inventory-events topic..."
    TOPIC_ARN=$(aws sns create-topic \
        --name inventory-events \
        --query TopicArn \
        --output text \
        --endpoint-url "$AWS_ENDPOINT_URL")
    echo "  Topic ARN: $TOPIC_ARN"

    sleep 1

    echo "  Subscribing event-queue to inventory-events..."
    aws sns subscribe \
        --topic-arn "$TOPIC_ARN" \
        --protocol sqs \
        --notification-endpoint "$QUEUE_ARN" \
        --endpoint-url "$AWS_ENDPOINT_URL"
fi

echo ""
echo "=== Init complete ==="
echo "  SNS Topic:  $TOPIC_ARN"
echo "  SQS Queue:  arn:aws:sqs:${REGION}:${ACCOUNT_ID}:event-queue"
echo "  DLQ:        $DLQ_ARN"