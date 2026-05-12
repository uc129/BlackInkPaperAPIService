#!/usr/bin/env bash
set -euo pipefail

CLOUD_NAME="blackinkpaper"
API_KEY="953248147123567"
API_SECRET="DzjkktpG29uS1VwlaMpwcy256RY"
FOLDER="illustrations"
ILLUSTRATIONS_DIR="BlackInkPaperAPIService/wwwroot/assets/illustrations"
RESULTS_FILE="cloudinary_upload_results.json"

# Start JSON array
echo "[" > "$RESULTS_FILE"
first=true
success=0
failed=0

for filepath in "$ILLUSTRATIONS_DIR"/*; do
  filename=$(basename "$filepath")
  public_id=$(echo "$filename" | sed 's/\.[^.]*$//' | tr '[:upper:]' '[:lower:]' | tr ' ' '_')

  echo "Uploading: $filename → $FOLDER/$public_id"

  http_code=$(curl -s -o /tmp/cl_upload_response.json -w "%{http_code}" \
    "https://${API_KEY}:${API_SECRET}@api.cloudinary.com/v1_1/${CLOUD_NAME}/image/upload" \
    -X POST \
    -F "file=@${filepath}" \
    -F "folder=${FOLDER}" \
    -F "public_id=${public_id}" \
    -F "resource_type=image")

  body=$(cat /tmp/cl_upload_response.json)

  if [[ "$http_code" == "200" ]]; then
    url=$(echo "$body" | grep -o '"secure_url":"[^"]*"' | cut -d'"' -f4)
    echo "  ✓ $url"

    # Append to results JSON array
    if [ "$first" = true ]; then
      first=false
    else
      echo "," >> "$RESULTS_FILE"
    fi
    echo "$body" >> "$RESULTS_FILE"

    ((success++))
  else
    error=$(echo "$body" | grep -o '"message":"[^"]*"' | cut -d'"' -f4)
    echo "  ✗ Failed (HTTP $http_code): $error"
    ((failed++))
  fi
done

echo "]" >> "$RESULTS_FILE"

echo ""
echo "Done: $success uploaded, $failed failed"
echo "Results saved to: $RESULTS_FILE"
