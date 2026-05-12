#!/usr/bin/env bash
set -euo pipefail

CLOUD_NAME="blackinkpaper"
API_KEY="953248147123567"
API_SECRET="DzjkktpG29uS1VwlaMpwcy256RY"
FOLDER="illustrations"
DIR="BlackInkPaperAPIService/wwwroot/assets/illustrations"
RESULTS_FILE="cloudinary_remaining_results.json"

FILES=(
  "Asset 32.svg" "Asset 33.svg" "Asset 34.svg" "Asset 35.svg"
  "Asset 36.svg" "Asset 37.svg" "Asset 38.svg" "Asset 39.svg"
  "Asset 4.svg"  "Asset 40.svg" "Asset 41.svg" "Asset 42.svg"
  "Asset 43.svg" "Asset 44.svg" "Asset 45.svg" "Asset 46.svg"
  "Asset 5.svg"  "Asset 6.svg"  "Asset 7.svg"  "Asset 8.svg"
  "Asset 9.svg"
)

echo "[" > "$RESULTS_FILE"
first=true
success=0; failed=0

for filename in "${FILES[@]}"; do
  public_id=$(echo "$filename" | sed 's/\.[^.]*$//' | tr '[:upper:]' '[:lower:]' | tr ' ' '_')
  echo "Uploading: $filename → $FOLDER/$public_id"

  http_code=$(curl -s -o /tmp/cl_rem_response.json -w "%{http_code}" \
    "https://${API_KEY}:${API_SECRET}@api.cloudinary.com/v1_1/${CLOUD_NAME}/image/upload" \
    -X POST \
    -F "file=@${DIR}/${filename}" \
    -F "folder=${FOLDER}" \
    -F "public_id=${public_id}" \
    -F "resource_type=image")

  body=$(cat /tmp/cl_rem_response.json)

  if [[ "$http_code" == "200" ]]; then
    url=$(echo "$body" | grep -o '"secure_url":"[^"]*"' | cut -d'"' -f4)
    echo "  ✓ $url"
    [ "$first" = true ] && first=false || echo "," >> "$RESULTS_FILE"
    echo "$body" >> "$RESULTS_FILE"
    ((success++))
  else
    error=$(echo "$body" | grep -o '"message":"[^"]*"' | cut -d'"' -f4)
    echo "  ✗ HTTP $http_code: $error"
    ((failed++))
  fi
done

echo "]" >> "$RESULTS_FILE"
echo ""
echo "Done: $success uploaded, $failed failed → $RESULTS_FILE"
