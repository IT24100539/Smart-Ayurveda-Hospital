import { useEffect, useState, type FormEvent } from "react";
import { api, type Patient } from "../api/client";

export function PatientsPage() {
  const [items, setItems] = useState<Patient[]>([]);
  const [query, setQuery] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [phone, setPhone] = useState("");

  async function load(q = query) {
    try {
      const page = await api.patients(q);
      setItems(page.items);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load patients");
    }
  }

  useEffect(() => {
    void load("");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function onCreate(event: FormEvent) {
    event.preventDefault();
    try {
      await api.createPatient({
        firstName,
        lastName,
        dateOfBirth: "1990-01-01",
        gender: 1,
        phone,
        email: null,
        address: null,
        bloodGroup: null,
        allergies: null,
        prakriti: 0,
        vikriti: 0
      });
      setFirstName("");
      setLastName("");
      setPhone("");
      await load("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create patient");
    }
  }

  return (
    <section>
      <h1>Patients</h1>
      <div className="toolbar">
        <input placeholder="Search UHID, name, or phone" value={query} onChange={(e) => setQuery(e.target.value)} />
        <button className="btn btn-primary" style={{ width: "auto", marginTop: 0 }} type="button" onClick={() => void load()}>
          Search
        </button>
      </div>
      {error ? <p className="error">{error}</p> : null}
      <form className="toolbar" onSubmit={onCreate}>
        <input placeholder="First name" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
        <input placeholder="Last name" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
        <input placeholder="Phone" value={phone} onChange={(e) => setPhone(e.target.value)} required />
        <button className="btn btn-primary" style={{ width: "auto", marginTop: 0 }} type="submit">Register</button>
      </form>
      <table className="table">
        <thead>
          <tr><th>UHID</th><th>Name</th><th>Phone</th></tr>
        </thead>
        <tbody>
          {items.map((p) => (
            <tr key={p.id}>
              <td>{p.uhid}</td>
              <td>{p.firstName} {p.lastName}</td>
              <td>{p.phone}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
