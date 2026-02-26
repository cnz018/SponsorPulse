/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./SponsorPulse/**/*.{razor,html,razor.cs}",
    "./SponsorPulse/wwwroot/**/*.html",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: "#f0f7ff",
          100: "#e0efff",
          200: "#bae6fd",
          300: "#7dd3fc",
          400: "#38bdf8",
          500: "#0ea5e9",
          600: "#0284c7",
          700: "#0369a1",
          800: "#075985",
          900: "#0c3d66",
          950: "#050e21", // Ink Black
        },
        ink: {
          black: "#0C1821",
          darkspace: "#1B2A41",
          charcoal: "#324A5F",
        },
        accent: {
          lavender: "#CCC9DC",
          charcoal_blue: "#324A5F",
        },
        neutral: {
          grey_text: "#94A3B8",
          grey_muted: "#475569",
          white: "#F8FAFC",
        },
      },
      borderRadius: {
        card: "16px",
        button: "8px",
        input: "12px",
      },
      fontFamily: {
        sans: ["Inter", "Gilroy", "system-ui", "sans-serif"],
      },
      spacing: {
        sidebar: "260px",
        content_padding: "32px",
      },
      backdropBlur: {
        glass: "10px",
      },
    },
  },
  plugins: [],
};
