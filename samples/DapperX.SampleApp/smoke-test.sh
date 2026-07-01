#!/usr/bin/env bash
# Smoke-test all DapperX Sample App minimal API endpoints.
# Prerequisites: curl, jq, app running at BASE_URL (default http://localhost:5000)
set -uo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESPONSES_FILE="${RESPONSES_FILE:-${SCRIPT_DIR}/responses.txt}"
BODY_FILE="$(mktemp)"
trap 'rm -f "$BODY_FILE"' EXIT

PASSED=0
FAILED=0

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_response() {
  local name="$1"
  local method="$2"
  local url="$3"
  local status="$4"
  {
    echo "=== ${name} ==="
    echo "${method} ${url} -> HTTP ${status}"
    if [[ -s "$BODY_FILE" ]]; then
      cat "$BODY_FILE"
    else
      echo "(empty body)"
    fi
    echo ""
  } >>"$RESPONSES_FILE"
}

pass() {
  echo -e "${GREEN}PASS${NC}: $1"
  PASSED=$((PASSED + 1))
}

fail() {
  echo -e "${RED}FAIL${NC}: $1"
  if [[ -s "$BODY_FILE" ]]; then
    echo "  Response: $(head -c 500 "$BODY_FILE")"
  fi
  FAILED=$((FAILED + 1))
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

assert_status() {
  local name="$1"
  local method="$2"
  local url="$3"
  local expected="$4"
  shift 4

  local status
  : >"$BODY_FILE"
  status=$(curl -sS -o "$BODY_FILE" -w "%{http_code}" -X "$method" "$@" "$url") || {
    fail "${name} (curl error)"
    log_response "$name" "$method" "$url" "curl-error"
    return 1
  }

  log_response "$name" "$method" "$url" "$status"

  if [[ "$status" == "$expected" ]]; then
    pass "${name} (${method} ${expected})"
    return 0
  fi

  fail "${name} (expected HTTP ${expected}, got ${status})"
  return 1
}

json_post_capture() {
  local name="$1"
  local url="$2"
  local body="$3"
  local expected="${4:-201}"

  assert_status "$name" POST "$url" "$expected" \
    -H "Content-Type: application/json" \
    -d "$body"
}

# --- prerequisites ---
require_cmd curl
require_cmd jq

: >"$RESPONSES_FILE"
echo "DapperX Sample App smoke test"
echo "BASE_URL=${BASE_URL}"
echo "RESPONSES_FILE=${RESPONSES_FILE}"
echo ""

if ! curl -sS -o /dev/null --connect-timeout 3 "$BASE_URL/"; then
  echo -e "${YELLOW}WARNING${NC}: Cannot reach ${BASE_URL}. Start the app first:" >&2
  echo "  docker compose -f samples/DapperX.SampleApp/docker-compose.yml up -d" >&2
  echo "  dotnet run --project samples/DapperX.SampleApp/DapperX.SampleApp.csproj" >&2
  exit 1
fi

# --- 1. Root ---
assert_status "GET /" GET "${BASE_URL}/" 200 || true
assert_status "GET /demo/sqlite" GET "${BASE_URL}/demo/sqlite" 200 || true

# --- 2. Catalog seed ---
json_post_capture "POST /demo/catalog (transformer path)" "${BASE_URL}/demo/catalog" \
  '{"sku":"SKU-1","name":"Widget","category":"Books","price":19.99,"inStock":true,"status":"Active","encryptedPayload":"secret"}' 201 || true
CATALOG_ID=$(jq -r '.id // empty' "$BODY_FILE" 2>/dev/null || true)

json_post_capture "POST /demo/catalog (second Books item)" "${BASE_URL}/demo/catalog" \
  '{"sku":"SKU-2","name":"Novel","category":"Books","price":9.99,"inStock":true,"status":"Active","encryptedPayload":""}' 201 || true
CATALOG_ID_2=$(jq -r '.id // empty' "$BODY_FILE" 2>/dev/null || true)

json_post_capture "POST /demo/catalog/batch" "${BASE_URL}/demo/catalog/batch" \
  '[{"sku":"SKU-3","name":"Batch A","category":"Electronics","price":29.99,"inStock":true,"status":"Active"},{"sku":"SKU-4","name":"Batch B","category":"Books","price":14.99,"inStock":true,"status":"Active"}]' 200 || true

if [[ -z "${CATALOG_ID}" || -z "${CATALOG_ID_2}" ]]; then
  mapfile -t _CATALOG_IDS < <(curl -sS "${BASE_URL}/demo/catalog" | jq -r '.[].id')
  [[ -z "${CATALOG_ID}" && ${#_CATALOG_IDS[@]} -ge 1 ]] && CATALOG_ID="${_CATALOG_IDS[0]}"
  [[ -z "${CATALOG_ID_2}" && ${#_CATALOG_IDS[@]} -ge 2 ]] && CATALOG_ID_2="${_CATALOG_IDS[1]}"
fi

# --- 3. Catalog reads ---
assert_status "GET /demo/catalog" GET "${BASE_URL}/demo/catalog" 200 || true

if [[ -n "${CATALOG_ID}" ]]; then
  assert_status "GET /demo/catalog/{id}" GET "${BASE_URL}/demo/catalog/${CATALOG_ID}" 200 || true
else
  fail "GET /demo/catalog/{id} (skipped — no CATALOG_ID)"
fi

if [[ -n "${CATALOG_ID}" && -n "${CATALOG_ID_2}" ]]; then
  assert_status "GET /demo/catalog/ids" GET "${BASE_URL}/demo/catalog/ids?ids=${CATALOG_ID}&ids=${CATALOG_ID_2}" 200 || true
else
  fail "GET /demo/catalog/ids (skipped — missing catalog ids)"
fi

if [[ -n "${CATALOG_ID}" ]]; then
  assert_status "GET /demo/catalog/exists/{id}" GET "${BASE_URL}/demo/catalog/exists/${CATALOG_ID}" 200 || true
else
  fail "GET /demo/catalog/exists/{id} (skipped — no CATALOG_ID)"
fi

assert_status "GET /demo/catalog/count" GET "${BASE_URL}/demo/catalog/count" 200 || true
assert_status "GET /demo/catalog/page" GET "${BASE_URL}/demo/catalog/page?page=0&size=10" 200 || true
assert_status "GET /demo/catalog/page/sorted" GET "${BASE_URL}/demo/catalog/page/sorted?page=0&size=10" 200 || true
assert_status "GET /demo/catalog/slice" GET "${BASE_URL}/demo/catalog/slice?page=0&size=10" 200 || true
assert_status "GET /demo/catalog/derived/in-stock-cheap" GET "${BASE_URL}/demo/catalog/derived/in-stock-cheap?maxPrice=50" 200 || true
assert_status "GET /demo/catalog/derived/by-category-sorted" GET "${BASE_URL}/demo/catalog/derived/by-category-sorted?category=Books" 200 || true
assert_status "GET /demo/catalog/derived/slice/{category}" GET "${BASE_URL}/demo/catalog/derived/slice/Books?page=0&size=10" 200 || true

# --- 4. Catalog locks ---
assert_status "POST /demo/catalog/lock-read/{category}" POST "${BASE_URL}/demo/catalog/lock-read/Books" 200 || true
assert_status "POST /demo/catalog/lock-update/{category}" POST "${BASE_URL}/demo/catalog/lock-update/Books" 200 || true

# --- 5. Catalog mutate ---
if [[ -n "${CATALOG_ID}" ]]; then
  assert_status "PUT /demo/catalog/{id}" PUT "${BASE_URL}/demo/catalog/${CATALOG_ID}" 204 \
    -H "Content-Type: application/json" \
    -d "{\"sku\":\"SKU-1\",\"name\":\"Widget Updated\",\"category\":\"Books\",\"price\":21.99,\"inStock\":true,\"status\":\"Active\",\"encryptedPayload\":\"secret\"}" || true
else
  fail "PUT /demo/catalog/{id} (skipped — no CATALOG_ID)"
fi

if [[ -n "${CATALOG_ID_2}" ]]; then
  assert_status "DELETE /demo/catalog/bulk" DELETE "${BASE_URL}/demo/catalog/bulk?ids=${CATALOG_ID_2}" 204 || true
else
  fail "DELETE /demo/catalog/bulk (skipped — no CATALOG_ID_2)"
fi

if [[ -n "${CATALOG_ID}" ]]; then
  assert_status "DELETE /demo/catalog/{id}" DELETE "${BASE_URL}/demo/catalog/${CATALOG_ID}" 204 || true
else
  fail "DELETE /demo/catalog/{id} (skipped — no CATALOG_ID)"
fi

# --- 6. Users ---
json_post_capture "POST /demo/users" "${BASE_URL}/demo/users" \
  '{"email":"demo@example.com","region":"US","addressCity":"NYC","addressCountry":"US"}' 201 || true
USER_ID=$(jq -r '.id // empty' "$BODY_FILE" 2>/dev/null || true)

assert_status "GET /demo/users" GET "${BASE_URL}/demo/users" 200 || true

if [[ -n "${USER_ID}" ]]; then
  assert_status "GET /demo/users/{id}/profile" GET "${BASE_URL}/demo/users/${USER_ID}/profile" 200 || true
else
  fail "GET /demo/users/{id}/profile (skipped — no USER_ID)"
fi

assert_status "GET /demo/users/listener-count" GET "${BASE_URL}/demo/users/listener-count" 200 || true
assert_status "GET /demo/users/include-deleted" GET "${BASE_URL}/demo/users/include-deleted" 200 || true

if [[ -n "${USER_ID}" ]]; then
  assert_status "DELETE /demo/users/{id}" DELETE "${BASE_URL}/demo/users/${USER_ID}" 200 || true
else
  fail "DELETE /demo/users/{id} (skipped — no USER_ID)"
fi

# --- 7. Members ---
json_post_capture "POST /demo/members" "${BASE_URL}/demo/members" \
  '{"email":"member@example.com","bio":"Hello"}' 201 || true
MEMBER_ID=$(jq -r '.id // empty' "$BODY_FILE" 2>/dev/null || true)

if [[ -n "${MEMBER_ID}" ]]; then
  assert_status "GET /demo/members/{id}" GET "${BASE_URL}/demo/members/${MEMBER_ID}" 200 || true
else
  fail "GET /demo/members/{id} (skipped — no MEMBER_ID)"
fi

# --- 8. Orders ---
json_post_capture "POST /demo/orders" "${BASE_URL}/demo/orders" \
  '{"code":"ORD-001"}' 201 || true
ORDER_ID=$(jq -r '.id // empty' "$BODY_FILE" 2>/dev/null || true)

if [[ -n "${ORDER_ID}" ]]; then
  assert_status "GET /demo/orders/{id}" GET "${BASE_URL}/demo/orders/${ORDER_ID}" 200 || true
else
  fail "GET /demo/orders/{id} (skipped — no ORDER_ID)"
fi

# --- 9. Org ---
json_post_capture "POST /demo/org/departments" "${BASE_URL}/demo/org/departments" \
  '{"name":"Engineering"}' 201 || true
DEPT_ID=$(jq -r '.id // empty' "$BODY_FILE" 2>/dev/null || true)

if [[ -n "${DEPT_ID}" ]]; then
  assert_status "GET /demo/org/departments/{id}/employees" GET "${BASE_URL}/demo/org/departments/${DEPT_ID}/employees" 200 || true
else
  fail "GET /demo/org/departments/{id}/employees (skipped — no DEPT_ID)"
fi

# --- 10. Graph ---
json_post_capture "POST /demo/graph" "${BASE_URL}/demo/graph" \
  '{"name":"Parent","children":["child-a","child-b"]}' 200 || true

# --- summary ---
echo ""
echo "Results: ${PASSED} passed, ${FAILED} failed"
echo "Responses written to: ${RESPONSES_FILE}"

if [[ "$FAILED" -gt 0 ]]; then
  exit 1
fi

exit 0
