import { useState, type ImgHTMLAttributes } from "react";

/** Decorative or content image that collapses to a muted panel if the file is missing. */
export function SafeImage({
  src,
  alt,
  className,
  onError,
  ...rest
}: ImgHTMLAttributes<HTMLImageElement>) {
  const [failed, setFailed] = useState(false);

  if (failed || !src) {
    return (
      <div
        className={["bg-primary-muted", className].filter(Boolean).join(" ")}
        role={alt ? "img" : undefined}
        aria-label={alt || undefined}
        aria-hidden={alt ? undefined : true}
      />
    );
  }

  return (
    <img
      src={src}
      alt={alt}
      className={className}
      onError={(event) => {
        setFailed(true);
        onError?.(event);
      }}
      {...rest}
    />
  );
}
