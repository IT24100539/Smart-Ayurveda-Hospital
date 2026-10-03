type SkeletonProps = {
  className?: string;
};

/** Placeholder block. Decorative: pair it with a labelled role="status" container. */
export function Skeleton({ className = "h-4 w-full" }: SkeletonProps) {
  return <div aria-hidden="true" className={["animate-pulse rounded-md bg-neutral-200", className].join(" ")} />;
}

export function SkeletonText({ lines = 3 }: { lines?: number }) {
  return (
    <div className="space-y-2" aria-hidden="true">
      {Array.from({ length: lines }, (_, index) => (
        <Skeleton key={index} className={index === lines - 1 ? "h-4 w-2/3" : "h-4 w-full"} />
      ))}
    </div>
  );
}
