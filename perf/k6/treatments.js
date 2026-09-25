import http from "k6/http";
import { check } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:5000";

export const options = {
  vus: 50,
  duration: "30s",
  thresholds: {
    http_req_failed: ["rate==0"],
    http_req_duration: ["p(95)<500"]
  }
};

export default function () {
  const response = http.get(`${baseUrl}/api/treatments?page=1&pageSize=20`);
  check(response, {
    "treatments list is 200": (res) => res.status === 200
  });
  if (response.status !== 200) {
    console.log(`status=${response.status} error=${response.error} body=${String(response.body).slice(0, 180)}`);
  }
}
