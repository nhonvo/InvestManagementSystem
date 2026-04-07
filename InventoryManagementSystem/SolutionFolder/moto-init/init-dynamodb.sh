#!/bin/bash
set -ex
echo "Initializing Moto DynamoDB..."

# We use the $AWS_ENDPOINT_URL injected from docker-compose.yml
ENDPOINT_URL="${AWS_ENDPOINT_URL:-http://moto:5000}"

aws dynamodb create-table \
    --table-name inventory-event-logs \
    --attribute-definitions \
        AttributeName=EventType,AttributeType=S \
        AttributeName=MessageId,AttributeType=S \
    --key-schema \
        AttributeName=EventType,KeyType=HASH \
        AttributeName=MessageId,KeyType=RANGE \
    --billing-mode PAY_PER_REQUEST \
    --endpoint-url $ENDPOINT_URL \
    --region us-east-1

aws dynamodb update-time-to-live \
    --table-name inventory-event-logs \
    --time-to-live-specification "Enabled=true, AttributeName=TTL" \
    --endpoint-url $ENDPOINT_URL \
    --region us-east-1

echo "Moto DynamoDB initialization complete."
