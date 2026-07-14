#!/usr/bin/env bash
set -euo pipefail

# Runs a measure -> change -> re-measure experiment against BudgetingAgent endpoints.
# Produces:
# 1) per-request raw CSV with correlation IDs and KPIs
# 2) compact summary CSV (phase x scenario)
# 3) quality scoring template CSV for manual rubric scoring

BASE_URL="${BASE_URL:-http://localhost:7071/api}"
RUNS_PER_SCENARIO="${RUNS_PER_SCENARIO:-10}"
OUTPUT_DIR="${OUTPUT_DIR:-./benchmark-output}"
PHASE_BASELINE="${PHASE_BASELINE:-baseline}"
PHASE_OPTIMIZED="${PHASE_OPTIMIZED:-optimized}"
INPUT_RATE_PER_1K="${INPUT_RATE_PER_1K:-0.00015}"
OUTPUT_RATE_PER_1K="${OUTPUT_RATE_PER_1K:-0.00060}"
START_HOST="${START_HOST:-1}"

RAW_CSV="${OUTPUT_DIR}/raw_metrics.csv"
SUMMARY_CSV="${OUTPUT_DIR}/summary_metrics.csv"
QUALITY_CSV="${OUTPUT_DIR}/quality_rubric_template.csv"
HOST_LOG="${OUTPUT_DIR}/func_host.log"
RESPONSES_DIR="${OUTPUT_DIR}/responses"

mkdir -p "${OUTPUT_DIR}" "${RESPONSES_DIR}"

host_pid=""

cleanup() {
  if [[ -n "${host_pid}" ]]; then
    kill "${host_pid}" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

estimate_tokens() {
  local text="$1"
  local len=${#text}
  awk -v l="$len" 'BEGIN { printf("%d", int((l + 3) / 4)) }'
}

uuid() {
  if command -v uuidgen >/dev/null 2>&1; then
    uuidgen | tr '[:upper:]' '[:lower:]'
  else
    python3 - <<'PY'
import uuid
print(str(uuid.uuid4()))
PY
  fi
}

wait_for_host() {
  echo "Waiting for function host at ${BASE_URL}..."
  local ok=0
  for _ in $(seq 1 45); do
    if curl -fsS "${BASE_URL}/ai/analyze-spending?timeframe=last-month&phase=warmup" >/dev/null 2>&1; then
      ok=1
      break
    fi
    sleep 1
  done

  if [[ "$ok" -ne 1 ]]; then
    echo "Host did not become ready. See ${HOST_LOG}" >&2
    exit 1
  fi
}

start_host_if_needed() {
  if [[ "${START_HOST}" == "1" ]]; then
    echo "Starting local function host..."
    func host start >"${HOST_LOG}" 2>&1 &
    host_pid="$!"
    wait_for_host
  else
    echo "Using existing host. Skipping start."
  fi
}

init_csvs() {
  echo "phase,scenario,run,workflow_id,hop_id,agent_id,response_id,status_code,latency_ms,token_measurement_mode,exact_usage_available,exact_usage_source,measured_input_tokens,measured_output_tokens,measured_total_tokens,exact_input_tokens,exact_output_tokens,exact_total_tokens,estimated_input_tokens,estimated_output_tokens,estimated_total_tokens,cost_per_request" >"${RAW_CSV}"
  echo "phase,scenario,runs,avg_measured_input_tokens,avg_measured_output_tokens,avg_measured_total_tokens,avg_exact_input_tokens,avg_exact_output_tokens,avg_exact_total_tokens,avg_estimated_input_tokens,avg_estimated_output_tokens,avg_estimated_total_tokens,avg_cost_per_request,p95_latency_ms" >"${SUMMARY_CSV}"
  echo "phase,scenario,run,workflow_id,response_id,quality_score,notes" >"${QUALITY_CSV}"
}

post_chat() {
  local phase="$1"
  local scenario="$2"
  local run_no="$3"
  local message="$4"

  local workflow_id="wf-$(uuid)"
  local hop_id="hop-$(uuid)"

  local req_file
  req_file="$(mktemp)"
  printf '{"Message":"%s"}' "${message}" >"${req_file}"

  local body_file
  body_file="$(mktemp)"

  local status latency
  read -r status latency < <(
    curl -sS -o "${body_file}" -w "%{http_code} %{time_total}" \
      -X POST "${BASE_URL}/ai/chat?phase=${phase}" \
      -H "Content-Type: application/json" \
      -H "x-experiment-phase: ${phase}" \
      -H "x-workflow-id: ${workflow_id}" \
      -H "x-hop-id: ${hop_id}" \
      --data-binary "@${req_file}"
  )

  local response_json
  response_json="$(cat "${body_file}")"

  local parsed
  parsed="$(python3 - <<'PY' "${response_json}"
import json,sys
raw=sys.argv[1]
try:
    o=json.loads(raw)
except Exception:
    print("||||||||||||||")
    raise SystemExit(0)
agent_id=o.get("AgentId") or ""
response_id=o.get("ResponseId") or ""
text=o.get("Text") or ""
mode=o.get("TokenMeasurementMode") or "hybrid"
exact_available=o.get("ExactUsageAvailable")
exact_available="true" if exact_available else "false"
exact_source=o.get("ExactUsageSource") or "none"
measured_input=o.get("MeasuredInputTokens")
measured_output=o.get("MeasuredOutputTokens")
measured_total=o.get("MeasuredTotalTokens")
exact_input=o.get("ExactInputTokens")
exact_output=o.get("ExactOutputTokens")
exact_total=o.get("ExactTotalTokens")
estimated_input=o.get("EstimatedInputTokens")
estimated_output=o.get("EstimatedOutputTokens")
estimated_total=o.get("EstimatedTotalTokens")

def n(v):
    return "" if v is None else str(v)

print("|".join([
    agent_id,
    response_id,
    text,
    mode,
    exact_available,
    exact_source,
    n(measured_input),
    n(measured_output),
    n(measured_total),
    n(exact_input),
    n(exact_output),
    n(exact_total),
    n(estimated_input),
    n(estimated_output),
    n(estimated_total),
]))
PY
)"

  local agent_id response_id text token_mode exact_available exact_source measured_input measured_output measured_total exact_input exact_output exact_total estimated_input estimated_output estimated_total
  agent_id="${parsed%%|*}"
  local tail_part
  tail_part="${parsed#*|}"
  response_id="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  text="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  token_mode="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  exact_available="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  exact_source="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  measured_input="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  measured_output="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  measured_total="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  exact_input="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  exact_output="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  exact_total="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  estimated_input="${tail_part%%|*}"
  tail_part="${tail_part#*|}"
  estimated_output="${tail_part%%|*}"
  estimated_total="${tail_part#*|}"

  if [[ -z "$estimated_input" ]]; then
    estimated_input="$(estimate_tokens "${message}")"
  fi
  if [[ -z "$estimated_output" ]]; then
    estimated_output="$(estimate_tokens "${text}")"
  fi
  if [[ -z "$estimated_total" ]]; then
    estimated_total=$((estimated_input + estimated_output))
  fi

  if [[ -z "$measured_input" ]]; then
    measured_input="$estimated_input"
  fi
  if [[ -z "$measured_output" ]]; then
    measured_output="$estimated_output"
  fi
  if [[ -z "$measured_total" ]]; then
    measured_total=$((measured_input + measured_output))
  fi

  local cost
  cost="$(awk -v i="$measured_input" -v o="$measured_output" -v ir="$INPUT_RATE_PER_1K" -v orr="$OUTPUT_RATE_PER_1K" 'BEGIN { printf("%.8f", (i/1000.0*ir) + (o/1000.0*orr)) }')"

  local latency_ms
  latency_ms="$(awk -v t="$latency" 'BEGIN { printf("%.2f", t*1000.0) }')"

  echo "${phase},${scenario},${run_no},${workflow_id},${hop_id},${agent_id},${response_id},${status},${latency_ms},${token_mode},${exact_available},${exact_source},${measured_input},${measured_output},${measured_total},${exact_input},${exact_output},${exact_total},${estimated_input},${estimated_output},${estimated_total},${cost}" >>"${RAW_CSV}"
  echo "${phase},${scenario},${run_no},${workflow_id},${response_id},," >>"${QUALITY_CSV}"

  printf '%s\n' "${response_json}" >"${RESPONSES_DIR}/${phase}_${scenario}_run${run_no}.json"

  rm -f "${req_file}" "${body_file}"
}

run_phase() {
  local phase="$1"
  echo "Running phase: ${phase}"

  # Scenario A: short context
  local short_message="Summarize my spending in 4 bullets and suggest one savings action."

  # Scenario B: medium context
  local medium_message="Analyze spending categories for last month, highlight top recurring costs, and recommend 3 realistic budget adjustments with rationale."

  # Scenario C: long context
  local long_message="Analyze my spending behavior using available transaction patterns, identify recurring categories, compare likely month-over-month behavior, flag non-essential expenses, and propose a practical budget optimization plan with prioritized steps, expected impact, and a short action checklist for next 30 days."

  for run_no in $(seq 1 "$RUNS_PER_SCENARIO"); do
    post_chat "$phase" "A-short" "$run_no" "$short_message"
    post_chat "$phase" "B-medium" "$run_no" "$medium_message"
    post_chat "$phase" "C-long" "$run_no" "$long_message"
  done
}

build_summary_csv() {
  python3 - <<'PY' "$RAW_CSV" "$SUMMARY_CSV"
import csv, sys, math
from collections import defaultdict

raw_path=sys.argv[1]
out_path=sys.argv[2]

groups=defaultdict(list)

with open(raw_path, newline='') as f:
    r=csv.DictReader(f)
    for row in r:
        if row.get("status_code") != "200":
            continue
        key=(row["phase"], row["scenario"])
        groups[key].append({
          "measured_input": float(row["measured_input_tokens"]),
          "measured_output": float(row["measured_output_tokens"]),
          "measured_total": float(row["measured_total_tokens"]),
          "exact_input": float(row["exact_input_tokens"] or 0),
          "exact_output": float(row["exact_output_tokens"] or 0),
          "exact_total": float(row["exact_total_tokens"] or 0),
          "estimated_input": float(row["estimated_input_tokens"]),
          "estimated_output": float(row["estimated_output_tokens"]),
          "estimated_total": float(row["estimated_total_tokens"]),
            "cost": float(row["cost_per_request"]),
            "lat": float(row["latency_ms"]),
        })

with open(out_path, 'a', newline='') as f:
    w=csv.writer(f)
    for (phase, scenario), rows in sorted(groups.items()):
        runs=len(rows)
        avg_measured_in=sum(x["measured_input"] for x in rows)/runs
        avg_measured_out=sum(x["measured_output"] for x in rows)/runs
        avg_measured_total=sum(x["measured_total"] for x in rows)/runs
        avg_exact_in=sum(x["exact_input"] for x in rows)/runs
        avg_exact_out=sum(x["exact_output"] for x in rows)/runs
        avg_exact_total=sum(x["exact_total"] for x in rows)/runs
        avg_estimated_in=sum(x["estimated_input"] for x in rows)/runs
        avg_estimated_out=sum(x["estimated_output"] for x in rows)/runs
        avg_estimated_total=sum(x["estimated_total"] for x in rows)/runs
        avg_cost=sum(x["cost"] for x in rows)/runs
        lats=sorted(x["lat"] for x in rows)
        if runs == 1:
            p95=lats[0]
        else:
            idx=max(0, math.ceil(0.95*runs)-1)
            p95=lats[idx]

        w.writerow([
            phase,
            scenario,
            runs,
            f"{avg_measured_in:.2f}",
            f"{avg_measured_out:.2f}",
            f"{avg_measured_total:.2f}",
            f"{avg_exact_in:.2f}",
            f"{avg_exact_out:.2f}",
            f"{avg_exact_total:.2f}",
            f"{avg_estimated_in:.2f}",
            f"{avg_estimated_out:.2f}",
            f"{avg_estimated_total:.2f}",
            f"{avg_cost:.8f}",
            f"{p95:.2f}",
        ])
PY
}

print_outputs() {
  echo
  echo "=== Compact Summary CSV ==="
  cat "${SUMMARY_CSV}"
  echo
  echo "Files generated:"
  echo "- ${RAW_CSV}"
  echo "- ${SUMMARY_CSV}"
  echo "- ${QUALITY_CSV}"
  echo "- ${RESPONSES_DIR}/*.json"
  if [[ -f "${HOST_LOG}" ]]; then
    echo "- ${HOST_LOG}"
  fi
}

main() {
  init_csvs
  start_host_if_needed

  run_phase "${PHASE_BASELINE}"

  echo
  echo "Apply one optimization change now (prompt trimming, context pruning, etc.), then press Enter to continue..."
  read -r _

  run_phase "${PHASE_OPTIMIZED}"
  build_summary_csv
  print_outputs
}

main "$@"
