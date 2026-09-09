def run_intake_agent(prompt: str, context: dict[str, str]) -> str:
    name = context.get("patientName") or context.get("patient_name") or "the patient"
    return (
        f"Intake notes for {name}: capture prakriti, current vikriti, agni, nidra, and "
        f"chief complaint. Prompt received: {prompt[:280]}"
    )
