/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
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
        danger: "#a84832"
      },
      fontFamily: {
        sans: ['"Source Sans 3"', "system-ui", "sans-serif"],
        serif: ['"Source Serif 4"', "Georgia", "serif"]
      }
    }
  },
  plugins: []
};
