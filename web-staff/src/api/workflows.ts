import { api } from "./client";

export type ApprovalStatus =
  | "Pending"
  | "Approved"
  | "Rejected"
  | "RevisionRequested"
  | "NotRequired";

export type FinalOutcome = "Success" | "SafeFailure" | "InProgress";

export type ToolResult = {
  tool?: string;
  succeeded?: boolean;
  output?: Record<string, unknown>;
  error?: string | null;
  calledAt?: string;
  called_at?: string;
};

export type ValidationResult = {
  check?: string;
  passed?: boolean;
  detail?: string;
};

export type WorkflowExecution = {
  id: string;
  agentName: string;
  objectiveText: string;
  plan: string[];
  completedSteps: string[];
  toolResults: ToolResult[];
  validationResults: ValidationResult[];
  errors: string[] | null;
  approvalStatus: ApprovalStatus;
  finalOutcome: FinalOutcome;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  createdAt: string;
  updatedAt: string;
};

export type WorkflowPage = {
  items: WorkflowExecution[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type WorkflowFilters = {
  agentName: string;
  approvalStatus: ApprovalStatus | "";
};

export function listWorkflows(filters: WorkflowFilters): Promise<WorkflowPage> {
  const params = new URLSearchParams({ sort: "-updatedAt", page: "1", pageSize: "50" });
  if (filters.agentName) params.set("agentName", filters.agentName);
  if (filters.approvalStatus) params.set("approvalStatus", filters.approvalStatus);
  return api.request<WorkflowPage>(`/workflow-executions?${params.toString()}`);
}

export function getWorkflow(id: string): Promise<WorkflowExecution> {
  return api.request<WorkflowExecution>(`/agent-workflows/${id}`);
}

export function decideWorkflow(
  id: string,
  decision: "Approve" | "Reject" | "RevisionRequested"
): Promise<WorkflowExecution> {
  return api.request<WorkflowExecution>(`/agent-workflows/${id}/approve`, {
    method: "PATCH",
    body: JSON.stringify({
      approve: decision === "Approve",
      decision: decision === "RevisionRequested" ? "RevisionRequested" : null
    })
  });
}
