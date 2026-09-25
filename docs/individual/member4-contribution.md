# Member 4 — Feedback & Communication

Branch: `member4/feedback-communication`

## Owned component

Patient and staff communication after a visit: feedback on an appointment or treatment, complaints, replies, reactions, and notifications.

The public API (`Hospital.Api`) owns the write path. Patients submit and edit feedback (rating, comment, optional anonymity, link to a completed visit). Staff moderate visibility, answer through replies, and handle complaints. A reply drafted by the internal agent is stored as a draft; staff review it before it is posted. Sentiment and category are filled after analysis, not at submit time.

The localhost-only feedback support agent (`agent-service`) returns the draft and the analysis. It does not publish a reply. The API calls it through `IAgentClient` with the internal service key. Architecture is below.

## Feedback & support agent architecture

Entry point: `POST /internal/agents/feedback-support` on `agent-service`, implemented in `app/agents/feedback_support_agent.py`. Request contract is `FeedbackAgentRequest` (`feedback_id`, `comment_text`, `patient_id`). Response contract is `FeedbackAgentResponse` (sentiment, category, priority, similar count, optional draft, `draft_skipped`, `workflow_id`, `status`, `immediate_dashboard_alert`, optional `refusal_reason`). Status is always `awaiting_review`. Staff approve or edit a draft in `Hospital.Api`; this agent never posts a reply.

### Node flow

LangGraph runs five nodes in a fixed order. There is no model-chosen branch.

1. **analyze** — `analyze_sentiment`, `categorize`, and `get_feedback` run together. Sentiment is Positive, Neutral, or Negative. Category is TreatmentQuality, WaitingTime, StaffService, FacilityIssue, or Other. Hospital context (rating, anonymity) is loaded when the internal API can be read.
2. **check_similar** — `check_similar_feedback` counts other patients' recent feedback in the same category. If category is missing, or the hospital API fails, the count is left unset and the graph continues.
3. **flag** — `flag_priority` sets High or Normal. No model call.
4. **draft** — `draft_reply` writes 2–4 sentences for staff review when sentiment and category are both set. A model failure skips the draft and keeps the earlier analysis.
5. **awaiting_review** — terminal node. The caller stores any draft.

### `flag_priority` rule

Plain Python in `app/tools/tools.py`. It does not call Ollama.

- **High** (and `immediate_dashboard_alert=true`) when sentiment is Negative **and** category is StaffService, **or** when `similar_feedback_count >= 2`. A repeated category is High even when sentiment is Neutral.
- **Normal** otherwise (for example Negative + WaitingTime with count 1).
- If sentiment, category, and the similar-count are all missing, the flag node leaves priority unset and does not raise an alert.

### Design decision: `flag_priority` logic

The original spec language can be read as High only when sentiment is Negative **and** (StaffService **or** `similar_feedback_count >= 2`). The implemented rule is `(Negative AND StaffService) OR (similar_feedback_count >= 2)`. A repeated category across patients is treated as independently worth staff attention, even when a single comment is labelled Neutral or Positive. Sentiment from a small local model is noisy and is not used as a gate that would hide a real recurring problem.

### Ollama failure at each step

`ollama_complete` turns a timeout or HTTP error into `OllamaCallError`. Each model step catches that and continues; the workflow still ends at `awaiting_review`.

| Step | On Ollama failure or timeout |
| --- | --- |
| analyze (`analyze_sentiment`) | Sentiment is left unset (`None`). A malformed label is retried once, then defaults to Neutral. |
| analyze (`categorize`) | Category is left unset (`None`). A malformed label is retried once, then defaults to Other. |
| check_similar | No model call. A hospital-API error leaves the count unset. |
| flag | No model call. If analyze left sentiment and category unset and there is no similar-count, priority stays unset. |
| draft (`draft_reply`) | Reply is skipped (`draft_skipped=true`, `suggested_reply=null`). Sentiment, category, and priority from earlier nodes stay in the response. |

### Tool allow-list

`ALLOWED_TOOLS` in `feedback_support_agent.py` is a closed set: `analyze_sentiment`, `categorize`, `get_feedback`, `check_similar_feedback`, `flag_priority`, `draft_reply`. Each of those six is wrapped so wrap-time and every later call go through `require_allowed_tool`. A name that is not on the list (including helpers such as `ollama_complete` or `fetch_hospital`) raises `DisallowedToolError`. The model is not given tools.

### Prompt-injection guard

Before any comment is placed in `SENTIMENT_PROMPT`, `CATEGORY_PROMPT`, or `DRAFT_PROMPT`, `injection_reason` runs a deterministic heuristic (no model call). It flags phrases that tell the model to ignore or disregard previous instructions, mention the system prompt, skip validation, change its role, or include tool-result-looking JSON.

A match is logged. The graph is not started. Classification and drafting are not called. The response is `draft_skipped=true` with `refusal_reason` set, and sentiment, category, and priority left unset.

Comments that do go to the model sit between `<<<USER_COMMENT>>>` and `<<<END_USER_COMMENT>>>`, with an instruction that the text inside is user-submitted content to analyze, not instructions to follow.

## Scope note (viva)

The early component sketch for this seat was Billing. The group replaced it with Feedback & Communication.

This system is a free provincial Ayurvedic hospital service. Patients are not invoiced, and there is no payment, insurance, or charge workflow in scope, so a billing component does not fit the service. Feedback, complaints, and staff replies do: a patient can respond to a consultation, panchakarma session, or nadi pariksha visit, and staff can moderate and answer.

That replacement is the assignment already recorded for member 4 in [ADR 0009](../adr/0009-git-branching.md) (Accepted, 2026-09-09), which names `member4/feedback-communication` in the branching model. It is a documented group decision, not an unapproved scope change by this member.

## Key commits

From `git log member4/feedback-communication --oneline`.

| Hash | Date | Subject |
| --- | --- | --- |
| `29c63f3` | 2026-09-10 | chore(foundation): solution scaffold, auth, CI, React/Flutter skeletons, agent-service skeleton, ADRs |
| `0858e9b` | 2026-09-09 | Initial commit: shared hospital foundation for main/develop workflow. |

## AI usage log

| Date | Tool | Task | What I kept | What I changed or rejected |
| --- | --- | --- | --- | --- |
| 2026-09-25 | Cursor | Close the feedback widget gap and record this contribution from the source tree | The five-node feedback graph, draft-until-staff-post rule, and injection guard already in `feedback_support_agent.py` | No invented earlier tool sessions. The log above this row was empty in the repo. |

## Reflection

### What I did myself

Feedback, complaints, replies, reactions, and notifications are implemented on `Hospital.Api`. A patient submits a rating and comment for a completed visit. Staff moderate visibility, post replies, and escalate complaints. An agent draft stays unpublished until staff approve it.

### Where AI helped

The 2026-09-25 pass used Cursor to compare the Flutter feedback form with `feedback_widgets_test.dart` and to fill this record from the files already in the branch.

### Difficulties

The name preview was below the test viewport, so the anonymous switch did not receive the tap until the test scrolled it into view. Sentiment from the local model is noisy, so a repeated category is flagged High even when a single comment is Neutral.

### What I would do differently

Keep the feedback form in a scroll view that still builds every field, and scroll widget tests to the control they tap.
