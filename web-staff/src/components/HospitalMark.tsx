import { useEffect, useRef, useState } from "react";

type MarkProps = {
  className?: string;
};

const LEAF_SLIDES = [
  { src: "/images/ayurveda-courtyard.png", label: "Courtyard" },
  { src: "/images/ayurveda-ward.png", label: "Ward" },
  { src: "/images/consultation-desk.png", label: "Consultation" },
  { src: "/images/herbal-oils.png", label: "Herbal oils" },
  { src: "/images/feedback-note.png", label: "Patient note" },
  { src: "/images/herbal-garden.png", label: "Herb garden" },
  { src: "/images/panchakarma-room.png", label: "Panchakarma" },
  { src: "/images/herb-pharmacy.png", label: "Pharmacy" }
] as const;

/** Round tulsi mark used in the staff sidebar and on the sign-in panel. */
export function HospitalLogo({ className = "h-11 w-11" }: MarkProps) {
  return (
    <svg viewBox="0 0 64 64" className={className} role="img" aria-label="Smart Ayurveda">
      <circle cx="32" cy="32" r="30" fill="#f4efe6" />
      <circle cx="32" cy="32" r="27" fill="none" stroke="#c6a15a" strokeWidth="1.4" />
      <path
        d="M32 50c0 0-14-12-10.5-24.5C24.2 16.8 32 13 32 13s7.8 3.8 10.5 12.5C46 38 32 50 32 50Z"
        fill="#0f5c5b"
      />
      <path
        d="M32 46c0 0-8.5-8.2-6.2-16.8C27.4 22.6 32 19.5 32 19.5s4.6 3.1 6.2 9.7C40.5 37.8 32 46 32 46Z"
        fill="#1a7573"
      />
      <path d="M32 46.5V16" fill="none" stroke="#f4efe6" strokeWidth="1.2" strokeLinecap="round" />
      <path d="M32 28c-3.2-1.2-5.2-2.2-6.4-3.6M32 34c3.1-1.1 5-2 6.2-3.4M32 22c-2.4-.6-4-1.2-5-2" fill="none" stroke="#e7d7a8" strokeWidth="0.9" strokeLinecap="round" />
      <path d="M20 18c2.4 1.6 4.2 1.2 6-0.4M44 18c-2.4 1.6-4.2 1.2-6-0.4" fill="none" stroke="#0a4342" strokeWidth="1.1" strokeLinecap="round" />
    </svg>
  );
}

/** Hospital photos cycling inside the sidebar leaf. */
export function LeafImageSlide() {
  const [index, setIndex] = useState(0);
  const boxRef = useRef<HTMLDivElement>(null);
  const measureRef = useRef<HTMLParagraphElement>(null);
  const [fontSize, setFontSize] = useState(48);
  const [wordHeight, setWordHeight] = useState(72);

  useEffect(() => {
    const media = window.matchMedia("(prefers-reduced-motion: reduce)");
    if (media.matches) return;
    const timer = window.setInterval(() => {
      setIndex((current) => (current + 1) % LEAF_SLIDES.length);
    }, 4000);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    const box = boxRef.current;
    const measure = measureRef.current;
    if (!box || !measure) return;

    const fit = () => {
      measure.style.fontSize = "10px";
      const width = measure.scrollWidth;
      if (width === 0) return;
      const size = (10 * box.clientWidth) / width;
      measure.style.fontSize = `${size}px`;
      setFontSize(size);
      setWordHeight(measure.getBoundingClientRect().height * 1.42);
    };

    fit();
    const observer = new ResizeObserver(fit);
    observer.observe(box);
    return () => observer.disconnect();
  }, []);

  const slide = LEAF_SLIDES[index];

  return (
    <div ref={boxRef} className="relative">
      <p
        ref={measureRef}
        aria-hidden="true"
        className="pointer-events-none absolute whitespace-nowrap font-display font-bold leading-none opacity-0"
      >
        HEALTH
      </p>
      <div className="relative w-full overflow-hidden" style={{ height: wordHeight }}>
        <p
          aria-hidden="true"
          className="absolute inset-0 flex items-center justify-center font-display font-bold leading-none text-transparent"
          style={{ fontSize, transform: "scaleY(1.42)", WebkitTextStroke: "1.25px #e7d7a8" }}
        >
          HEALTH
        </p>
        {LEAF_SLIDES.map((item, itemIndex) => (
          <p
            key={item.src}
            aria-hidden="true"
            className="absolute inset-0 flex items-center justify-center bg-cover bg-center font-display font-bold leading-none text-transparent transition-opacity duration-700"
            style={{
              fontSize,
              transform: "scaleY(1.42)",
              backgroundImage: `url(${item.src})`,
              WebkitBackgroundClip: "text",
              backgroundClip: "text",
              opacity: itemIndex === index ? 1 : 0
            }}
          >
            HEALTH
          </p>
        ))}
      </div>
      <p className="sr-only">HEALTH, {slide.label}</p>
      <div className="mt-2 flex items-center justify-between gap-2 px-1">
        <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-[#e7d7a8]">{slide.label}</p>
        <div className="flex gap-1" role="group" aria-label="Hospital photos">
          {LEAF_SLIDES.map((item, itemIndex) => (
            <button
              key={item.src}
              type="button"
              aria-label={item.label}
              aria-current={itemIndex === index ? "true" : undefined}
              onClick={() => setIndex(itemIndex)}
              className={[
                "h-1.5 w-1.5 rounded-full",
                itemIndex === index ? "bg-[#e7d7a8]" : "bg-white/35"
              ].join(" ")}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

/** Framed compound leaf shown under the staff menu. */
export function SidebarLeafArt() {
  return (
    <svg viewBox="0 0 240 250" className="h-44 w-full" aria-hidden="true">
      <g fill="none" strokeLinecap="round" strokeLinejoin="round">
        <path d="M120 18c2 40 4 90 0 150-3 28-8 48-14 64" stroke="#e7d7a8" strokeWidth="5" />
        <path d="M120 22c1 36 2 80 0 140" stroke="#f6f1e8" strokeWidth="1.2" opacity="0.7" />

        <path d="M118 48c-22-22-58-18-74 2-10 14 4 28 22 20 18-8 38-6 52-22Z" stroke="#f6f1e8" strokeWidth="1.4" />
        <path d="M112 46c-16-4-36 0-48 10" stroke="#e7d7a8" strokeWidth="1" />
        <path d="M70 36c5 0 7 6 2 8-6 3-10-2-7-6 2-2 3-2 5-2Z" stroke="#f6f1e8" strokeWidth="1.1" />
        <path d="M90 32c6 0 8 7 2 9-7 3-12-2-8-7 2-2 4-2 6-2Z" stroke="#f6f1e8" strokeWidth="1.1" />

        <path d="M122 78c24-20 62-14 76 8 8 14-8 26-26 16-20-10-38-8-50-24Z" stroke="#f6f1e8" strokeWidth="1.4" />
        <path d="M128 76c18-2 40 4 54 16" stroke="#e7d7a8" strokeWidth="1" />
        <path d="M150 70c16-8 32-8 46 2" stroke="#f6f1e8" strokeWidth="1" />
        <path d="M156 82c12-5 24-4 34 2" stroke="#f6f1e8" strokeWidth="1" />

        <path d="M116 112c-26-24-70-16-84 8-8 16 8 30 28 18 22-12 42-10 56-26Z" stroke="#f6f1e8" strokeWidth="1.4" />
        <path d="M110 110c-20 0-44 8-58 20" stroke="#e7d7a8" strokeWidth="1" />
        <path d="M52 108c10 6 10-6 20 0s10-6 20 0 10-6 20 0" stroke="#f6f1e8" strokeWidth="1.15" />
        <path d="M60 120c8 5 8-5 16 0s8-5 16 0 8-5 16 0" stroke="#f6f1e8" strokeWidth="1.15" />

        <path d="M124 148c28-22 74-12 86 14 6 16-12 28-32 16-22-14-40-12-54-30Z" stroke="#f6f1e8" strokeWidth="1.4" />
        <path d="M130 146c22 2 48 10 62 22" stroke="#e7d7a8" strokeWidth="1" />
        <path d="M156 142l10-7 10 7 10-7 10 7 10-7" stroke="#f6f1e8" strokeWidth="1.15" />
        <path d="M164 154l8-5 8 5 8-5 8 5 8-5" stroke="#f6f1e8" strokeWidth="1.15" />

        <path d="M114 186c-24-18-64-8-74 16-5 14 10 24 26 14 18-12 36-12 48-30Z" stroke="#f6f1e8" strokeWidth="1.4" />
        <path d="M108 184c-16 2-36 10-48 18" stroke="#e7d7a8" strokeWidth="1" />
        <circle cx="62" cy="184" r="2.3" stroke="#f6f1e8" strokeWidth="1.1" />
        <circle cx="76" cy="178" r="2.3" stroke="#f6f1e8" strokeWidth="1.1" />
        <circle cx="90" cy="176" r="2.3" stroke="#f6f1e8" strokeWidth="1.1" />
        <circle cx="68" cy="194" r="2" stroke="#e7d7a8" strokeWidth="1" />
        <circle cx="84" cy="190" r="2" stroke="#e7d7a8" strokeWidth="1" />

        <path d="M118 214c18-12 46-4 52 14 3 10-8 16-18 10-12-8-24-10-34-24Z" stroke="#f6f1e8" strokeWidth="1.3" />
        <path d="M124 212c12 2 26 8 32 14" stroke="#e7d7a8" strokeWidth="1" />
        <path d="M138 214l5 7h-10zM152 210l5 7h-10z" stroke="#f6f1e8" strokeWidth="1" />
      </g>
    </svg>
  );
}
