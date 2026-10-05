import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusPill, UnderlineTabs } from "./recipes";

describe("shared recipes", () => {
  it("status pills include an icon and the status text", () => {
    render(<StatusPill status="Approved" />);
    expect(screen.getByText("Approved")).toBeInTheDocument();
    expect(document.querySelector("svg")).not.toBeNull();
  });

  it("underline tabs expose the selected tab", () => {
    render(
      <UnderlineTabs
        tabs={[
          { id: "a", label: "Upcoming", count: 2 },
          { id: "b", label: "Therapy", count: 0 }
        ]}
        selected="a"
        onSelect={() => undefined}
      />
    );
    expect(screen.getByRole("tab", { name: /Upcoming/ })).toHaveAttribute("aria-selected", "true");
    expect(screen.getByRole("tab", { name: /Therapy/ })).toHaveAttribute("aria-selected", "false");
  });
});
