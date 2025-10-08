#!/bin/bash

# Upload a document
echo "uploading document"
UPLOAD_RESPONSE=$(curl -s -X POST http://localhost:5198/api/document/upload \
  -F "file=@sample.pdf" \
  -H "Accept: application/json")
echo "$UPLOAD_RESPONSE"
echo 

DOCUMENT_ID=$(echo "$UPLOAD_RESPONSE" | grep -o '"id":"[^"]*"' | sed 's/"id":"\(.*\)"/\1/')
if [[ -z "$DOCUMENT_ID" ]]; then
  echo "Failed to extract document ID."
  exit 1
fi

echo "Extracted document ID: $DOCUMENT_ID"
echo

echo "get document"
# Get document
curl -X GET "http://localhost:5198/api/document/$DOCUMENT_ID" \
  -H "Accept: application/json"