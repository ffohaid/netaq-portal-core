/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#1a56db',
          600: '#1e40af',
          700: '#1e3a8a',
          800: '#1e3a5f',
          900: '#172554',
        },
        secondary: {
          50: '#f0fdf4',
          100: '#dcfce7',
          500: '#22c55e',
          600: '#16a34a',
          700: '#15803d',
        },
        danger: {
          50: '#fef2f2',
          500: '#ef4444',
          600: '#dc2626',
          700: '#b91c1c',
        },
        warning: {
          50: '#fffbeb',
          500: '#f59e0b',
          600: '#d97706',
        },
        govtech: {
          dark: '#0c4a6e',
          light: '#e0f2fe',
        }
      },
      fontFamily: {
        'arabic': ['IBM Plex Sans Arabic', 'Noto Sans Arabic', 'sans-serif'],
        'english': ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
}

