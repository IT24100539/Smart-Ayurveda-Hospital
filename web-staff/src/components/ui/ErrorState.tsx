import { Badge } from "./Badge";
import { Button } from "./Button";

type ErrorStateProps = {
  message: string;
  onRetry?: () => void;
};

export function ErrorState({ message, onRetry }: ErrorStateProps) {
  return (
    <div className="rounded-card border border-status-error-fg/30 bg-status-error-bg px-6 py-8 text-center" role="alert">
      <Badge tone="error">Error</Badge>
      <p className="mt-3 text-sm text-status-error-fg">{message}</p>
      {onRetry ? (
        <Button variant="secondary" className="mt-4" onClick={onRetry}>
          Try again
        </Button>
      ) : null}
    </div>
  );
}
