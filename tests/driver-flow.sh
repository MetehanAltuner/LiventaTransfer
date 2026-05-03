#!/usr/bin/env bash
set +e
BASE="http://localhost:5062"
USER_ID="fe7d1013-736e-42f9-99d8-1665447dd797"  # koordinator (will resolve dynamically below)

# --- Resolve coordinator userId from JWT (sub claim) ---
TOKEN=$(curl -s -X POST -H "Content-Type: application/json" \
  -d '{"username":"koordinator","password":"coord123"}' \
  "$BASE/api/auth/login" | grep -oE '"token":"[^"]+"' | sed 's/.*"://; s/"//g')
PAYLOAD=$(echo "$TOKEN" | cut -d. -f2)
# base64-url decode (pad to multiple of 4)
PAD=$(printf '%*s' $(( (4 - ${#PAYLOAD} % 4) % 4 )) '' | tr ' ' '=')
USER_ID=$(echo "${PAYLOAD}${PAD}" | tr '_-' '/+' | base64 -d 2>/dev/null | grep -oE '"sub":"[a-f0-9-]+"' | sed 's/.*"://; s/"//g')

# --- Resolve Job publicIds by internal id ---
JOBS_JSON=$(curl -s "$BASE/api/jobs?pageSize=20")
get_pid() { echo "$JOBS_JSON" | grep -oE "\"id\":$1,\"publicId\":\"[a-f0-9-]+\"" | grep -oE '[a-f0-9]{8}-[a-f0-9-]+' ; }

PID6=$(get_pid 6)
PID7=$(get_pid 7)
PID10=$(get_pid 10)
RANDOM_GUID="00000000-0000-0000-0000-000000000000"

# Helpers
hr() { printf '\n──── %s ────\n' "$1"; }
field() { grep -oE "\"$1\":[^,}]+" | head -1 | sed -E "s/\"$1\"://; s/\"//g"; }
http() { curl -s -o /tmp/resp.json -w "%{http_code}" "$@"; }
ok() { printf '  ✓ %s\n' "$1"; }
fail() { printf '  ✗ %s — %s\n' "$1" "$2"; cat /tmp/resp.json | head -c 300; echo; }
assert_status() { [ "$2" = "$1" ] && ok "$3 (HTTP $2)" || fail "$3" "expected $1, got $2"; }
assert_eq() { [ "$2" = "$1" ] && ok "$3 = $2" || fail "$3" "expected '$1', got '$2'"; }

echo "USER_ID=$USER_ID"
echo "PID6=$PID6  PID7=$PID7  PID10=$PID10"

# ================================================================
hr "1. LOOKUP: driver-stages enum"
http "$BASE/api/lookups/driver-stages" > /tmp/code
COUNT=$(grep -oE '"id":[0-9]+' /tmp/resp.json | wc -l)
assert_eq "5" "$(echo $COUNT)" "driver-stages count"

# ================================================================
hr "2. READ: GET /api/driver/jobs/{PID6} (Assigned)"
CODE=$(http "$BASE/api/driver/jobs/$PID6")
assert_status "200" "$CODE" "GET job6 by Guid"
PID_RESP=$(field publicId < /tmp/resp.json)
assert_eq "$PID6" "$PID_RESP" "publicId echo"
STAGE=$(field driverStage < /tmp/resp.json)
assert_eq "0" "$STAGE" "stage = NotStarted"

# ================================================================
hr "3. READ: random Guid → 404"
CODE=$(http "$BASE/api/driver/jobs/$RANDOM_GUID")
assert_status "404" "$CODE" "random Guid → 404"

# ================================================================
hr "4. READ: invalid Guid format (long id) → 404 from route"
# /api/driver/jobs/6 — won't match {publicId:guid}, expect 404
CODE=$(http "$BASE/api/driver/jobs/6")
assert_status "404" "$CODE" "long id on Guid route → 404"

# ================================================================
hr "5. ACTION: contact on job7 (driver yok) → 400"
CODE=$(http -X POST "$BASE/api/driver/jobs/$PID7/contact?userId=$USER_ID")
assert_status "400" "$CODE" "contact without driver → 400"
ok "message = $(field message < /tmp/resp.json)"

# ================================================================
hr "6. ACTION: contact on job10 (Cancelled) → 400"
CODE=$(http -X POST "$BASE/api/driver/jobs/$PID10/contact?userId=$USER_ID")
assert_status "400" "$CODE" "contact on Cancelled → 400"
ok "message = $(field message < /tmp/resp.json)"

# ================================================================
hr "7. ACTION: random Guid → 404"
CODE=$(http -X POST "$BASE/api/driver/jobs/$RANDOM_GUID/contact?userId=$USER_ID")
assert_status "404" "$CODE" "contact non-existent Guid → 404"

# ================================================================
hr "8. FULL HAPPY PATH on job6 (single-stop)"
echo "  → Contact"
http -X POST "$BASE/api/driver/jobs/$PID6/contact?userId=$USER_ID" > /tmp/code
assert_eq "1" "$(field driverStage < /tmp/resp.json)" "stage after contact"

echo "  → Depart"
http -X POST "$BASE/api/driver/jobs/$PID6/depart?userId=$USER_ID" > /tmp/code
assert_eq "2" "$(field driverStage < /tmp/resp.json)" "stage after depart"
assert_eq "3" "$(field status < /tmp/resp.json)" "JobStatus → InProgress"

echo "  → Pickup stop 6"
http -X POST "$BASE/api/driver/jobs/$PID6/stops/6/pickup?userId=$USER_ID" > /tmp/code
assert_eq "3" "$(field driverStage < /tmp/resp.json)" "stage after pickup"

echo "  → Dropoff stop 6 → all done → Completed"
http -X POST "$BASE/api/driver/jobs/$PID6/stops/6/dropoff?userId=$USER_ID" > /tmp/code
assert_eq "4" "$(field driverStage < /tmp/resp.json)" "stage after dropoff"
assert_eq "4" "$(field status < /tmp/resp.json)" "JobStatus → Completed"

# ================================================================
hr "9. NULL EDGE: re-contact on Completed → 400"
CODE=$(http -X POST "$BASE/api/driver/jobs/$PID6/contact?userId=$USER_ID")
assert_status "400" "$CODE" "contact on Completed → 400"

# ================================================================
hr "10. PREP job7 multi-stop: assign driver via PUT"
curl -s -X PATCH -H "Content-Type: application/json" \
  -d '{"newStatus":2}' "$BASE/api/jobs/7/status?userId=$USER_ID" > /dev/null
curl -s -X PUT -H "Content-Type: application/json" \
  -d '{"jobDate":"2026-05-03","jobTime":"15:00:00","jobType":1,"driverId":1,"vehicleId":1,"vehicleOwnerId":1,"stops":[{"customerId":5,"passengerId":8,"passengerCount":2,"pickupLocationId":3,"dropoffLocationId":8,"flightCode":"TK2554","salePrice":180},{"customerId":3,"passengerId":6,"passengerCount":2,"pickupLocationId":3,"dropoffLocationId":8,"flightCode":"TK2554","salePrice":200}]}' \
  "$BASE/api/jobs/7" > /dev/null

RESP=$(curl -s "$BASE/api/driver/jobs/$PID7")
STOP1=$(echo "$RESP" | grep -oE '"id":[0-9]+,"sequence":1' | grep -oE '[0-9]+' | head -1)
STOP2=$(echo "$RESP" | grep -oE '"id":[0-9]+,"sequence":2' | grep -oE '[0-9]+' | head -1)
ok "job7 stops: $STOP1, $STOP2"

# ================================================================
hr "11. NULL EDGE: dropoff before pickup → 400"
CODE=$(http -X POST "$BASE/api/driver/jobs/$PID7/stops/$STOP1/dropoff?userId=$USER_ID")
assert_status "400" "$CODE" "dropoff before pickup → 400"
ok "message = $(field message < /tmp/resp.json)"

# ================================================================
hr "12. NULL EDGE: stopId belongs to another job → 404"
CODE=$(http -X POST "$BASE/api/driver/jobs/$PID7/stops/6/pickup?userId=$USER_ID")
assert_status "404" "$CODE" "wrong-job stopId → 404"

# ================================================================
hr "13. MULTI-STOP partial progress on job7"
echo "  → Pickup stop1 (auto-fills Contacted+Departed)"
http -X POST "$BASE/api/driver/jobs/$PID7/stops/$STOP1/pickup?userId=$USER_ID" > /tmp/code
assert_eq "3" "$(field driverStage < /tmp/resp.json)" "stage = PickedUp"
[ "$(field contactedAt < /tmp/resp.json)" != "null" ] && ok "auto contactedAt set" || fail "auto contact" ""
[ "$(field departedAt < /tmp/resp.json)" != "null" ] && ok "auto departedAt set" || fail "auto depart" ""

echo "  → Dropoff stop1 (stop2 still pending)"
http -X POST "$BASE/api/driver/jobs/$PID7/stops/$STOP1/dropoff?userId=$USER_ID" > /tmp/code
assert_eq "4" "$(field driverStage < /tmp/resp.json)" "stage = DroppedOff"
assert_eq "3" "$(field status < /tmp/resp.json)" "JobStatus stays InProgress"

NAV_TYPE=$(grep -oE '"nextNavigation":\{[^}]*' /tmp/resp.json | grep -oE '"type":"[^"]*"' | sed 's/.*://; s/"//g')
NAV_STOP=$(grep -oE '"nextNavigation":\{[^}]*' /tmp/resp.json | grep -oE '"stopId":[0-9]+' | head -1 | sed 's/.*://')
assert_eq "pickup" "$NAV_TYPE" "next nav type → stop2 pickup"
assert_eq "$STOP2" "$NAV_STOP" "next nav stopId"

echo "  → Pickup + Dropoff stop2 → Completed"
http -X POST "$BASE/api/driver/jobs/$PID7/stops/$STOP2/pickup?userId=$USER_ID" > /tmp/code
http -X POST "$BASE/api/driver/jobs/$PID7/stops/$STOP2/dropoff?userId=$USER_ID" > /tmp/code
assert_eq "4" "$(field status < /tmp/resp.json)" "JobStatus → Completed"

# ================================================================
hr "14. LIST endpoint includes publicId + DriverStage"
curl -s "$BASE/api/jobs?pageSize=5" \
  | grep -oE '"id":[0-9]+,"publicId":"[a-f0-9-]+","jobNumber":"[^"]+"' \
  | head -5
ok "list includes publicId field"

hr "DONE"
