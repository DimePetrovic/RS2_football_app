/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  // Prefiks sprečava koliziju sa Angular Material klasama
  prefix: 'tw-',
  theme: {
    extend: {
      colors: {
        // Comeback brand paleta — Concrete Dark (default tema).
        // Vrednosti se vezuju za CSS tokene iz styles.scss (jedan izvor istine).
        brand: {
          volt:      'var(--cb-primary)',      // #C7F24E
          'volt-deep': 'var(--cb-primary-deep)', // #5F8C14
          amber:     'var(--cb-amber)',        // #F7B75C
          blue:      'var(--cb-blue)',         // #3D88F2
          bg:        'var(--cb-bg)',           // #181B20
          surface:   'var(--cb-surface)',      // #22252C
          'surface-2': 'var(--cb-surface-2)',  // #282C34
          border:    'var(--cb-border)',       // #3D424B
          text:      'var(--cb-text)',
          muted:     'var(--cb-text-muted)',   // #8A8D93
          win:       'var(--cb-success)',      // #34C759
          loss:      'var(--cb-error)',        // #FF5340
        },
      },
      fontFamily: {
        display: ['Oswald', 'sans-serif'],
        body:    ['Inter', 'sans-serif'],
        mono:    ['"JetBrains Mono"', 'monospace'],
      },
      borderRadius: {
        cb:     'var(--cb-radius)',
        'cb-sm': 'var(--cb-radius-sm)',
      },
    },
  },
  plugins: [
    // 'class' strategy: only style elements with explicit .form-* classes, so the plugin
    // never touches Angular Material inputs (Material owns all form styling here).
    require('@tailwindcss/forms')({ strategy: 'class' }),
  ],
};
