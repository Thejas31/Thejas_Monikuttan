/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#00b4d8', // Cyan
          dark: '#0096c7',
        },
        dark: {
          DEFAULT: '#1e293b', // Slate 800
          blue: '#1e3a8a', // Blue 900
          bg: '#0f172a', // Slate 900
          card: 'rgba(30, 41, 59, 0.7)', // Transparent card bg
        },
        status: {
          success: '#10b981', // Green
          successBg: '#d1fae5',
          warning: '#f59e0b', // Yellow
          warningBg: '#fef3c7',
          danger: '#ef4444', // Red
          dangerBg: '#fee2e2',
        }
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
      }
    },
  },
  plugins: [],
}
