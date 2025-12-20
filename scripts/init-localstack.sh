#!/bin/bash

echo "Waiting for LocalStack to be ready..."
sleep 10

# Create DynamoDB Table
echo "Creating DynamoDB table: Payments..."
awslocal dynamodb create-table \
    --table-name Payments \
    --attribute-definitions \
        AttributeName=PaymentId,AttributeType=S \
        AttributeName=OrderId,AttributeType=S \
        AttributeName=Status,AttributeType=S \
        AttributeName=CreatedAt,AttributeType=S \
    --key-schema AttributeName=PaymentId,KeyType=HASH \
    --global-secondary-indexes \
        "[
            {
                \"IndexName\": \"GSI_OrderId\",
                \"KeySchema\": [{\"AttributeName\":\"OrderId\",\"KeyType\":\"HASH\"}],
                \"Projection\":{\"ProjectionType\":\"ALL\"},
                \"ProvisionedThroughput\":{\"ReadCapacityUnits\":5,\"WriteCapacityUnits\":5}
            },
            {
                \"IndexName\": \"GSI_Status_CreatedAt\",
                \"KeySchema\": [
                    {\"AttributeName\":\"Status\",\"KeyType\":\"HASH\"},
                    {\"AttributeName\":\"CreatedAt\",\"KeyType\":\"RANGE\"}
                ],
                \"Projection\":{\"ProjectionType\":\"ALL\"},
                \"ProvisionedThroughput\":{\"ReadCapacityUnits\":5,\"WriteCapacityUnits\":5}
            }
        ]" \
    --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5

# Create SNS Topic
echo "Creating SNS topic: sns-payment-events..."
awslocal sns create-topic --name sns-payment-events

# Create SQS Queue
echo "Creating SQS queue: sqs-payments-order-queue..."
awslocal sqs create-queue --queue-name sqs-payments-order-queue

# Subscribe SQS to SNS
echo "Subscribing SQS to SNS..."
TOPIC_ARN="arn:aws:sns:us-east-1:000000000000:sns-payment-events"
QUEUE_URL="http://localhost:4566/000000000000/sqs-payments-order-queue"

awslocal sns subscribe \
    --topic-arn $TOPIC_ARN \
    --protocol sqs \
    --notification-endpoint arn:aws:sqs:us-east-1:000000000000:sqs-payments-order-queue

echo "LocalStack initialization completed!"
