/** @type {import('tailwindcss').Config} */
const spacing = {
  px: "1px",
  0: "0px",
  0.5: "0.125rem",
  1: "0.25rem",
  1.5: "0.375rem",
  2: "0.5rem",
  2.5: "0.625rem",
  3: "0.75rem",
  3.5: "0.875rem",
  4: "1rem",
  5: "1.25rem",
  6: "1.5rem",
  7: "1.75rem",
  8: "2rem",
  9: "2.25rem",
  10: "2.5rem",
  11: "2.75rem",
  12: "3rem",
  14: "3.5rem",
  16: "4rem",
  20: "5rem",
  24: "6rem",
  28: "7rem",
  32: "8rem",
  36: "9rem",
  40: "10rem",
  44: "11rem",
  48: "12rem",
  52: "13rem",
  56: "14rem",
  60: "15rem",
  64: "16rem",
  72: "18rem",
  80: "20rem",
  96: "24rem"
};

/** Colour backed by a CSS variable of RGB channels (see src/index.css). */
const token = (name) => `rgb(var(--${name}) / <alpha-value>)`;

const status = (name) => ({
  DEFAULT: token(`status-${name}-fg`),
  bg: token(`status-${name}-bg`),
  fg: token(`status-${name}-fg`)
});

export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    spacing,
    extend: {
      colors: {
        primary: {
          DEFAULT: token("primary"),
          dark: token("primary-dark"),
          light: token("primary-light"),
          muted: token("primary-muted"),
          on: token("on-primary")
        },
        surface: {
          DEFAULT: token("surface"),
          raised: token("surface-raised"),
          sunken: token("surface-sunken"),
          border: token("surface-border")
        },
        field: {
          DEFAULT: token("field-bg"),
          border: token("field-border")
        },
        ink: token("ink"),
        muted: token("muted"),
        heading: token("heading"),
        focus: token("focus"),
        gold: {
          DEFAULT: token("gold"),
          muted: token("gold-muted")
        },
        neutral: {
          50: token("neutral-50"),
          100: token("neutral-100"),
          200: token("neutral-200"),
          300: token("neutral-300"),
          400: token("neutral-400"),
          500: token("neutral-500"),
          600: token("neutral-600"),
          700: token("neutral-700"),
          800: token("neutral-800"),
          900: token("neutral-900")
        },
        danger: {
          DEFAULT: token("danger"),
          on: token("on-danger")
        },
        warning: token("warning"),
        success: token("success"),
        saffron: token("saffron"),
        "label-muted": token("label"),
        scrim: token("scrim"),
        "on-scrim": token("on-scrim"),
        "on-hero": token("on-hero"),
        "on-header": token("on-header"),
        pill: {
          DEFAULT: token("pill-fg"),
          bg: token("pill-bg"),
          fg: token("pill-fg")
        },
        header: {
          start: token("header-start"),
          end: token("header-end")
        },
        sunken: token("surface-sunken"),
        /* Sidebar and page heroes stay dark in both modes. Values live in index.css. */
        hero: {
          DEFAULT: token("hero"),
          deep: token("hero-deep"),
          muted: token("hero-muted"),
          accent: token("hero-accent"),
          button: token("hero-button"),
          "button-hover": token("hero-button-hover")
        },
        status: {
          pending: status("pending"),
          approved: status("approved"),
          rejected: status("rejected"),
          success: status("success"),
          error: status("error")
        }
      },
      fontFamily: {
        sans: ['"Source Sans 3"', '"Noto Sans Sinhala"', "system-ui", "sans-serif"],
        display: ['"Source Serif 4"', '"Noto Sans Sinhala"', "Georgia", "serif"],
        serif: ['"Source Serif 4"', '"Noto Sans Sinhala"', "Georgia", "serif"]
      },
      fontSize: {
        display: ["2.5rem", { lineHeight: "2.75rem" }],
        page: ["2rem", { lineHeight: "2.5rem" }],
        section: ["1.375rem", { lineHeight: "1.75rem" }],
        card: ["1.25rem", { lineHeight: "1.75rem" }],
        body: ["0.9375rem", { lineHeight: "1.5rem" }],
        caption: ["0.8125rem", { lineHeight: "1.25rem" }],
        label: ["0.6875rem", { lineHeight: "1rem", letterSpacing: "0.12em" }]
      },
      borderRadius: {
        sm: "0.25rem",
        md: "0.5rem",
        lg: "0.75rem",
        xl: "1rem",
        "2xl": "1.25rem",
        control: "16px",
        card: "24px",
        header: "28px",
        modal: "28px",
        full: "9999px"
      },
      boxShadow: {
        sm: "0 1px 2px rgb(var(--shadow) / 0.05)",
        md: "0 4px 12px rgb(var(--shadow) / 0.06)",
        lg: "0 8px 20px rgb(var(--shadow) / 0.08)",
        card: "0 1px 2px rgb(var(--shadow) / 0.06)"
      }
    }
  },
  plugins: []
};
