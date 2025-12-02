#!/bin/bash

# Script to create Kafka topics for the JustSaying Kafka sample
# Usage: ./setup-topics.sh

set -e

echo "🚀 Creating Kafka topics for JustSaying sample..."
echo ""

CONTAINER_NAME="kafka"
BOOTSTRAP_SERVER="localhost:9092"
PARTITIONS=3
REPLICATION_FACTOR=1

# Check if Kafka container is running
if ! docker ps | grep -q "$CONTAINER_NAME"; then
    echo "❌ Error: Kafka container '$CONTAINER_NAME' is not running"
    echo "Please start Kafka first with: docker-compose up -d"
    exit 1
fi

echo "✅ Kafka container is running"
echo ""

# Function to create a topic
create_topic() {
    local topic_name=$1
    echo "📝 Creating topic: $topic_name"
    
    if docker exec "$CONTAINER_NAME" kafka-topics --list --bootstrap-server "$BOOTSTRAP_SERVER" | grep -q "^${topic_name}$"; then
        echo "⚠️  Topic '$topic_name' already exists, skipping..."
    else
        docker exec "$CONTAINER_NAME" kafka-topics --create \
            --topic "$topic_name" \
            --bootstrap-server "$BOOTSTRAP_SERVER" \
            --partitions "$PARTITIONS" \
            --replication-factor "$REPLICATION_FACTOR"
        echo "✅ Topic '$topic_name' created successfully"
    fi
    echo ""
}

# Create topics
create_topic "order-placed"
create_topic "order-confirmed"

# List all topics
echo "📋 All topics:"
docker exec "$CONTAINER_NAME" kafka-topics --list --bootstrap-server "$BOOTSTRAP_SERVER"
echo ""

echo "✨ Setup complete! You can now run the sample application."
echo ""
echo "To run the sample:"
echo "  dotnet run"
echo ""
echo "To test the API:"
echo "  curl http://localhost:5000/api/health"
echo ""
