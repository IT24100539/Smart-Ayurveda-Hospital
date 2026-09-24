import { useState } from "react";
import { ComplaintQueuePage } from "./feedback/ComplaintQueuePage";
import { FeedbackDashboardPage } from "./feedback/FeedbackDashboardPage";

type Tab = "feedback" | "complaints";

export function FeedbackPage() {
  const [tab, setTab] = useState<Tab>("feedback");

  return (
    <div>
      <h1>Feedback</h1>
      <p className="mt-1 max-w-2xl text-sm text-muted">
        Review comments on care, answer them yourself or with an approved draft, and follow complaints.
      </p>
      <div className="mt-4 flex gap-2" role="tablist" aria-label="Feedback sections">
        <button
          type="button"
          role="tab"
          id="tab-feedback"
          aria-selected={tab === "feedback"}
          aria-controls="feedback-panel"
          className={tabClass(tab === "feedback")}
          onClick={() => setTab("feedback")}
        >
          Patient feedback
        </button>
        <button
          type="button"
          role="tab"
          id="tab-complaints"
          aria-selected={tab === "complaints"}
          aria-controls="feedback-panel"
          className={tabClass(tab === "complaints")}
          onClick={() => setTab("complaints")}
        >
          Complaint queue
        </button>
      </div>
      <div className="mt-6" role="tabpanel" id="feedback-panel" aria-labelledby={tab === "feedback" ? "tab-feedback" : "tab-complaints"}>
        {tab === "feedback" ? <FeedbackDashboardPage /> : <ComplaintQueuePage />}
      </div>
    </div>
  );
}

function tabClass(selected: boolean): string {
  return [
    "rounded-lg px-3 py-1.5 text-sm font-semibold",
    selected ? "bg-primary text-white" : "border border-surface-border bg-white text-primary hover:bg-primary-muted"
  ].join(" ");
}
