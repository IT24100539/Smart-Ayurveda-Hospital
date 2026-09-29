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

export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    spacing,
    extend: {
      colors: {
        primary: {
          DEFAULT: "#0f5c5b",
          dark: "#0a4342",
          light: "#1a7573",
          muted: "#d7eceb"
        },
        surface: {
          DEFAULT: "#f4efe6",
          raised: "#fffdf8",
          border: "#e4ddd0"
        },
        ink: "#1a2e2d",
        muted: "#5d6f6e",
        neutral: {
          50: "#f7f5f2",
          100: "#efeae3",
          200: "#e4ddd0",
          300: "#cfc6b8",
          400: "#8f8680",
          500: "#5d6f6e",
          600: "#3f524f",
          700: "#2c3d3b",
          800: "#1a2e2d",
          900: "#101c1b"
        },
        danger: "#9f1239",
        status: {
          pending: { DEFAULT: "#8a5a12", bg: "#f8ecd4", fg: "#6b4208" },
          approved: { DEFAULT: "#0f5c5b", bg: "#d7eceb", fg: "#0a4342" },
          rejected: { DEFAULT: "#9a3412", bg: "#fde8dc", fg: "#7c2d12" },
          success: { DEFAULT: "#1d4e89", bg: "#e4eef8", fg: "#163a66" },
          error: { DEFAULT: "#9f1239", bg: "#fde8ee", fg: "#881337" }
        }
      },
      fontFamily: {
        sans: ['"Source Sans 3"', "system-ui", "sans-serif"],
        display: ['"Source Serif 4"', "Georgia", "serif"],
        serif: ['"Source Serif 4"', "Georgia", "serif"]
      },
      borderRadius: {
        sm: "0.25rem",
        md: "0.5rem",
        lg: "0.75rem",
        xl: "1rem",
        "2xl": "1.25rem",
        full: "9999px"
      },
      boxShadow: {
        sm: "0 1px 2px rgba(26, 46, 45, 0.06)",
        md: "0 4px 12px rgba(26, 46, 45, 0.08)",
        lg: "0 12px 32px rgba(26, 46, 45, 0.12)"
      }
    }
  },
  plugins: []
};
