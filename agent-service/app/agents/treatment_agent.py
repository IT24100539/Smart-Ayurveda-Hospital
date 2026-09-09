def run_treatment_agent(prompt: str, context: dict[str, str]) -> str:
    dosha = context.get("vikriti") or context.get("dosha") or "unspecified dosha"
    return (
        f"Treatment advisor ({dosha}): consider abhyanga, snehana, or shirodhara only after "
        f"physician review. This is decision support, not a prescription. Prompt: {prompt[:280]}"
    )
