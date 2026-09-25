import http from "k6/http";

const agentUrl = __ENV.AGENT_URL || "http://127.0.0.1:8100";
const secret = __ENV.AGENT_SECRET || "dev-internal-agent-secret";
const calls = 10;

export const options = {
  vus: 1,
  iterations: 1
};

export default function () {
  const durations = [];
  let successes = 0;
  for (let i = 0; i < calls; i += 1) {
    const response = http.post(
      `${agentUrl}/internal/agents/scheduling-bed`,
      JSON.stringify({
        patient_id: "6df22187-9488-4000-bbe0-7b3b3b53259b",
        treatment_id: "19415cdb-746a-4729-8349-02bc4e632cf7",
        ward_id: "2d177995-6c4c-44b9-bee8-c19ec9071932",
        objective_text: "Admit the patient for inpatient panchakarma.",
        preferred_date: "2026-10-20"
      }),
      {
        headers: {
          "Content-Type": "application/json",
          "X-Internal-Secret": secret
        },
        timeout: "120s"
      }
    );
    durations.push(response.timings.duration);
    if (response.status === 200) successes += 1;
    console.log(`call ${i + 1} status=${response.status} ms=${response.timings.duration.toFixed(1)}`);
  }
  const min = Math.min(...durations);
  const max = Math.max(...durations);
  const avg = durations.reduce((sum, value) => sum + value, 0) / durations.length;
  console.log(
    `scheduling-bed latency ms min=${min.toFixed(1)} avg=${avg.toFixed(1)} max=${max.toFixed(1)} successes=${successes}/${calls}`
  );
}
