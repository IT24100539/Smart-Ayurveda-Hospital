import type { InputHTMLAttributes, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";

/** Shared look for text inputs, selects and textareas (defined in src/index.css). */
export const fieldClass = "field";

function join(className: string | undefined): string {
  return [fieldClass, className].filter(Boolean).join(" ");
}

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={join(className)} {...props} />;
}

export function Select({ className, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={join(className)} {...props} />;
}

export function Textarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={join(className)} {...props} />;
}
