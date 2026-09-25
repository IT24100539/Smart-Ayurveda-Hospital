# Performance report

Measured on 25 September 2026 against the local Hospital API at `http://127.0.0.1:5000` and the agent service at `http://127.0.0.1:8100`. Tooling is k6 2.2.0. Scripts are in `perf/k6/`.

## Concurrent reads — GET /api/treatments

`perf/k6/treatments.js`: 50 virtual users for 30 seconds. Pass bar: p95 under 500 ms and an HTTP error rate of 0%.

| Metric | Result |
| --- | --- |
| Requests | 2064 |
| Throughput | 59.6 req/s |
| Median latency | 115 ms |
| p90 | 548 ms |
| p95 | 2.48 s |
| Average | 715 ms |
| Max | 19.0 s |
| HTTP errors | 1.16% (24 / 2064) |

The pass bar was not met. A repeat of the same script, after the booking race below, was worse: 929 requests, p95 15.8 s, 4.41% HTTP 500 (`An unexpected error occurred.`). Median stayed under 100 ms, so the failure is the tail under 50 concurrent readers, not the typical response.

## Double-booking race — last slot, 20 parallel attempts

`perf/k6/double-booking.js` books the seeded Abhyanga Monday `09:00-10:00` schedule. Setup fills every seat except one on a fresh Monday, using distinct patients, then fires 20 `POST /api/appointments` calls together.

| Outcome | Count |
| --- | --- |
| Created (201) | 1 |
| Conflict (409) | 19 |

Both checks passed: exactly one booking took the last seat, and the other nineteen were rejected. The transaction guard (`LOCK TABLE appointments` inside `TryAddWithinCapacityAsync`) held at 20-way concurrency, beyond the earlier two-request unit test. k6 reports those 409s inside `http_req_failed`; that counter is the expected rejection, not a failed assertion.

## Scheduling agent latency — POST /internal/agents/scheduling-bed

`perf/k6/scheduling-agent.js`: 10 sequential calls, preferred date `2026-10-20`. Every call returned HTTP 500. A probe of Ollama at `http://127.0.0.1:11434` did not connect, so these times are the failure path, not a completed plan.

| Metric | Milliseconds |
| --- | --- |
| Min | 7105 |
| Average | 9427 |
| Max | 10808 |

For a live demo, leave at least 11 seconds after starting a scheduling-bed run before expecting a result on this machine. A successful generation on CPU would be slower than this failure path.

## How to repeat

```text
k6 run perf/k6/treatments.js -e BASE_URL=http://127.0.0.1:5000
k6 run perf/k6/double-booking.js -e BASE_URL=http://127.0.0.1:5000
k6 run perf/k6/scheduling-agent.js -e AGENT_URL=http://127.0.0.1:8100
```
