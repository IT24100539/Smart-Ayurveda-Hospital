import http from "k6/http";
import { check } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:5000";
const email = __ENV.ADMIN_EMAIL || "admin@smartayurveda.local";
const password = __ENV.ADMIN_PASSWORD || "ChangeMe!Admin1";
const racers = 20;

export const options = {
  vus: 1,
  iterations: 1,
  thresholds: {
    checks: ["rate==1"]
  }
};

function headers(token) {
  return {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json"
    }
  };
}

function login() {
  const response = http.post(
    `${baseUrl}/api/auth/login`,
    JSON.stringify({ email, password }),
    { headers: { "Content-Type": "application/json" } }
  );
  if (response.status !== 200) {
    throw new Error(`Admin login failed: ${response.status} ${response.body}`);
  }
  return response.json("token");
}

function hhmm(value) {
  return String(value).slice(0, 5);
}

function uniqueMonday() {
  const start = new Date(Date.UTC(2027, 0, 4));
  const weeks = Math.floor(Date.now() / 60000) % 520;
  start.setUTCDate(start.getUTCDate() + weeks * 7);
  return start.toISOString().slice(0, 10);
}

function findLastSlot(token) {
  const list = http.get(`${baseUrl}/api/treatments?page=1&pageSize=50&name=Abhyanga`, headers(token));
  const treatmentId = list.json("items.0.id");
  if (!treatmentId) throw new Error("Abhyanga treatment was not found.");
  const detail = http.get(`${baseUrl}/api/treatments/${treatmentId}`, headers(token));
  const schedule = detail.json("schedule").find(
    (entry) => entry.dayOfWeek === "Monday" && hhmm(entry.startTime) === "09:00" && entry.maxSlotsPerDay > 1
  );
  if (!schedule) throw new Error("Monday 09:00 Abhyanga schedule was not found.");
  return {
    treatmentId,
    scheduleId: schedule.id,
    timeSlot: `${hhmm(schedule.startTime)}-${hhmm(schedule.endTime)}`,
    maxSlots: schedule.maxSlotsPerDay
  };
}

function ensurePatients(token, needed) {
  const listed = http.get(`${baseUrl}/api/patients?page=1&pageSize=100`, headers(token));
  const ids = listed.json("items").map((patient) => patient.id);
  let created = 0;
  while (ids.length < needed) {
    created += 1;
    const phone = `078${String(Date.now()).slice(-7)}${String(created).padStart(2, "0")}`.slice(0, 12);
    const response = http.post(
      `${baseUrl}/api/patients`,
      JSON.stringify({
        firstName: "Perf",
        lastName: `Racer${created}`,
        dateOfBirth: "1990-01-15",
        gender: "Female",
        phone,
        email: null,
        address: "Colombo",
        bloodGroup: null,
        allergies: null,
        prakriti: "Vata",
        vikriti: "None"
      }),
      headers(token)
    );
    if (response.status !== 201) {
      throw new Error(`Create patient failed: ${response.status} ${response.body}`);
    }
    ids.push(response.json("id"));
  }
  return ids.slice(0, needed);
}

function book(token, slot, patientId, date) {
  return http.post(
    `${baseUrl}/api/appointments`,
    JSON.stringify({
      patientId,
      treatmentId: slot.treatmentId,
      scheduleId: slot.scheduleId,
      requestedDate: date,
      requestedTimeSlot: slot.timeSlot
    }),
    headers(token)
  );
}

export function setup() {
  const token = login();
  const slot = findLastSlot(token);
  const date = uniqueMonday();
  const patients = ensurePatients(token, slot.maxSlots - 1 + racers);
  const fillers = patients.slice(0, slot.maxSlots - 1);
  const contenders = patients.slice(slot.maxSlots - 1);
  for (const patientId of fillers) {
    const response = book(token, slot, patientId, date);
    if (response.status !== 201) {
      throw new Error(`Prefill failed: ${response.status} ${response.body}`);
    }
  }
  return { token, slot, date, contenders };
}

export default function (data) {
  const responses = http.batch(
    data.contenders.map((patientId) => [
      "POST",
      `${baseUrl}/api/appointments`,
      JSON.stringify({
        patientId,
        treatmentId: data.slot.treatmentId,
        scheduleId: data.slot.scheduleId,
        requestedDate: data.date,
        requestedTimeSlot: data.slot.timeSlot
      }),
      headers(data.token)
    ])
  );
  const created = responses.filter((response) => response.status === 201).length;
  const conflicts = responses.filter((response) => response.status === 409).length;
  check(responses, {
    "exactly one booking wins the last slot": () => created === 1,
    "the other nineteen attempts conflict": () => conflicts === racers - 1
  });
}
