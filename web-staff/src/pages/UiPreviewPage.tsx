import { useState } from "react";
import { ThemeToggle } from "../components/ThemeToggle";
import { Button } from "../components/ui/Button";
import { EmptyState } from "../components/ui/EmptyState";
import { ErrorState } from "../components/ui/ErrorState";
import { Input } from "../components/ui/Field";
import {
  BookingOption,
  ChatPanel,
  InfoCard,
  KpiCard,
  SectionRow,
  StatusPill,
  Stepper,
  TextLink,
  UnderlineTabs
} from "../components/ui/recipes";

const tabs = [
  { id: "upcoming", label: "Upcoming", count: 2 },
  { id: "therapy", label: "Therapy", count: 1 },
  { id: "record", label: "Registration", count: 1 }
];

/** Dev-only catalogue of shared components. Not linked from the staff navigation. */
export function UiPreviewPage() {
  const [tab, setTab] = useState("upcoming");
  return (
    <main className="mx-auto max-w-5xl space-y-8 px-4 py-8">
      <div className="flex items-center justify-between gap-4">
        <h1>Component preview</h1>
        <ThemeToggle />
      </div>
      <p className="text-muted">Samples only. These are not hospital records.</p>
      <SectionRow title="Status" action={<TextLink>Open</TextLink>} />
      <div className="flex flex-wrap gap-2">
        {["Approved", "Pending", "Rejected", "Cancelled", "Completed"].map((status) => (
          <StatusPill key={status} status={status} />
        ))}
      </div>
      <UnderlineTabs tabs={tabs} selected={tab} onSelect={setTab} />
      <InfoCard
        kicker="Therapy session"
        title="Sample therapy"
        status="Pending"
        facts={[
          { label: "Date", value: "From the appointment" },
          { label: "Time", value: "From the appointment" }
        ]}
        action={<Button variant="ghost">Reschedule</Button>}
      />
      <Stepper labels={["Therapy", "Date", "Time", "Confirm"]} current={1} />
      <div className="grid gap-5 md:grid-cols-2">
        <BookingOption
          name="Sample therapy"
          price="From catalogue"
          description="Name, price and duration come from the treatments API."
          duration="From catalogue"
          category="Therapy"
          selected
        />
        <KpiCard label="Loaded count" value="—" />
      </div>
      <ChatPanel />
      <Input aria-label="Sample field" placeholder="Pill field" />
      <EmptyState title="Nothing here" description="Empty states keep a title, a short line, and an action." />
      <ErrorState message="Could not load this sample." onRetry={() => undefined} />
    </main>
  );
}
