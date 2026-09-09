def run_appointment_agent(prompt: str, context: dict[str, str]) -> str:
    doctor = context.get("doctorName") or "the assigned physician"
    return (
        f"Appointment assistant: suggest the next available 30-minute consultation with {doctor}. "
        f"Confirm patient UHID before booking. Prompt: {prompt[:280]}"
    )
