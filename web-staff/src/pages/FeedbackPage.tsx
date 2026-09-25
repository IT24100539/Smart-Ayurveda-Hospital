import { useState } from "react";
import { ComplaintQueuePage } from "./feedback/ComplaintQueuePage";
import { FeedbackDashboardPage } from "./feedback/FeedbackDashboardPage";

type Tab = "feedback" | "complaints";

export function FeedbackPage() {
  const [tab, setTab] = useState<Tab>("feedback");

  return (
    <div className="mx-auto max-w-7xl">
      <section className="relative overflow-hidden rounded-2xl bg-primary-dark text-white shadow-lg">
        <img
          src="/images/feedback-note.png"
          alt="Notebook, lotus, and herbal tea on the feedback desk"
          className="h-44 w-full object-cover sm:h-52"
        />
        <div className="pointer-events-none absolute bottom-0 left-0 h-1 w-full bg-gradient-to-r from-amber-300/80 via-amber-200/30 to-transparent" aria-hidden="true" />
        <div className="relative p-6 md:p-8">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-primary-muted">After the visit</p>
          <h1 className="mt-2 text-3xl text-white">Feedback</h1>
          <p className="mt-3 max-w-2xl text-sm leading-6 text-primary-muted">
            Review comments on care, answer them yourself or with an approved draft, and follow complaints.
          </p>
        </div>
      </section>
      <div className="mt-6 flex gap-2" role="tablist" aria-label="Feedback sections">
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
